using Microfinance.Models.Business;
using Microfinance.Reports.DTO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Microfinance.Reports;

public class LoansSummaryPdfReport : IDocument
{
    private readonly List<LoanSummaryDto> _loansSummary;

    public LoansSummaryPdfReport(List<LoanSummaryDto> loansSummary)
    {
        _loansSummary = loansSummary;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header()
                .AlignCenter()
                .Text(text => text.Span("Resumen de Préstamos").Bold().FontSize(16));

            page.Content()
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(); // Estado
                        columns.RelativeColumn(); // Cantidad
                        columns.RelativeColumn(); // Monto Total
                        columns.RelativeColumn(); // Monto Promedio
                    });

                    // Encabezados
                    table.Header(header =>
                    {
                        header.Cell().Text(text => text.Span("Estado").Bold());
                        header.Cell().Text(text => text.Span("Cantidad").Bold());
                        header.Cell().Text(text => text.Span("Monto Total").Bold());
                        header.Cell().Text(text => text.Span("Monto Promedio").Bold());
                    });

                    // Datos
                    foreach (var summary in _loansSummary)
                    {
                        table.Cell().Text(text => text.Span(summary.Status));
                        table.Cell().Text(text => text.Span(summary.Count.ToString()));
                        table.Cell().Text(text => text.Span($"${summary.TotalAmount:N2}"));
                        table.Cell().Text(text => text.Span($"${summary.AverageAmount:N2}"));
                    }

                    // Totales
                    table.Cell().ColumnSpan(2).Text(text => text.Span("TOTALES:").Bold());
                    table.Cell().Text(text => text.Span($"${_loansSummary.Sum(x => x.TotalAmount):N2}").Bold());
                    table.Cell().Text(text => text.Span($"${_loansSummary.Average(x => x.AverageAmount):N2}").Bold());
                });

            page.Footer()
                .AlignCenter()
                .Text(text => 
                {
                    text.Span("Generado el ");
                    text.Span($"{DateTime.Now:dd/MM/yyyy HH:mm}");
                });
        });
    }
}