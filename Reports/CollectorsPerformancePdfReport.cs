using Microfinance.Reports.DTO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Microfinance.Reports;

public class CollectorsPerformancePdfReport : IDocument
{
    private readonly List<CollectorPerformanceData> _collectorsData;

    public CollectorsPerformancePdfReport(List<CollectorPerformanceData> collectorsData)
    {
        _collectorsData = collectorsData;
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
                .Text("Reporte de Desempeño de Cobradores")
                .SemiBold().FontSize(16).FontColor(Colors.Blue.Darken3);

            page.Content()
                .PaddingTop(15)
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(); // Cobrador
                        columns.RelativeColumn(); // Gestiones
                        columns.RelativeColumn(); // Exitosas
                        columns.RelativeColumn(); // Monto
                    });

                    // Encabezados con estilo
                    table.Header(header =>
                    {
                        header.Cell().Element(CellHeaderStyle).Text("Cobrador").Bold();
                        header.Cell().Element(CellHeaderStyle).Text("Prestamos Asociados").Bold();
                        header.Cell().Element(CellHeaderStyle).Text("Pagos Exitosos").Bold();
                        header.Cell().Element(CellHeaderStyle).Text("Monto").Bold();
                    });

                    // Datos con estilo
                    foreach (var data in _collectorsData)
                    {
                        string collectorName = data.Collector?.UserName ?? "N/A";
                        int collections = data.Collections;
                        int successful = data.SuccessfulCollections;
                        decimal amount = data.AmountCollected;

                        table.Cell().Element(CellStyle).Text(collectorName);
                        table.Cell().Element(CellStyle).Text(collections.ToString());
                        table.Cell().Element(CellStyle).Text(successful.ToString());
                        table.Cell().Element(CellStyle).Text($"C${amount:N2}");
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
