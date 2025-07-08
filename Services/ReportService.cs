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

// 1. Configuración de estilos
            var titleStyle = workbook.Style;
            titleStyle.Font.Bold = true;
            titleStyle.Font.FontSize = 16; // Tamaño más grande para el título principal
            titleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleStyle.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189); // Azul corporativo
            titleStyle.Font.FontColor = XLColor.White;
            titleStyle.Border.OutsideBorder = XLBorderStyleValues.Medium;
            titleStyle.Border.OutsideBorderColor = XLColor.DarkBlue;

            var subtitleStyle = workbook.Style;
            subtitleStyle.Font.Italic = true;
            subtitleStyle.Font.FontSize = 12;
            subtitleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            subtitleStyle.Font.FontColor = XLColor.DarkGray;

// 2. Encabezado principal
            worksheet.Cell(1, 1).Value = "REPORTE DETALLADO DE PRÉSTAMOS";
            worksheet.Range(1, 1, 1, 9).Merge().Style =
                titleStyle; // Ajustado a 9 columnas para coincidir con los datos

// 3. Subtítulo con fecha
            worksheet.Cell(2, 1).Value = $"Generado el {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}";
            worksheet.Range(2, 1, 2, 9).Merge().Style = subtitleStyle;

// 4. Espaciado y configuración inicial
            worksheet.Row(1).Height = 25; // Altura de fila para el título
            worksheet.Row(2).Height = 20; // Altura de fila para el subtítulo

// 5. Configuración de página
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.FitToPages(1, 0); // Ajustar a 1 página de ancho
            worksheet.PageSetup.Margins.Top = 0.5;
            worksheet.PageSetup.Margins.Bottom = 0.5;
            worksheet.PageSetup.Margins.Left = 0.5;
            worksheet.PageSetup.Margins.Right = 0.5;

            int currentRow = 3; // Fila actual para comenzar los datos

            foreach (var loan in loans)
            {
                currentRow += 2; // Espacio entre préstamos

                // Encabezado del préstamo
                var loanHeaderStyle = workbook.Style;
                loanHeaderStyle.Font.Bold = true;
                loanHeaderStyle.Fill.BackgroundColor = XLColor.FromArgb(220, 230, 241);
                loanHeaderStyle.Font.FontColor = XLColor.FromArgb(47, 84, 150);

                worksheet.Cell(currentRow, 1).Value = $"PRÉSTAMO #{loan.LoanId}";
                worksheet.Range(currentRow, 1, currentRow, 9).Merge().Style = loanHeaderStyle;
                currentRow++;

                // Información básica del préstamo
                worksheet.Cell(currentRow, 1).Value = "Cliente:";
                worksheet.Cell(currentRow, 2).Value = loan.Customer?.FullName;
                worksheet.Cell(currentRow, 6).Value = "Cédula:";
                worksheet.Cell(currentRow, 7).Value = loan.Customer?.IdCard;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Monto:";
                worksheet.Cell(currentRow, 2).Value = loan.PrincipalAmount;
                worksheet.Cell(currentRow, 2).Style.NumberFormat.Format = "C$#,##0.00";
                worksheet.Cell(currentRow, 6).Value = "Tasa:";
                worksheet.Cell(currentRow, 7).Value = loan.MonthlyInterestRate / 100;
                worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "0.00%";
                currentRow++;

                // Fechas solo con día/mes/año
                worksheet.Cell(currentRow, 1).Value = "Inicio:";
                worksheet.Cell(currentRow, 2).Value = loan.StartDate.Date; // Solo fecha sin hora
                worksheet.Cell(currentRow, 2).Style.DateFormat.Format = "dd/MM/yyyy";
                worksheet.Cell(currentRow, 6).Value = "Vencimiento:";
                worksheet.Cell(currentRow, 7).Value = loan.DueDate.Date; // Solo fecha sin hora
                worksheet.Cell(currentRow, 7).Style.DateFormat.Format = "dd/MM/yyyy";
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Estado:";
                worksheet.Cell(currentRow, 2).Value = loan.LoanStatus;
                var statusColor = loan.LoanStatus == "Activo" ? XLColor.Green : XLColor.Red;
                worksheet.Cell(currentRow, 2).Style.Font.FontColor = statusColor;
                worksheet.Cell(currentRow, 6).Value = "Cuotas Pendientes:";
                worksheet.Cell(currentRow, 7).Value = loan.Installments?.Count(i => i.InstallmentStatus != "Pagada");
                currentRow++;

                // Encabezados de cuotas (exactamente como los necesitas)
                currentRow++;
                var headerStyle = workbook.Style;
                headerStyle.Font.Bold = true;
                headerStyle.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189);
                headerStyle.Font.FontColor = XLColor.White;
                headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerStyle.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerStyle.Border.OutsideBorderColor = XLColor.Black;

                // Columnas exactas solicitadas
                string[] headers =
                {
                    "Vencimiento", "Principal", "Interés", "Moratorio", "Total", "Pagado", "Saldo", "Estado", "Pago"
                };

                // Ajustar índices de columnas para empezar en 1
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(currentRow, i + 1).Value = headers[i];
                    worksheet.Cell(currentRow, i + 1).Style = headerStyle;
                }

                currentRow++;

                // Datos de cuotas
                foreach (var installment in loan.Installments?.OrderBy(i => i.InstallmentNumber))
                {
                    var dueDate = installment.DueDate.Date; // Solo fecha sin hora
                    var paymentDate = installment.PaymentDate?.Date; // Solo fecha sin hora
                    var totalAmount = installment.PrincipalAmount + installment.NormalInterestAmount +
                                      installment.LateInterestAmount;
                    var balance = totalAmount - installment.PaidAmount;

                    // Ajuste de índices para las columnas solicitadas
                    worksheet.Cell(currentRow, 1).Value = dueDate;
                    worksheet.Cell(currentRow, 1).Style.DateFormat.Format = "dd/MM/yyyy";

                    worksheet.Cell(currentRow, 2).Value = installment.PrincipalAmount;
                    worksheet.Cell(currentRow, 3).Value = installment.NormalInterestAmount;
                    worksheet.Cell(currentRow, 4).Value = installment.LateInterestAmount;
                    worksheet.Cell(currentRow, 5).Value = totalAmount;
                    worksheet.Cell(currentRow, 6).Value = installment.PaidAmount;
                    worksheet.Cell(currentRow, 7).Value = balance;
                    worksheet.Cell(currentRow, 8).Value = installment.InstallmentStatus;
                    worksheet.Cell(currentRow, 9).Value = paymentDate;

                    // Formato de fechas y moneda
                    worksheet.Cell(currentRow, 9).Style.DateFormat.Format = "dd/MM/yyyy";
                    for (int col = 2; col <= 7; col++) // Columnas con montos
                    {
                        worksheet.Cell(currentRow, col).Style.NumberFormat.Format = "C$#,##0.00";
                        worksheet.Cell(currentRow, col).Style.NumberFormat.NumberFormatId =
                            4; // Formato de moneda estándar
                    }

                    // Solución para los asteriscos en saldo:
                    if (worksheet.Cell(currentRow, 7).Value.ToString() == "********")
                    {
                        worksheet.Cell(currentRow, 7).Value = balance;
                        worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "C$#,##0.00";
                    }

                    // Color según estado
                    var rowRange = worksheet.Range(currentRow, 1, currentRow, 9);
                    if (installment.InstallmentStatus == "Vencida")
                    {
                        rowRange.Style.Fill.BackgroundColor = XLColor.LightPink;
                        rowRange.Style.Font.FontColor = XLColor.Red;
                    }
                    else if (installment.InstallmentStatus == "Pagada")
                    {
                        rowRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        rowRange.Style.Font.FontColor = XLColor.DarkGreen;
                    }

                    currentRow++;
                }

                // Totales del préstamo (ajustados a las nuevas columnas)
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "TOTALES:";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;

                // Columnas de totales (ajustadas)
                worksheet.Cell(currentRow, 2).FormulaA1 =
                    $"SUM(B{currentRow - loan.Installments.Count - 1}:B{currentRow - 1})";
                worksheet.Cell(currentRow, 3).FormulaA1 =
                    $"SUM(C{currentRow - loan.Installments.Count - 1}:C{currentRow - 1})";
                worksheet.Cell(currentRow, 4).FormulaA1 =
                    $"SUM(D{currentRow - loan.Installments.Count - 1}:D{currentRow - 1})";
                worksheet.Cell(currentRow, 5).FormulaA1 =
                    $"SUM(E{currentRow - loan.Installments.Count - 1}:E{currentRow - 1})";
                worksheet.Cell(currentRow, 6).FormulaA1 =
                    $"SUM(F{currentRow - loan.Installments.Count - 1}:F{currentRow - 1})";
                worksheet.Cell(currentRow, 7).FormulaA1 =
                    $"SUM(G{currentRow - loan.Installments.Count - 1}:G{currentRow - 1})";

                // Formato de totales
                for (int col = 2; col <= 7; col++)
                {
                    worksheet.Cell(currentRow, col).Style.NumberFormat.Format = "C$#,##0.00";
                    worksheet.Cell(currentRow, col).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, col).Style.Fill.BackgroundColor = XLColor.DarkGray;
                }
            }

// Ajustar columnas
            worksheet.Columns().AdjustToContents();

// Configuración de página
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.FitToPages(1, 0);

// Solución definitiva para los asteriscos - asegurar ancho de columna
            worksheet.Columns().Width = 15; // Columna de saldo

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
                .ThenInclude(i => i!.Loan)
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

            // 1. Configuración de estilos (se mantiene igual)
            var titleStyle = workbook.Style;
            titleStyle.Font.Bold = true;
            titleStyle.Font.FontSize = 16;
            titleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleStyle.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189);
            titleStyle.Font.FontColor = XLColor.White;
            titleStyle.Border.OutsideBorder = XLBorderStyleValues.Medium;
            titleStyle.Border.OutsideBorderColor = XLColor.DarkBlue;

            var subtitleStyle = workbook.Style;
            subtitleStyle.Font.Italic = true;
            subtitleStyle.Font.FontSize = 12;
            subtitleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            subtitleStyle.Font.FontColor = XLColor.DarkGray;

            var headerStyle = workbook.Style;
            headerStyle.Font.Bold = true;
            headerStyle.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189);
            headerStyle.Font.FontColor = XLColor.White;
            headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerStyle.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerStyle.Border.OutsideBorderColor = XLColor.Black;

            // 2. Encabezado principal (se mantiene igual)
            worksheet.Cell(1, 1).Value = "REPORTE DE PAGOS";
            worksheet.Range(1, 1, 1, 9).Merge().Style = titleStyle;

            // 3. Subtítulo con período (se mantiene igual)
            worksheet.Cell(2, 1).Value =
                $"Período: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy} | Generado el {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Range(2, 1, 2, 9).Merge().Style = subtitleStyle;

            // 4. Configuración inicial (se mantiene igual)
            worksheet.Row(1).Height = 25;
            worksheet.Row(2).Height = 20;
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.FitToPages(1, 0);
            worksheet.PageSetup.Margins.Top = 0.5;
            worksheet.PageSetup.Margins.Bottom = 0.5;
            worksheet.PageSetup.Margins.Left = 0.5;
            worksheet.PageSetup.Margins.Right = 0.5;

            // 5. Encabezados de columnas (se mantiene igual)
            string[] headers =
            {
                "ID Pago", "Fecha Pago", "Cliente", "Cédula", "ID Préstamo", "No. Cuota", "Monto Pagado", "Referencia",
                "Cobrador"
            };

            int headerRow = 4;
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(headerRow, i + 1).Value = headers[i];
                worksheet.Cell(headerRow, i + 1).Style = headerStyle;
            }

            // 6. DEFINIR ANCHOS DE COLUMNA ANTES DE LLENAR DATOS (MODIFICADO)
            worksheet.Column(1).Width = 10; // ID Pago
            worksheet.Column(2).Width = 20; // Fecha Pago - AUMENTADO para evitar ####
            worksheet.Column(3).Width = 30; // Cliente
            worksheet.Column(4).Width = 15; // Cédula
            worksheet.Column(5).Width = 12; // ID Préstamo
            worksheet.Column(6).Width = 12; // No. Cuota
            worksheet.Column(7).Width = 15; // Monto Pagado
            worksheet.Column(8).Width = 25; // Referencia
            worksheet.Column(9).Width = 25; // Cobrador

            // 7. Llenado de datos con manejo mejorado de fechas (MODIFICADO)
            int currentRow = headerRow + 1;
            foreach (var payment in payments)
            {
                // ID Pago
                worksheet.Cell(currentRow, 1).Value = payment.PaymentId;

                // FECHA - Manejo mejorado
                var dateCell = worksheet.Cell(currentRow, 2);
                if (payment.PaymentDate.HasValue)
                {
                    dateCell.Value = payment.PaymentDate.Value.LocalDateTime;
                    dateCell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                    dateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Forzar el ancho mínimo si es necesario
                    if (dateCell.Value.ToString().Length > 16)
                    {
                        worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 20);
                    }
                }
                else
                {
                    dateCell.Value = "N/A";
                }

                // Resto de los datos (se mantiene igual)
                worksheet.Cell(currentRow, 3).Value = payment.Installment?.Loan?.Customer?.FullName ?? "N/A";
                worksheet.Cell(currentRow, 4).Value = payment.Installment?.Loan?.Customer?.IdCard ?? "N/A";
                worksheet.Cell(currentRow, 5).Value = payment.Installment?.LoanId ?? 0;
                worksheet.Cell(currentRow, 6).Value = payment.Installment?.InstallmentNumber ?? 0;

                worksheet.Cell(currentRow, 7).Value = payment.PaidAmount;
                worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "C$#,##0.00";

                worksheet.Cell(currentRow, 8).Value = payment.Reference ?? "N/A";
                worksheet.Cell(currentRow, 9).Value = payment.Collector?.UserName ?? "N/A";

                // Alternar color de fila (se mantiene igual)
                if (currentRow % 2 == 0)
                {
                    worksheet.Range(currentRow, 1, currentRow, 9).Style.Fill.BackgroundColor =
                        XLColor.FromArgb(242, 242, 242);
                }

                currentRow++;
            }

            // 8. Totales (se mantiene igual)
            var totalRow = currentRow + 1;
            worksheet.Cell(totalRow, 6).Value = "TOTAL:";
            worksheet.Cell(totalRow, 6).Style.Font.Bold = true;
            worksheet.Cell(totalRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(totalRow, 7).Value = payments.Sum(p => p.PaidAmount);
            worksheet.Cell(totalRow, 7).Style.NumberFormat.Format = "C$#,##0.00";
            worksheet.Cell(totalRow, 7).Style.Font.Bold = true;
            worksheet.Cell(totalRow, 7).Style.Fill.BackgroundColor = XLColor.DarkGray;

            // 9. Ajustes finales (MODIFICADO)
            // Asegurar que la columna de fecha mantenga un ancho suficiente
            worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 20);

            // Ajustar el resto de columnas al contenido
            worksheet.Columns().AdjustToContents();

            // Forzar un ancho mínimo para columnas clave
            worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 20); // Fecha
            worksheet.Column(7).Width = Math.Max(worksheet.Column(7).Width, 15); // Monto

            // Borde exterior (se mantiene igual)
            var dataRange = worksheet.Range(headerRow, 1, currentRow - 1, 9);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            dataRange.Style.Border.OutsideBorderColor = XLColor.DarkGray;

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
            var utcStartDate = startDate?.Date.ToUniversalTime() ?? DateTime.UtcNow.AddMonths(-1);
            var utcEndDate = endDate?.Date.AddDays(1).AddTicks(-1).ToUniversalTime() ?? DateTime.UtcNow;


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
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Gestiones");

            // 1. Configuración de estilos
            var titleStyle = workbook.Style;
            titleStyle.Font.Bold = true;
            titleStyle.Font.FontSize = 16;
            titleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleStyle.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189); // Azul corporativo
            titleStyle.Font.FontColor = XLColor.White;
            titleStyle.Border.OutsideBorder = XLBorderStyleValues.Medium;
            titleStyle.Border.OutsideBorderColor = XLColor.DarkBlue;

            var subtitleStyle = workbook.Style;
            subtitleStyle.Font.Italic = true;
            subtitleStyle.Font.FontSize = 12;
            subtitleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            subtitleStyle.Font.FontColor = XLColor.DarkGray;

            var headerStyle = workbook.Style;
            headerStyle.Font.Bold = true;
            headerStyle.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189);
            headerStyle.Font.FontColor = XLColor.White;
            headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerStyle.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerStyle.Border.OutsideBorderColor = XLColor.Black;

            // 2. Encabezado principal
            worksheet.Cell(1, 1).Value = "REPORTE DE GESTIONES DE COBRO";
            worksheet.Range(1, 1, 1, 7).Merge().Style = titleStyle;

            // 3. Subtítulo con período
            string periodo = startDate.HasValue && endDate.HasValue
                ? $"Período: {startDate.Value:dd/MM/yyyy} - {endDate.Value:dd/MM/yyyy} | Generado el {DateTime.Now:dd/MM/yyyy HH:mm}"
                : $"Todos los registros | Generado el {DateTime.Now:dd/MM/yyyy HH:mm}";

            worksheet.Cell(2, 1).Value = periodo;
            worksheet.Range(2, 1, 2, 7).Merge().Style = subtitleStyle;

            // 4. Configuración de página
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.FitToPages(1, 0);
            worksheet.PageSetup.Margins.Top = 0.5;
            worksheet.PageSetup.Margins.Bottom = 0.5;
            worksheet.PageSetup.Margins.Left = 0.5;
            worksheet.PageSetup.Margins.Right = 0.5;

            // 5. Definir anchos de columnas
            worksheet.Column(1).Width = 8; // ID
            worksheet.Column(2).Width = 18; // Fecha
            worksheet.Column(3).Width = 25; // Cliente
            worksheet.Column(4).Width = 10; // Préstamo
            worksheet.Column(5).Width = 20; // Cobrador
            worksheet.Column(6).Width = 20; // Resultado
            worksheet.Column(7).Width = 40; // Notas

            // 6. Encabezados de columnas
            string[] headers =
            {
                "ID",
                "Fecha Gestión",
                "Cliente",
                "Préstamo",
                "Cobrador",
                "Resultado",
                "Notas"
            };

            int headerRow = 4;
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(headerRow, i + 1).Value = headers[i];
                worksheet.Cell(headerRow, i + 1).Style = headerStyle;
            }

            // 7. Llenado de datos
            int currentRow = headerRow + 1;
            foreach (var gestion in collections.OrderByDescending(g => g.ManagementDate))
            {
                // ID
                worksheet.Cell(currentRow, 1).Value = gestion.CollectionId;

                // Fecha con formato seguro
                var dateCell = worksheet.Cell(currentRow, 2);
                if (gestion.ManagementDate != default)
                {
                    dateCell.Value = gestion.ManagementDate.LocalDateTime;
                    dateCell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                    dateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else
                {
                    dateCell.Value = "N/A";
                }

                // Datos del cliente
                worksheet.Cell(currentRow, 3).Value = gestion.Loan?.Customer?.FullName ?? "N/A";
                worksheet.Cell(currentRow, 4).Value = gestion.LoanId;

                // Información del cobrador
                worksheet.Cell(currentRow, 5).Value = gestion.Collector?.UserName ?? "N/A";

                // Resultado con color según tipo
                var resultCell = worksheet.Cell(currentRow, 6);
                resultCell.Value = gestion.ManagementResult ?? "N/A";

                if (gestion.ManagementResult?.Contains("Exitos") ?? false)
                {
                    resultCell.Style.Font.FontColor = XLColor.DarkGreen;
                }
                else if (gestion.ManagementResult?.Contains("Fallid") ?? false)
                {
                    resultCell.Style.Font.FontColor = XLColor.Red;
                }

                // Notas
                worksheet.Cell(currentRow, 7).Value = gestion.Notes ?? "Sin notas";

                // Alternar color de fila para mejor legibilidad
                if (currentRow % 2 == 0)
                {
                    worksheet.Range(currentRow, 1, currentRow, 7).Style.Fill.BackgroundColor =
                        XLColor.FromArgb(242, 242, 242);
                }

                currentRow++;
            }

            // 8. Ajustes finales
            var dataRange = worksheet.Range(headerRow, 1, currentRow - 1, 7);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            dataRange.Style.Border.OutsideBorderColor = XLColor.DarkGray;

            // Asegurar que los ajustes no reduzcan demasiado las columnas clave
            worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 18); // Fecha
            worksheet.Column(7).Width = Math.Max(worksheet.Column(7).Width, 30); // Notas

            // 9. Generar el archivo
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
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
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Cuotas Vencidas");

            // 1. Configuración de estilos
            var titleStyle = workbook.Style;
            titleStyle.Font.Bold = true;
            titleStyle.Font.FontSize = 16;
            titleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleStyle.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189); // Azul corporativo
            titleStyle.Font.FontColor = XLColor.White;
            titleStyle.Border.OutsideBorder = XLBorderStyleValues.Medium;
            titleStyle.Border.OutsideBorderColor = XLColor.DarkBlue;

            var subtitleStyle = workbook.Style;
            subtitleStyle.Font.Italic = true;
            subtitleStyle.Font.FontSize = 12;
            subtitleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            subtitleStyle.Font.FontColor = XLColor.DarkGray;

            var headerStyle = workbook.Style;
            headerStyle.Font.Bold = true;
            headerStyle.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189);
            headerStyle.Font.FontColor = XLColor.White;
            headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerStyle.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerStyle.Border.OutsideBorderColor = XLColor.Black;

            var currencyStyle = workbook.Style;
            currencyStyle.NumberFormat.Format = "C$#,##0.00";

            // 2. Encabezado principal
            worksheet.Cell(1, 1).Value = "REPORTE DE CUOTAS VENCIDAS";
            worksheet.Range(1, 1, 1, 7).Merge().Style = titleStyle;

            // 3. Subtítulo con fecha de generación
            worksheet.Cell(2, 1).Value = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Range(2, 1, 2, 7).Merge().Style = subtitleStyle;

            // 4. Configuración de página
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.FitToPages(1, 0);
            worksheet.PageSetup.Margins.Top = 0.5;
            worksheet.PageSetup.Margins.Bottom = 0.5;
            worksheet.PageSetup.Margins.Left = 0.5;
            worksheet.PageSetup.Margins.Right = 0.5;

            // 5. Definir anchos de columnas
            worksheet.Column(1).Width = 25; // Cliente
            worksheet.Column(2).Width = 15; // Cédula
            worksheet.Column(3).Width = 10; // Préstamo
            worksheet.Column(4).Width = 10; // Cuota
            worksheet.Column(5).Width = 15; // Vencimiento
            worksheet.Column(6).Width = 15; // Días vencida
            worksheet.Column(7).Width = 15; // Monto

            // 6. Encabezados de columnas
            string[] headers =
            {
                "Cliente", "Cédula", "Préstamo", "Cuota",
                "Vencimiento", "Días vencida", "Monto"
            };

            int headerRow = 4;
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(headerRow, i + 1).Value = headers[i];
                worksheet.Cell(headerRow, i + 1).Style = headerStyle;
            }

            // 7. Datos de cuotas vencidas (MODIFICADO para mejor contraste)
            int currentRow = headerRow + 1;
            foreach (var cuota in installments.OrderByDescending(i => (DateTime.Now - i.DueDate).Days))
            {
                var dueDate = cuota.DueDate.LocalDateTime;
                var montoTotal = cuota.PrincipalAmount + cuota.NormalInterestAmount;
                var diasMora = (DateTime.Now - cuota.DueDate).Days;

                // Determinar estilo de fila
                bool isEvenRow = currentRow % 2 == 0;
                var rowStyle = new
                {
                    Background = isEvenRow ? XLColor.FromArgb(234, 244, 254) : XLColor.White,
                    TextColor = XLColor.Black
                };

                // Aplicar estilo base a toda la fila
                var rowRange = worksheet.Range(currentRow, 1, currentRow, 7);
                rowRange.Style.Fill.BackgroundColor = rowStyle.Background;
                rowRange.Style.Font.FontColor = rowStyle.TextColor;

                // Cliente
                worksheet.Cell(currentRow, 1).Value = cuota.Loan?.Customer?.FullName ?? "N/A";

                // Cédula
                worksheet.Cell(currentRow, 2).Value = cuota.Loan?.Customer?.IdCard ?? "N/A";

                // Préstamo
                worksheet.Cell(currentRow, 3).Value = cuota.LoanId;
                worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Cuota
                worksheet.Cell(currentRow, 4).Value = cuota.InstallmentNumber;
                worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Vencimiento
                worksheet.Cell(currentRow, 5).Value = dueDate;
                worksheet.Cell(currentRow, 5).Style.DateFormat.Format = "dd/MM/yyyy";
                worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Días de mora (con color según días vencidos)
                var diasCell = worksheet.Cell(currentRow, 6);
                diasCell.Value = diasMora;
                diasCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (diasMora > 30)
                {
                    diasCell.Style.Font.FontColor = XLColor.Red;
                    diasCell.Style.Font.Bold = true;
                    // Resaltar toda la fila para mora muy alta
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 230, 230); // Rojo claro
                }
                else if (diasMora > 15)
                {
                    diasCell.Style.Font.FontColor = XLColor.Orange;
                    diasCell.Style.Font.Bold = true;
                    // Resaltar toda la fila para mora media
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 242, 204); // Amarillo claro
                }

                // Monto
                worksheet.Cell(currentRow, 7).Value = montoTotal;
                worksheet.Cell(currentRow, 7).Style = currencyStyle;

                currentRow++;
            }

            // 8. Totales (MODIFICADO para mejor contraste)
            var totalRow = currentRow + 1;
            worksheet.Cell(totalRow, 6).Value = "TOTAL:";
            worksheet.Cell(totalRow, 6).Style.Font.Bold = true;
            worksheet.Cell(totalRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var totalMonto = installments.Sum(i => i.PrincipalAmount + i.NormalInterestAmount);
            worksheet.Cell(totalRow, 7).Value = totalMonto;
            worksheet.Cell(totalRow, 7).Style = currencyStyle;
            worksheet.Cell(totalRow, 7).Style.Font.Bold = true;
            worksheet.Cell(totalRow, 7).Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Cell(totalRow, 7).Style.Font.FontColor = XLColor.Black;

            // 9. Ajustes finales
            var dataRange = worksheet.Range(headerRow, 1, currentRow - 1, 7);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            dataRange.Style.Border.OutsideBorderColor = XLColor.DarkGray;

            // Asegurar anchos mínimos
            worksheet.Column(1).Width = Math.Max(worksheet.Column(1).Width, 25); // Cliente
            worksheet.Column(7).Width = Math.Max(worksheet.Column(7).Width, 15); // Monto

            // 10. Generar el archivo
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
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

    // 1. ESTILOS ACTUALIZADOS //
    var titleStyle = workbook.Style;
    titleStyle.Font.Bold = true;
    titleStyle.Font.FontSize = 16;
    titleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    titleStyle.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189); // Azul oscuro
    titleStyle.Font.FontColor = XLColor.White;
    titleStyle.Border.OutsideBorder = XLBorderStyleValues.Medium;
    titleStyle.Border.OutsideBorderColor = XLColor.DarkBlue;

    var subtitleStyle = workbook.Style;
    subtitleStyle.Font.Italic = true;
    subtitleStyle.Font.FontSize = 12;
    subtitleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    subtitleStyle.Font.FontColor = XLColor.DarkGray;

    var headerStyle = workbook.Style;
    headerStyle.Font.Bold = true;
    headerStyle.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189); // Azul oscuro (mismo que título)
    headerStyle.Font.FontColor = XLColor.White;
    headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    headerStyle.Border.OutsideBorder = XLBorderStyleValues.Thin;
    headerStyle.Border.OutsideBorderColor = XLColor.Black;

    var currencyStyle = workbook.Style;
    currencyStyle.NumberFormat.Format = "C$#,##0.00";

    // 2. ESTRUCTURA ACTUALIZADA (sin columna Efectividad) //
    worksheet.Cell(1, 1).Value = "REPORTE DE DESEMPEÑO DE COBRADORES";
    worksheet.Range(1, 1, 1, 4).Merge().Style = titleStyle; // Ahora solo 4 columnas

    worksheet.Cell(2, 1).Value = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
    worksheet.Range(2, 1, 2, 4).Merge().Style = subtitleStyle; // Ahora solo 4 columnas

    worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
    worksheet.PageSetup.FitToPages(1, 0);
    worksheet.PageSetup.Margins.Top = 0.5;
    worksheet.PageSetup.Margins.Bottom = 0.5;
    worksheet.PageSetup.Margins.Left = 0.5;
    worksheet.PageSetup.Margins.Right = 0.5;

    worksheet.Column(1).Width = 25; // Cobrador
    worksheet.Column(2).Width = 20; // Préstamos Asociados
    worksheet.Column(3).Width = 20; // Pagos Exitosos
    worksheet.Column(4).Width = 20; // Monto Recaudado (más ancho para moneda)

    // Encabezados actualizados (sin Efectividad)
    string[] headers = { "Cobrador", "Préstamos Asociados", "Pagos Exitosos", "Monto Recaudado" };

    int headerRow = 4;
    for (int i = 0; i < headers.Length; i++)
    {
        worksheet.Cell(headerRow, i + 1).Value = headers[i];
        worksheet.Cell(headerRow, i + 1).Style = headerStyle;
    }

    // 3. DATOS ACTUALIZADOS (sin columna Efectividad) //
    int currentRow = headerRow + 1;
    foreach (var data in collectorsData.OrderByDescending(d => d.AmountCollected))
    {
        // Cobrador
        worksheet.Cell(currentRow, 1).Value = data.Collector?.UserName ?? "N/A";

        // Préstamos Asociados
        worksheet.Cell(currentRow, 2).Value = data.Collections;
        worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Pagos Exitosos
        worksheet.Cell(currentRow, 3).Value = data.SuccessfulCollections;
        worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Monto Recaudado (formato de moneda C$)
        worksheet.Cell(currentRow, 4).Value = data.AmountCollected;
        worksheet.Cell(currentRow, 4).Style = currencyStyle;

        // Filas alternadas con gris claro (ajustado para mejor visibilidad)
        if (currentRow % 2 == 0)
        {
            var rowRange = worksheet.Range(currentRow, 1, currentRow, 4);
            rowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(230, 230, 230); // Gris más claro
            rowRange.Style.Font.FontColor = XLColor.Black; // Texto en negro para mejor contraste
        }

        currentRow++;
    }

    // 4. TOTALES ACTUALIZADOS (para 4 columnas) //
    var totalRow = currentRow + 1;
    worksheet.Cell(totalRow, 3).Value = "TOTAL:";
    worksheet.Cell(totalRow, 3).Style.Font.Bold = true;
    worksheet.Cell(totalRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

    worksheet.Cell(totalRow, 4).FormulaA1 = $"SUM(D5:D{currentRow - 1})";
    worksheet.Cell(totalRow, 4).Style = currencyStyle;
    worksheet.Cell(totalRow, 4).Style.Font.Bold = true;
    worksheet.Cell(totalRow, 4).Style.Fill.BackgroundColor = XLColor.FromArgb(200, 200, 200); // Gris medio
    worksheet.Cell(totalRow, 4).Style.Font.FontColor = XLColor.Black; // Texto en negro

    // 5. AJUSTES FINALES (para el rango de 4 columnas) //
    var dataRange = worksheet.Range(headerRow, 1, currentRow - 1, 4);
    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
    dataRange.Style.Border.OutsideBorderColor = XLColor.DarkGray;

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

    // 1. Configuración de estilos
    var titleStyle = workbook.Style;
    titleStyle.Font.Bold = true;
    titleStyle.Font.FontSize = 16;
    titleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    titleStyle.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189); // Azul corporativo
    titleStyle.Font.FontColor = XLColor.White;
    titleStyle.Border.OutsideBorder = XLBorderStyleValues.Medium;
    titleStyle.Border.OutsideBorderColor = XLColor.DarkBlue;

    var subtitleStyle = workbook.Style;
    subtitleStyle.Font.Italic = true;
    subtitleStyle.Font.FontSize = 12;
    subtitleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    subtitleStyle.Font.FontColor = XLColor.DarkGray;

    var headerStyle = workbook.Style;
    headerStyle.Font.Bold = true;
    headerStyle.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189);
    headerStyle.Font.FontColor = XLColor.White;
    headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    headerStyle.Border.OutsideBorder = XLBorderStyleValues.Thin;
    headerStyle.Border.OutsideBorderColor = XLColor.Black;

    var currencyStyle = workbook.Style;
    currencyStyle.NumberFormat.Format = "C$#,##0.00";

    // 2. Encabezado principal
    worksheet.Cell(1, 1).Value = "RESUMEN DE PRÉSTAMOS";
    worksheet.Range(1, 1, 1, 4).Merge().Style = titleStyle;

    // 3. Subtítulo con fecha de generación
    worksheet.Cell(2, 1).Value = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
    worksheet.Range(2, 1, 2, 4).Merge().Style = subtitleStyle;

    // 4. Configuración de página
    worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
    worksheet.PageSetup.FitToPages(1, 0);
    worksheet.PageSetup.Margins.Top = 0.5;
    worksheet.PageSetup.Margins.Bottom = 0.5;
    worksheet.PageSetup.Margins.Left = 0.5;
    worksheet.PageSetup.Margins.Right = 0.5;

    // 5. Definir anchos de columnas
    worksheet.Column(1).Width = 25; // Estado
    worksheet.Column(2).Width = 15; // Cantidad
    worksheet.Column(3).Width = 20; // Monto Total
    worksheet.Column(4).Width = 20; // Monto Promedio

    // 6. Encabezados de columnas
    string[] headers =
    {
        "Estado del Préstamo",
        "Cantidad",
        "Monto Total",
        "Monto Promedio"
    };

    int headerRow = 4;
    for (int i = 0; i < headers.Length; i++)
    {
        worksheet.Cell(headerRow, i + 1).Value = headers[i];
        worksheet.Cell(headerRow, i + 1).Style = headerStyle;
    }

    // 7. Datos del resumen (MODIFICADO para mejor contraste)
    int currentRow = headerRow + 1;
    foreach (var item in loansSummary.OrderByDescending(x => x.TotalAmount))
    {
        // Determinar estilo de fila
        bool isEvenRow = currentRow % 2 == 0;
        var rowStyle = new {
            Background = isEvenRow ? XLColor.FromArgb(234, 244, 254) : XLColor.White,
            TextColor = XLColor.Black
        };

        // Aplicar estilo base a toda la fila
        var rowRange = worksheet.Range(currentRow, 1, currentRow, 4);
        rowRange.Style.Fill.BackgroundColor = rowStyle.Background;
        rowRange.Style.Font.FontColor = rowStyle.TextColor;

        // Estado con color según condición
        var statusCell = worksheet.Cell(currentRow, 1);
        statusCell.Value = item.Status;

        if (item.Status.Contains("Vencido") || item.Status.Contains("Atrasado"))
        {
            statusCell.Style.Font.FontColor = XLColor.Red;
            statusCell.Style.Font.Bold = true;
            rowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 230, 230); // Rojo claro
        }
        else if (item.Status.Contains("Pagado") || item.Status.Contains("Completado"))
        {
            statusCell.Style.Font.FontColor = XLColor.DarkGreen;
            rowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(230, 255, 230); // Verde claro
        }

        // Cantidad
        worksheet.Cell(currentRow, 2).Value = item.Count;
        worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Monto Total
        worksheet.Cell(currentRow, 3).Value = item.TotalAmount;
        worksheet.Cell(currentRow, 3).Style = currencyStyle;

        // Monto Promedio
        worksheet.Cell(currentRow, 4).Value = item.AverageAmount;
        worksheet.Cell(currentRow, 4).Style = currencyStyle;

        currentRow++;
    }

    // 8. Totales (MODIFICADO para mejor contraste)
    var totalRow = currentRow + 1;
    worksheet.Cell(totalRow, 1).Value = "TOTALES:";
    worksheet.Cell(totalRow, 1).Style.Font.Bold = true;
    worksheet.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

    // Total de cantidad
    worksheet.Cell(totalRow, 2).FormulaA1 = $"SUM(B5:B{currentRow - 1})";
    worksheet.Cell(totalRow, 2).Style.Font.Bold = true;
    worksheet.Cell(totalRow, 2).Style.Font.FontColor = XLColor.Black;

    // Total de montos
    worksheet.Cell(totalRow, 3).FormulaA1 = $"SUM(C5:C{currentRow - 1})";
    worksheet.Cell(totalRow, 3).Style = currencyStyle;
    worksheet.Cell(totalRow, 3).Style.Font.Bold = true;
    worksheet.Cell(totalRow, 3).Style.Font.FontColor = XLColor.Black;

    // Promedio general
    worksheet.Cell(totalRow, 4).FormulaA1 = $"AVERAGE(D5:D{currentRow - 1})";
    worksheet.Cell(totalRow, 4).Style = currencyStyle;
    worksheet.Cell(totalRow, 4).Style.Font.Bold = true;
    worksheet.Cell(totalRow, 4).Style.Font.FontColor = XLColor.Black;

    // Estilo de fila de totales
    worksheet.Range(totalRow, 1, totalRow, 4).Style.Fill.BackgroundColor = XLColor.LightGray;

    // 9. Ajustes finales
    var dataRange = worksheet.Range(headerRow, 1, currentRow - 1, 4);
    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
    dataRange.Style.Border.OutsideBorderColor = XLColor.DarkGray;

    // Asegurar anchos mínimos
    worksheet.Column(1).Width = Math.Max(worksheet.Column(1).Width, 25); // Estado
    worksheet.Column(3).Width = Math.Max(worksheet.Column(3).Width, 20); // Monto Total
    worksheet.Column(4).Width = Math.Max(worksheet.Column(4).Width, 20); // Monto Promedio

    // 10. Generar el archivo
    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    return stream.ToArray();
}
    }
}