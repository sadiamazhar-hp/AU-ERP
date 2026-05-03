using System.Globalization;
using AU_ERP.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AU_ERP.Services;

public sealed class SalesReturnCreditMemoPdfService
{
    public Task<byte[]> BuildPdfAsync(SalesReturnCreditMemo memo, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var inv = CultureInfo.InvariantCulture;
        var docNo = (memo.DocumentNumber ?? string.Empty).Trim();
        var dealer = memo.DealerDisplayName ?? memo.DealerBusinessPartnerId ?? "—";

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("CREDIT MEMO").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken3);
                        c.Item().PaddingTop(4).Text(text =>
                        {
                            text.Span("Document no. ").FontSize(10).FontColor(Colors.Grey.Darken2);
                            text.Span(docNo.Length == 0 ? "—" : docNo).FontSize(11).SemiBold();
                        });
                    });
                    row.ConstantItem(220).AlignRight().Column(cc =>
                    {
                        cc.Item().Text($"Date: {memo.DocumentDate:dd-MMM-yyyy}").FontSize(9);
                        cc.Item().Text($"Return order: {memo.ReturnOrderDocumentNumber}").FontSize(9);
                        cc.Item().Text($"Invoice: {memo.InvoiceDocumentNumber}").FontSize(9);
                    });
                });

                page.Content().PaddingTop(12).Column(main =>
                {
                    main.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(block =>
                    {
                        block.Item().Text("Dealer").FontSize(8).FontColor(Colors.Grey.Darken2);
                        block.Item().Text(dealer).SemiBold().FontSize(10);
                    });

                    main.Item().PaddingTop(10).Text("Credit lines (returned quantity)").FontSize(11).SemiBold();

                    main.Item().PaddingTop(4).Table(t =>
                    {
                        t.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(0.45f);
                            cols.RelativeColumn(1.4f);
                            cols.RelativeColumn(0.75f);
                            cols.RelativeColumn(0.55f);
                            cols.RelativeColumn(0.55f);
                            cols.RelativeColumn(0.65f);
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.DefaultTextStyle(x => x.FontSize(9).SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Medium);
                        static IContainer Cell(IContainer c) =>
                            c.DefaultTextStyle(x => x.FontSize(9)).PaddingVertical(4);

                        t.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("#");
                            h.Cell().Element(HeaderCell).Text("Material / description");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Returned qty");
                            h.Cell().Element(HeaderCell).Text("UOM");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Unit price");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Credit");
                        });

                        foreach (var line in memo.Lines.OrderBy(x => x.LineNo))
                        {
                            var uom = line.QuantityUom?.Code ?? "—";
                            var desc = string.IsNullOrWhiteSpace(line.MaterialDescription) ? "" : $" — {line.MaterialDescription}";
                            t.Cell().Element(Cell).Text(line.LineNo.ToString(inv));
                            t.Cell().Element(Cell).Text($"{line.MaterialNumber}{desc}");
                            t.Cell().Element(Cell).AlignRight().Text(line.QuantityReturned.ToString("0.####", inv));
                            t.Cell().Element(Cell).Text(uom);
                            t.Cell().Element(Cell).AlignRight().Text(line.UnitPrice.ToString("N4", inv));
                            t.Cell().Element(Cell).AlignRight().Text(line.LineCreditAmount.ToString("N4", inv));
                        }
                    });

                    main.Item().PaddingTop(12).AlignRight().Text(text =>
                    {
                        text.Span("Total credit: ").FontSize(11).SemiBold();
                        text.Span(memo.GrandTotalCredit.ToString("N4", inv)).FontSize(12).SemiBold();
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
                    x.Span("Generated from return order · ");
                    x.Span(DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm 'UTC'", inv));
                });
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }
}
