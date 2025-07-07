using Microfinance.Models.Business;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Microfinance.Reports;

public class PaymentsPdfReport : IDocument
{
    private readonly List<Payment> _payments;
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;

    public PaymentsPdfReport(List<Payment> payments, DateTime startDate, DateTime endDate)
    {
        _payments = payments;
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
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header()
                .AlignCenter()
                .Text("Reporte de Pagos")
                .SemiBold().FontSize(16).FontColor(Colors.Blue.Darken3);

            page.Content()
                .PaddingVertical(1, Unit.Centimetre)
                .Column(column =>
                {
                    column.Item()
                        .PaddingBottom(10)
                        .Text($"Período: {_startDate:dd/MM/yyyy} - {_endDate:dd/MM/yyyy}")
                        .Italic();

                    column.Item()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40); // ID
                                columns.RelativeColumn(); // Fecha
                                columns.RelativeColumn(); // Cliente
                                columns.RelativeColumn(); // Cédula
                                columns.RelativeColumn();  // Préstamo
                                columns.RelativeColumn();  // Cuota
                                columns.RelativeColumn();  // Monto
                                columns.RelativeColumn(); // Ref. Pago
                                columns.RelativeColumn(); // Cobrador
                            });

                            // Encabezados
                            table.Header(header =>
                            {
                                header.Cell().Element(CellHeaderStyle).Text("ID");
                                header.Cell().Element(CellHeaderStyle).Text("Fecha");
                                header.Cell().Element(CellHeaderStyle).Text("Cliente");
                                header.Cell().Element(CellHeaderStyle).Text("Cédula");
                                header.Cell().Element(CellHeaderStyle).Text("Préstamo");
                                header.Cell().Element(CellHeaderStyle).Text("Cuota");
                                header.Cell().Element(CellHeaderStyle).Text("Monto");
                                header.Cell().Element(CellHeaderStyle).Text("Ref. Pago");
                                header.Cell().Element(CellHeaderStyle).Text("Cobrador");

                                header.Cell().ColumnSpan(9)
                                    .PaddingTop(5)
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten1);
                            });

                            // Datos
                            foreach (var payment in _payments)
                            {
                                table.Cell().Element(CellStyle).Text(payment.PaymentId.ToString());
                                table.Cell().Element(CellStyle).Text(payment.PaymentDate?.LocalDateTime.ToString("dd/MM/yyyy HH:mm") ?? "N/A");
                                table.Cell().Element(CellStyle).Text(payment.Installment?.Loan?.Customer?.FullName ?? "N/A");
                                table.Cell().Element(CellStyle).Text(payment.Installment?.Loan?.Customer?.IdCard ?? "N/A");
                                table.Cell().Element(CellStyle).Text(payment.Installment?.LoanId.ToString() ?? "N/A");
                                table.Cell().Element(CellStyle).Text(payment.Installment?.InstallmentNumber.ToString() ?? "N/A");
                                table.Cell().Element(CellStyle).AlignRight().Text("C$" + payment.PaidAmount);
                                table.Cell().Element(CellStyle).Text(payment.Reference ?? "N/A");
                                table.Cell().Element(CellStyle).Text(payment.Collector?.UserName ?? "N/A");
                            }

                            // Totales
                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(6).Element(CellFooterStyle).Text("Total:");
                                footer.Cell().ColumnSpan(3)
                                    .Element(CellFooterStyle)
                                    .AlignRight()
                                    .Text("C$" + _payments.Sum(p => p.PaidAmount));
                            });
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

    private IContainer CellFooterStyle(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(5)
            .PaddingHorizontal(5)
            .Background(Colors.Grey.Lighten4);
    }
}