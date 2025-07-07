using Microfinance.Models.Business;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Microfinance.Reports;

public class LoanWithInstallmentsPdfReport : IDocument
{
    private readonly List<Loan> _loans;

    public LoanWithInstallmentsPdfReport(List<Loan> loans)
    {
        _loans = loans;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header()
                .AlignCenter()
                .Text("Reporte Detallado de Préstamos con Cuotas")
                .SemiBold().FontSize(16).FontColor(Colors.Blue.Darken3);

            page.Content()
                .PaddingVertical(1, Unit.Centimetre)
                .Column(column =>
                {
                    foreach (var loan in _loans)
                    {
                        column.Item().BorderBottom(1).PaddingBottom(10).Column(loanColumn =>
                        {
                            // Encabezado del préstamo mejorado
                            loanColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Préstamo #{loan.LoanId}")
                                    .SemiBold().FontSize(14);

                                row.AutoItem().Text(loan.LoanStatus)
                                    .FontColor(loan.LoanStatus == "Activo" ? Colors.Green.Darken3 : Colors.Red.Darken3)
                                    .SemiBold();
                            });

                            loanColumn.Item().Text($"Cliente: {loan.Customer.FullName} ({loan.Customer.IdCard})").SemiBold()
                                .FontSize(11);

                            // Detalles ampliados del préstamo
                            loanColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Column(detailColumn =>
                                {
                                    detailColumn.Item().Text($"Monto total: C${loan.PrincipalAmount:N2}");
                                    detailColumn.Item().Text($"Interés mensual: {loan.MonthlyInterestRate}%");
                                });

                                row.RelativeItem().Column(detailColumn =>
                                {
                                    detailColumn.Item().Text($"Inicio: {loan.StartDate:dd/MM/yyyy}");
                                    detailColumn.Item().Text($"Vencimiento: {loan.DueDate:dd/MM/yyyy}");
                                    detailColumn.Item().Text($"Plazo: {loan.TermMonths} meses");
                                    detailColumn.Item()
                                        .Text(
                                            $"Cuotas pendientes: {loan.Installments.Count(i => i.InstallmentStatus != "Pagada")}");
                                });
                            });

                            // Tabla de cuotas ampliada
                            loanColumn.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(40); // N°
                                    columns.RelativeColumn(); // Fecha
                                    columns.RelativeColumn(); // Principal
                                    columns.RelativeColumn(); // Interés
                                    columns.RelativeColumn(); // Moratorio
                                    columns.RelativeColumn(); // Total
                                    columns.RelativeColumn(); // Pagado
                                    columns.RelativeColumn(); // Saldo
                                    columns.RelativeColumn(); // Estado
                                    columns.RelativeColumn(); // Fecha Pago
                                });

                                // Encabezado de la tabla
                                table.Header(header =>
                                {
                                    header.Cell().Text("#").FontSize(7).SemiBold();
                                    header.Cell().Text("Vencimiento").FontSize(7).SemiBold();
                                    header.Cell().AlignRight().Text("Principal").FontSize(7).SemiBold();
                                    header.Cell().AlignRight().Text("Interés").FontSize(7).SemiBold();
                                    header.Cell().AlignRight().Text("Moratorio").FontSize(7).SemiBold();
                                    header.Cell().AlignRight().Text("Total").FontSize(7).SemiBold();
                                    header.Cell().AlignRight().Text("Pagado").FontSize(7).SemiBold();
                                    header.Cell().AlignRight().Text("Saldo").FontSize(7).SemiBold();
                                    header.Cell().AlignRight().Text("Estado").FontSize(7).SemiBold();
                                    header.Cell().AlignRight().Text("Pago").FontSize(7).SemiBold();

                                    header.Cell().ColumnSpan(10)
                                        .PaddingTop(5)
                                        .BorderBottom(1)
                                        .BorderColor(Colors.Grey.Lighten1);
                                });

                                // Filas de cuotas
                                foreach (var installment in loan.Installments.OrderBy(i => i.InstallmentNumber))
                                {
                                    var statusColor = installment.InstallmentStatus switch
                                    {
                                        "Pagada" => Colors.Green.Darken3,
                                        "Vencida" => Colors.Red.Darken3,
                                        _ => Colors.Orange.Darken3
                                    };

                                    var totalAmount = installment.PrincipalAmount +
                                                      installment.NormalInterestAmount +
                                                      installment.LateInterestAmount;
                                    var balance = totalAmount - installment.PaidAmount;

                                    table.Cell().Element(CellStyle).Text(installment.InstallmentNumber.ToString()).FontSize(6);
                                    table.Cell().Element(CellStyle).Text(installment.DueDate.ToString("dd/MM/yyyy")).FontSize(6);
                                    table.Cell().Element(CellStyle).AlignRight()
                                        .Text($"C${installment.PrincipalAmount:N2}").FontSize(6);
                                    table.Cell().Element(CellStyle).AlignRight()
                                        .Text($"C${installment.NormalInterestAmount:N2}").FontSize(6);
                                    table.Cell().Element(CellStyle).AlignRight()
                                        .Text($"C${installment.LateInterestAmount:N2}").FontSize(6);
                                    table.Cell().Element(CellStyle).AlignRight().Text($"C${totalAmount:N2}").FontSize(6).SemiBold();
                                    table.Cell().Element(CellStyle).AlignRight().Text($"C${installment.PaidAmount:N2}").FontSize(6);
                                    table.Cell().Element(CellStyle).AlignRight().Text($"C${balance:N2}").FontSize(6)
                                        .FontColor(balance > 0 ? Colors.Red.Darken3 : Colors.Green.Darken3);
                                    table.Cell().Element(CellStyle).Text(text =>
                                        text.Span(installment.InstallmentStatus).FontColor(statusColor).FontSize(6));
                                    table.Cell().Element(CellStyle)
                                        .Text(installment.PaymentDate?.ToString("dd/MM/yyyy") ?? "-").FontSize(6);
                                }
                                
                            });
                        });
                    }
                });

            page.Footer()
                .AlignCenter()
                .Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                    text.Span($" - Generado el {DateTime.Now:dd/MM/yyyy HH:mm}");
                });
        });
    }
    
    
    private IContainer CellStyle(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(5)
            .PaddingHorizontal(10)
            .ShowOnce();
    }
}
