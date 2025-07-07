using Microfinance.Models.Business;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Microfinance.Reports;

public class OverdueInstallmentsPdfReport : IDocument
{
    private readonly List<Installment> _installments;

    public OverdueInstallmentsPdfReport(List<Installment> installments)
    {
        _installments = installments;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header()
                .AlignCenter()
                .Text("Reporte de Cuotas Vencidas")
                .SemiBold().FontSize(16).FontColor(Colors.Red.Darken3);

            page.Content()
                .PaddingVertical(1, Unit.Centimetre)
                .Column(column =>
                {
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(); // Cliente
                            columns.RelativeColumn(); // Cédula
                            columns.RelativeColumn(); // Préstamo
                            columns.RelativeColumn(); // Cuota
                            columns.RelativeColumn(); // Vencimiento
                            columns.RelativeColumn(); // Días vencida
                            columns.RelativeColumn(); // Monto
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellHeaderStyle).Text("Cliente");
                            header.Cell().Element(CellHeaderStyle).Text("Cédula");
                            header.Cell().Element(CellHeaderStyle).Text("Préstamo");
                            header.Cell().Element(CellHeaderStyle).Text("Cuota");
                            header.Cell().Element(CellHeaderStyle).Text("Vencimiento");
                            header.Cell().Element(CellHeaderStyle).Text("Días vencida");
                            header.Cell().Element(CellHeaderStyle).Text("Monto");

                            header.Cell().ColumnSpan(7)
                                .PaddingTop(5)
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten1);
                        });

                        foreach (var installment in _installments)
                        {
                            var daysOverdue = (DateTime.Now - installment.DueDate).Days;

                            table.Cell().Element(CellStyle).Text(installment.Loan?.Customer?.FullName ?? "N/A");
                            table.Cell().Element(CellStyle).Text(installment.Loan?.Customer?.IdCard ?? "N/A");
                            table.Cell().Element(CellStyle).Text(installment.LoanId.ToString());
                            table.Cell().Element(CellStyle).Text($"#{installment.InstallmentNumber}");
                            table.Cell().Element(CellStyle).Text(installment.DueDate.ToString("dd/MM/yyyy"));
                            table.Cell().Element(CellStyle).Text(daysOverdue.ToString())
                                .FontColor(daysOverdue > 30 ? Colors.Red.Darken3 : Colors.Orange.Darken3);
                            table.Cell().Element(CellStyle).AlignRight()
                                .Text($"C${installment.PrincipalAmount + installment.NormalInterestAmount:N2}");
                        }
                    });
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
            .PaddingHorizontal(5)
            .ShowOnce();
    }

    private IContainer CellHeaderStyle(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(5)
            .PaddingHorizontal(5)
            .Background(Colors.Grey.Lighten3);
    }
}