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
                .Text("Reporte de Préstamos con Cuotas")
                .SemiBold().FontSize(16).FontColor(Colors.Blue.Darken3);

            page.Content()
                .PaddingVertical(1, Unit.Centimetre)
                .Column(column =>
                {
                    foreach (var loan in _loans)
                    {
                        column.Item().BorderBottom(1).PaddingBottom(10).Column(loanColumn =>
                        {
                            // Encabezado del préstamo
                            loanColumn.Item().Text($"Préstamo #{loan.LoanId} - {loan.Customer.FullName}")
                                .SemiBold().FontSize(12);

                            // Detalles del préstamo
                            loanColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Column(detailColumn =>
                                {
                                    detailColumn.Item().Text($"Monto: C${loan.PrincipalAmount:N2}");
                                    detailColumn.Item().Text($"Interés: {loan.MonthlyInterestRate}% mensual");
                                    detailColumn.Item().Text($"Estado: {loan.LoanStatus}");
                                });

                                row.RelativeItem().Column(detailColumn =>
                                {
                                    detailColumn.Item().Text($"Inicio: {loan.StartDate:dd/MM/yyyy}");
                                    detailColumn.Item().Text($"Vencimiento: {loan.DueDate:dd/MM/yyyy}");
                                    detailColumn.Item().Text($"Plazo (meses): {loan.TermMonths}");
                                });
                            });

                            // Tabla de cuotas
                            loanColumn.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(40); // Número
                                    columns.RelativeColumn();  // Fecha
                                    columns.RelativeColumn();  // Principal
                                    columns.RelativeColumn();  // Interés
                                    columns.RelativeColumn();  // Estado
                                    columns.RelativeColumn();  // Pagado
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("#");
                                    header.Cell().Text("Vencimiento");
                                    header.Cell().AlignRight().Text("Principal");
                                    header.Cell().AlignRight().Text("Interés");
                                    header.Cell().Text("Estado");
                                    header.Cell().AlignRight().Text("Pagado");

                                    header.Cell().ColumnSpan(6)
                                        .PaddingTop(5)
                                        .BorderBottom(1)
                                        .BorderColor(Colors.Grey.Lighten1);
                                });

                                foreach (var installment in loan.Installments.OrderBy(i => i.InstallmentNumber))
                                {
                                    var statusColor = installment.InstallmentStatus switch
                                    {
                                        "Pagada" => Colors.Green.Darken3,
                                        "Vencida" => Colors.Red.Darken3,
                                        _ => Colors.Orange.Darken3
                                    };

                                    table.Cell().Element(CellStyle).Text(installment.InstallmentNumber.ToString());
                                    table.Cell().Element(CellStyle).Text(installment.DueDate.ToString("dd/MM/yyyy"));
                                    table.Cell().Element(CellStyle).AlignRight().Text($"C${installment.PrincipalAmount:N2}");
                                    table.Cell().Element(CellStyle).AlignRight().Text($"C${installment.NormalInterestAmount:N2}");
                                    table.Cell().Element(CellStyle).Text(text => 
                                        text.Span(installment.InstallmentStatus).FontColor(statusColor));
                                    table.Cell().Element(CellStyle).AlignRight().Text($"C${installment.PaidAmount:N2}");
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