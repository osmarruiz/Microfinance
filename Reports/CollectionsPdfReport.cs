using Microfinance.Models.Business;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Microfinance.Reports;

public class CollectionsPdfReport : IDocument
{
    private readonly List<CollectionManagement> _collections;
    private readonly DateTime? _startDate;
    private readonly DateTime? _endDate;

    public CollectionsPdfReport(List<CollectionManagement> collections, DateTime? startDate, DateTime? endDate)
    {
        _collections = collections;
        _startDate = startDate;
        _endDate = endDate;
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
                .Text("Reporte de Gestiones de Cobro")
                .SemiBold().FontSize(16).FontColor(Colors.Blue.Darken3);

            page.Content()
                .PaddingVertical(1, Unit.Centimetre)
                .Column(column =>
                {
                    // Período del reporte
                    column.Item()
                        .PaddingBottom(10)
                        .Text(text =>
                        {
                            text.Span("Período: ");
                            text.Span(_startDate.HasValue ? _startDate.Value.ToString("dd/MM/yyyy") : "Todos");
                            text.Span(" - ");
                            text.Span(_endDate.HasValue ? _endDate.Value.ToString("dd/MM/yyyy") : "Todos");
                        });

                    // Tabla de gestiones
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40); // ID
                            columns.RelativeColumn();   // Fecha
                            columns.RelativeColumn();   // Cliente
                            columns.RelativeColumn();   // Préstamo
                            columns.RelativeColumn();   // Cobrador
                            columns.RelativeColumn();   // Resultado
                            columns.RelativeColumn();   // Notas
                        });

                        // Encabezados
                        table.Header(header =>
                        {
                            header.Cell().Element(CellHeaderStyle).Text("ID");
                            header.Cell().Element(CellHeaderStyle).Text("Fecha");
                            header.Cell().Element(CellHeaderStyle).Text("Cliente");
                            header.Cell().Element(CellHeaderStyle).Text("Préstamo");
                            header.Cell().Element(CellHeaderStyle).Text("Cobrador");
                            header.Cell().Element(CellHeaderStyle).Text("Resultado");
                            header.Cell().Element(CellHeaderStyle).Text("Notas");

                            header.Cell().ColumnSpan(7)
                                .PaddingTop(5)
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten1);
                        });

                        // Datos
                        foreach (var collection in _collections)
                        {
                            table.Cell().Element(CellStyle).Text(collection.CollectionId.ToString());
                            table.Cell().Element(CellStyle).Text(collection.ManagementDate.LocalDateTime.ToString("dd/MM/yyyy HH:mm"));
                            table.Cell().Element(CellStyle).Text(collection.Loan?.Customer?.FullName ?? "N/A");
                            table.Cell().Element(CellStyle).Text(collection.LoanId.ToString());
                            table.Cell().Element(CellStyle).Text(collection.Collector?.UserName ?? "N/A");
                            
                            var resultColor = collection.ManagementResult switch
                            {
                                "Exitoso" => Colors.Green.Darken3,
                                "Fallido" => Colors.Red.Darken3,
                                _ => Colors.Grey.Darken2
                            };
                            
                            table.Cell().Element(CellStyle).Text(text => 
                                text.Span(collection.ManagementResult ?? "N/A").FontColor(resultColor));
                            
                            table.Cell().Element(CellStyle).Text(collection.Notes ?? "Sin notas");
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