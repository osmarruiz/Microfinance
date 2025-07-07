// Services/ReportService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;
using Microfinance.Data;
using Microfinance.Enums;
using Microfinance.Models;
using Microfinance.Models.Business;
using Microfinance.Reports;
using Microfinance.Reports.DTO;


namespace Microfinance.Services
{
    public class ReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReportResult> GenerateLoanWithInstallmentsReport(int? loanId, ReportFormat format)
        {
            var loans = await _context.Loans
                .Include(l => l.Customer)
                .Include(l => l.Installments)
                .Where(l => !l.IsDeleted && (!loanId.HasValue || l.LoanId == loanId.Value))
                .OrderByDescending(l => l.StartDate)
                .Take(loanId.HasValue ? 1 : 100)
                .ToListAsync();

            if (!loans.Any())
            {
                throw new Exception("No se encontraron préstamos para generar el reporte.");
            }

            if (format == ReportFormat.PDF)
            {
                var report = new LoanWithInstallmentsPdfReport(loans);

                using var stream = new MemoryStream();
                report.GeneratePdf(stream);
                var pdfBytes = stream.ToArray();

                return new ReportResult
                {
                    Content = pdfBytes,
                    ContentType = "application/pdf",
                    FileName =
                        $"PrestamosCuotas_{(loanId.HasValue ? loanId.Value : "Todos")}_{DateTime.Now:yyyyMMdd}.pdf"
                };
            }
            else
            {
                return new ReportResult
                {
                    Content = GenerateLoanWithInstallmentsExcel(loans),
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    FileName =
                        $"PrestamosCuotas_{(loanId.HasValue ? loanId.Value : "Todos")}_{DateTime.Now:yyyyMMdd}.xlsx"
                };
            }
        }

        private byte[] GenerateLoanWithInstallmentsExcel(List<Loan> loans)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Préstamos");

            // Encabezado
            worksheet.Cell(1, 1).Value = "Reporte de Préstamos con Cuotas";
            worksheet.Range(1, 1, 1, 7).Merge().Style.Font.Bold = true;
            worksheet.Range(1, 1, 1, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            foreach (var loan in loans)
            {
                var currentRow = worksheet.LastRowUsed()?.RowNumber() + 2 ?? 3;

                // Información del préstamo
                worksheet.Cell(currentRow, 1).Value = $"Préstamo #{loan.LoanId}";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, 7).Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Cliente:";
                worksheet.Cell(currentRow, 2).Value = loan.Customer.FullName;
                worksheet.Cell(currentRow, 4).Value = "Cédula:";
                worksheet.Cell(currentRow, 5).Value = loan.Customer.IdCard;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Monto:";
                worksheet.Cell(currentRow, 2).Value = loan.PrincipalAmount;
                worksheet.Cell(currentRow, 2).Style.NumberFormat.Format = "C$#,##0.00";
                worksheet.Cell(currentRow, 4).Value = "Tasa:";
                worksheet.Cell(currentRow, 5).Value = loan.MonthlyInterestRate / 100;
                worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "0.00%";
                currentRow++;

                // Encabezados de cuotas
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Cuota #";
                worksheet.Cell(currentRow, 2).Value = "Vencimiento";
                worksheet.Cell(currentRow, 3).Value = "Principal";
                worksheet.Cell(currentRow, 4).Value = "Interés";
                worksheet.Cell(currentRow, 5).Value = "Estado";
                worksheet.Cell(currentRow, 6).Value = "Pagado";
                worksheet.Range(currentRow, 1, currentRow, 6).Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, 6).Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                // Datos de cuotas
                foreach (var installment in loan.Installments.OrderBy(i => i.InstallmentNumber))
                {
                    var dueDate = installment.DueDate.LocalDateTime;

                    worksheet.Cell(currentRow, 1).Value = installment.InstallmentNumber;
                    worksheet.Cell(currentRow, 2).Value = dueDate;
                    worksheet.Cell(currentRow, 2).Style.DateFormat.Format = "dd/MM/yyyy";
                    worksheet.Cell(currentRow, 3).Value = installment.PrincipalAmount;
                    worksheet.Cell(currentRow, 4).Value = installment.NormalInterestAmount;
                    worksheet.Cell(currentRow, 5).Value = installment.InstallmentStatus;
                    worksheet.Cell(currentRow, 6).Value = installment.PaidAmount;

                    // Formato de moneda
                    worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "C$#,##0.00";
                    worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "C$#,##0.00";
                    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "C$#,##0.00";

                    // Color según estado
                    if (installment.InstallmentStatus == "Vencida")
                        worksheet.Range(currentRow, 1, currentRow, 6).Style.Fill.BackgroundColor = XLColor.LightPink;
                    else if (installment.InstallmentStatus == "Pagada")
                        worksheet.Range(currentRow, 1, currentRow, 6).Style.Fill.BackgroundColor = XLColor.LightGreen;

                    currentRow++;
                }
            }

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }


        public async Task<ReportResult> GeneratePaymentsReport(DateTime startDate, DateTime endDate,
            ReportFormat format)
        {
            // Validar fechas
            if (startDate > endDate)
            {
                throw new ArgumentException("La fecha de inicio no puede ser mayor a la fecha final");
            }

            var utcStartDate = startDate.ToUniversalTime();
            var utcEndDate = endDate.Date.AddDays(1).AddTicks(-1).ToUniversalTime();

            var payments = await _context.Payments
                .Include(p => p.Installment)
                .ThenInclude(i => i.Loan)
                .ThenInclude(l => l.Customer)
                .Include(p => p.Collector)
                .Where(p => p.PaymentDate >= utcStartDate &&
                            p.PaymentDate <= utcEndDate &&
                            !p.IsDeleted)
                .OrderBy(p => p.PaymentDate)
                .ToListAsync();

            if (!payments.Any())
            {
                throw new Exception("No se encontraron pagos en el período seleccionado");
            }

            if (format == ReportFormat.PDF)
            {
                var report = new PaymentsPdfReport(payments, startDate, endDate);

                using var stream = new MemoryStream();
                report.GeneratePdf(stream);
                var pdfBytes = stream.ToArray();

                return new ReportResult
                {
                    Content = pdfBytes,
                    ContentType = "application/pdf",
                    FileName = $"ReportePagos_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf"
                };
            }
            else
            {
                return new ReportResult
                {
                    Content = GeneratePaymentsExcelReport(payments, startDate, endDate),
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    FileName = $"ReportePagos_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx"
                };
            }
        }

        private byte[] GeneratePaymentsExcelReport(List<Payment> payments, DateTime startDate, DateTime endDate)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Pagos");

            // Encabezado del reporte
            worksheet.Cell(1, 1).Value = "Reporte de Pagos";
            worksheet.Range(1, 1, 1, 9).Merge().Style.Font.Bold = true;
            worksheet.Range(1, 1, 1, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Período del reporte
            worksheet.Cell(2, 1).Value = $"Período: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
            worksheet.Range(2, 1, 2, 9).Merge().Style.Font.Italic = true;

            // Encabezados de columnas
            var headers = new[]
            {
                "ID Pago", "Fecha", "Cliente", "Cédula", "Préstamo", "Cuota",
                "Monto", "Referencia Pago", "Cobrador"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(4, i + 1).Value = headers[i];
                worksheet.Cell(4, i + 1).Style.Font.Bold = true;
                worksheet.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Datos de pagos
            int currentRow = 5;
            foreach (var payment in payments)
            {
                worksheet.Cell(currentRow, 1).Value = payment.PaymentId;
                worksheet.Cell(currentRow, 2).Value = payment.PaymentDate?.LocalDateTime;
                worksheet.Cell(currentRow, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                worksheet.Cell(currentRow, 3).Value = payment.Installment?.Loan?.Customer?.FullName;
                worksheet.Cell(currentRow, 4).Value = payment.Installment?.Loan?.Customer?.IdCard;
                worksheet.Cell(currentRow, 5).Value = payment.Installment?.LoanId;
                worksheet.Cell(currentRow, 7).Value = payment.Installment?.InstallmentNumber;
                worksheet.Cell(currentRow, 8).Value = payment.PaidAmount;
                worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "C$#,##0.00";
                worksheet.Cell(currentRow, 9).Value = payment.Reference ?? "N/A";
                worksheet.Cell(currentRow, 10).Value = payment.Collector?.UserName ?? "N/A";

                currentRow++;
            }

            // Ajustar columnas al contenido
            worksheet.Columns().AdjustToContents();

            // Totales
            worksheet.Cell(currentRow + 1, 7).Value = "Total:";
            worksheet.Cell(currentRow + 1, 7).Style.Font.Bold = true;
            worksheet.Cell(currentRow + 1, 8).Value = payments.Sum(p => p.PaidAmount);
            worksheet.Cell(currentRow + 1, 8).Style.NumberFormat.Format = "C$#,##0.00";
            worksheet.Cell(currentRow + 1, 8).Style.Font.Bold = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }

        public async Task<ReportResult> GenerateCollectionsReport(DateTime? startDate, DateTime? endDate,
            ReportFormat format)
        {
            // Validar fechas
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                throw new ArgumentException("La fecha de inicio no puede ser mayor a la fecha final");
            }

            // Establecer fechas por defecto si no están especificadas
            var utcStartDate = startDate?.ToUniversalTime() ?? DateTime.UtcNow.AddMonths(-1);
            var utcEndDate = endDate?.ToUniversalTime() ?? DateTime.UtcNow;

            var collections = await _context.CollectionManagements
                .Include(c => c.Loan)
                .ThenInclude(l => l.Customer)
                .Include(c => c.Collector)
                .Where(c => c.ManagementDate >= utcStartDate &&
                            c.ManagementDate <= utcEndDate &&
                            !c.IsDeleted)
                .OrderBy(c => c.ManagementDate)
                .ToListAsync();

            if (!collections.Any())
            {
                throw new Exception("No se encontraron gestiones de cobro en el período seleccionado");
            }

            if (format == ReportFormat.PDF)
            {
                var report = new CollectionsPdfReport(collections, startDate, endDate);

                using var stream = new MemoryStream();
                report.GeneratePdf(stream);
                var pdfBytes = stream.ToArray();

                return new ReportResult
                {
                    Content = pdfBytes,
                    ContentType = "application/pdf",
                    FileName = $"ReporteGestionesCobro_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf"
                };
            }
            else
            {
                return new ReportResult
                {
                    Content = GenerateCollectionsExcelReport(collections, startDate, endDate),
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    FileName = $"ReporteGestionesCobro_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx"
                };
            }
        }

        private byte[] GenerateCollectionsExcelReport(List<CollectionManagement> collections, DateTime? startDate,
            DateTime? endDate)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Gestiones");

                // 1. Configuración básica del reporte
                worksheet.Cell(1, 1).Value = "Reporte de Gestiones de Cobro";
                worksheet.Range(1, 1, 1, 7).Merge();
                worksheet.Range(1, 1, 1, 7).Style.Font.Bold = true;
                worksheet.Range(1, 1, 1, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // 2. Período del reporte
                string periodo = startDate.HasValue && endDate.HasValue
                    ? $"Período: {startDate.Value:dd/MM/yyyy} - {endDate.Value:dd/MM/yyyy}"
                    : "Todos los registros";

                worksheet.Cell(2, 1).Value = periodo;
                worksheet.Range(2, 1, 2, 7).Merge();
                worksheet.Range(2, 1, 2, 7).Style.Font.Italic = true;
                worksheet.Range(2, 1, 2, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // 3. Encabezados de columnas
                string[] headers = { "ID", "Fecha", "Cliente", "Préstamo", "Cobrador", "Resultado", "Notas" };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(4, i + 1).Value = headers[i];
                    worksheet.Cell(4, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                // 4. Llenado de datos
                int filaActual = 5;
                foreach (var gestion in collections)
                {
                    worksheet.Cell(filaActual, 1).Value = gestion.CollectionId;

                    // Fecha con formato
                    if (gestion.ManagementDate != default)
                    {
                        worksheet.Cell(filaActual, 2).Value = gestion.ManagementDate.LocalDateTime;
                        worksheet.Cell(filaActual, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                    }

                    worksheet.Cell(filaActual, 3).Value = gestion.Loan?.Customer?.FullName ?? "N/A";
                    worksheet.Cell(filaActual, 4).Value = gestion.LoanId;
                    worksheet.Cell(filaActual, 5).Value = gestion.Collector?.UserName ?? "N/A";
                    worksheet.Cell(filaActual, 6).Value = gestion.ManagementResult ?? "N/A";
                    worksheet.Cell(filaActual, 7).Value = gestion.Notes ?? "Sin notas";

                    filaActual++;
                }

                // 5. Ajustes finales
                worksheet.Columns().AdjustToContents();

                // 6. Generar el archivo
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public async Task<ReportResult> GenerateOverdueInstallmentsReport(ReportFormat format)
        {
            var overdueInstallments = await _context.Installments
                .Include(i => i.Loan)
                .ThenInclude(l => l.Customer)
                .Include(i => i.Loan)
                .Where(i => i.DueDate < DateTime.Now &&
                            i.InstallmentStatus == "Vencida" &&
                            !i.IsDeleted)
                .OrderBy(i => i.DueDate)
                .ToListAsync();

            if (!overdueInstallments.Any())
            {
                throw new Exception("No se encontraron cuotas vencidas");
            }

            if (format == ReportFormat.PDF)
            {
                var report = new OverdueInstallmentsPdfReport(overdueInstallments);

                using var stream = new MemoryStream();
                report.GeneratePdf(stream);
                var pdfBytes = stream.ToArray();

                return new ReportResult
                {
                    Content = pdfBytes,
                    ContentType = "application/pdf",
                    FileName = $"ReporteCuotasVencidas_{DateTime.Now:yyyyMMdd}.pdf"
                };
            }
            else
            {
                return new ReportResult
                {
                    Content = GenerateOverdueInstallmentsExcel(overdueInstallments),
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    FileName = $"ReporteCuotasVencidas_{DateTime.Now:yyyyMMdd}.xlsx"
                };
            }
        }


        private byte[] GenerateOverdueInstallmentsExcel(List<Installment> installments)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Cuotas Vencidas");

                // 1. Título del reporte
                worksheet.Cell(1, 1).Value = "Reporte de Cuotas Vencidas";
                worksheet.Range(1, 1, 1, 8).Merge();
                worksheet.Range(1, 1, 1, 8).Style.Font.Bold = true;
                worksheet.Range(1, 1, 1, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // 2. Fecha de generación
                worksheet.Cell(2, 1).Value = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
                worksheet.Range(2, 1, 2, 8).Merge();
                worksheet.Range(2, 1, 2, 8).Style.Font.Italic = true;
                worksheet.Range(2, 1, 2, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // 3. Encabezados de columnas
                string[] headers =
                {
                    "Cliente", "Cédula", "Préstamo", "Cuota",
                    "Vencimiento", "Días vencida", "Monto"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(4, i + 1).Value = headers[i];
                    worksheet.Cell(4, i + 1).Style.Font.Bold = true;
                }

                // 4. Datos de cuotas vencidas
                int fila = 5;
                foreach (var cuota in installments)
                {
                    var dueDate = cuota.DueDate.LocalDateTime;
                    worksheet.Cell(fila, 1).Value = cuota.Loan?.Customer?.FullName ?? "N/A";
                    worksheet.Cell(fila, 2).Value = cuota.Loan?.Customer?.IdCard ?? "N/A";
                    worksheet.Cell(fila, 3).Value = cuota.LoanId;
                    worksheet.Cell(fila, 4).Value = cuota.InstallmentNumber;

                    // Formato de fecha
                    worksheet.Cell(fila, 5).Value = dueDate;
                    worksheet.Cell(fila, 5).Style.DateFormat.Format = "dd/MM/yyyy";

                    // Días de mora
                    int diasMora = (DateTime.Now - cuota.DueDate).Days;
                    worksheet.Cell(fila, 6).Value = diasMora;

                    // Monto con formato de moneda
                    worksheet.Cell(fila, 7).Value = cuota.PrincipalAmount + cuota.NormalInterestAmount;
                    worksheet.Cell(fila, 7).Style.NumberFormat.Format = "C$#,##0.00";


                    fila++;
                }

                // 5. Ajustar columnas al contenido
                worksheet.Columns().AdjustToContents();

                // 6. Generar el archivo
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public async Task<ReportResult> GenerateCollectorsPerformanceReport(ReportFormat format)
        {
            // Obtener cobradores con al menos una gestión
            var collectors = await _context.Users
                .Where(u => _context.CollectionManagements.Any(cm => cm.CollectorId == u.Id))
                .ToListAsync();

            // Obtener todas las gestiones y pagos activos
            var collectionManagements = await _context.CollectionManagements
                .Where(cm => !cm.IsDeleted)
                .ToListAsync();

            var payments = await _context.Payments
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            // Armar datos por cobrador
            var collectorsData = collectors
                .Select(collector =>
                {
                    var userCollections = collectionManagements
                        .Where(cm => cm.CollectorId == collector.Id)
                        .ToList();

                    // Se considera exitosa si hay al menos un pago asociado al cobrador
                    int successfulCollections = userCollections
                        .Count(cm => payments.Any(p => p.CollectorId == cm.CollectorId));

                    decimal totalCollected = payments
                        .Where(p => p.CollectorId == collector.Id)
                        .Sum(p => p.PaidAmount);

                    return new CollectorPerformanceData
                    {
                        Collector = collector,
                        Collections = userCollections.Count,
                        SuccessfulCollections = successfulCollections,
                        AmountCollected = totalCollected
                    };
                })
                .OrderByDescending(x => x.AmountCollected)
                .ToList();

            if (!collectorsData.Any())
                throw new Exception("No se encontraron datos de cobradores.");

            // Generar PDF o Excel según formato
            if (format == ReportFormat.PDF)
            {
                var report = new CollectorsPerformancePdfReport(collectorsData);

                using var stream = new MemoryStream();
                report.GeneratePdf(stream);

                return new ReportResult
                {
                    Content = stream.ToArray(),
                    ContentType = "application/pdf",
                    FileName = $"ReporteDesempeñoCobradores_{DateTime.Now:yyyyMMdd}.pdf"
                };
            }

            return new ReportResult
            {
                Content = GenerateCollectorsPerformanceExcel(collectorsData),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"ReporteDesempeñoCobradores_{DateTime.Now:yyyyMMdd}.xlsx"
            };
        }


        private byte[] GenerateCollectorsPerformanceExcel(List<CollectorPerformanceData> collectorsData)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Desempeño");

            // 1. Título
            worksheet.Cell(1, 1).Value = "Reporte de Desempeño de Cobradores";
            worksheet.Range(1, 1, 1, 5).Merge().Style.Font.Bold = true;

            // 2. Encabezados
            string[] headers = { "Cobrador", "Prestamos Asociados", "Pagos Exitosos", "Monto" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style.Font.Bold = true;
            }

            // 3. Datos
            int row = 4;
            foreach (var data in collectorsData)
            {
                double efectividad = data.Collections > 0
                    ? Math.Round((double)data.SuccessfulCollections / data.Collections * 100, 2)
                    : 0;

                worksheet.Cell(row, 1).Value = data.Collector?.UserName ?? "N/A";
                worksheet.Cell(row, 2).Value = data.Collections;
                worksheet.Cell(row, 3).Value = data.SuccessfulCollections;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "0.00%";
                worksheet.Cell(row, 5).Value = data.AmountCollected;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";

                row++;
            }

            // 4. Totales
            worksheet.Cell(row, 4).Value = "TOTAL:";
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).FormulaA1 = $"SUM(E4:E{row - 1})";
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Cell(row, 5).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        
        public async Task<ReportResult> GenerateLoansSummaryReport(ReportFormat format)
        {
            var loansSummary = await _context.Loans
                .Where(l => !l.IsDeleted)
                .GroupBy(l => l.LoanStatus)
                .Select(g => new LoanSummaryDto
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(l => l.PrincipalAmount),
                    AverageAmount = g.Average(l => l.PrincipalAmount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToListAsync();

            if (!loansSummary.Any())
            {
                throw new Exception("No se encontraron préstamos para generar el reporte");
            }

            if (format == ReportFormat.PDF)
            {
                var report = new LoansSummaryPdfReport(loansSummary);

                using var stream = new MemoryStream();
                report.GeneratePdf(stream);
        
                return new ReportResult
                {
                    Content = stream.ToArray(),
                    ContentType = "application/pdf",
                    FileName = $"ResumenPrestamos_{DateTime.Now:yyyyMMdd}.pdf"
                };
            }
            else
            {
                return new ReportResult
                {
                    Content = GenerateLoansSummaryExcel(loansSummary),
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    FileName = $"ResumenPrestamos_{DateTime.Now:yyyyMMdd}.xlsx"
                };
            }
        }
        
        private byte[] GenerateLoansSummaryExcel(List<LoanSummaryDto> loansSummary)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Resumen");

            // Título
            worksheet.Cell(1, 1).Value = "Resumen de Préstamos";
            worksheet.Range(1, 1, 1, 4).Merge().Style.Font.Bold = true;

            // Encabezados
            string[] headers = { "Estado", "Cantidad", "Monto Total", "Monto Promedio" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style.Font.Bold = true;
            }

            // Datos
            int row = 4;
            foreach (var item in loansSummary)
            {
                worksheet.Cell(row, 1).Value = item.Status;
                worksheet.Cell(row, 2).Value = item.Count;
                worksheet.Cell(row, 3).Value = item.TotalAmount;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                worksheet.Cell(row, 4).Value = item.AverageAmount;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                row++;
            }

            // Totales
            worksheet.Cell(row, 1).Value = "TOTALES:";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 3).FormulaA1 = $"SUM(C4:C{row-1})";
            worksheet.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Cell(row, 4).FormulaA1 = $"AVERAGE(D4:D{row-1})";
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}