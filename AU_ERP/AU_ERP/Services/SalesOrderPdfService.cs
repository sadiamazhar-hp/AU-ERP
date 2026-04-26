using System.Globalization;
using AU_ERP.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AU_ERP.Services;

public static class SalesOrderPdfService
{
    public static byte[] BuildPdf(SalesOrder o, IReadOnlyList<SalesOrderItem> items)
    {
        var inv = CultureInfo.InvariantCulture;
        var lines = (items ?? Array.Empty<SalesOrderItem>()).OrderBy(x => x.Id).ToList();
        var total = lines.Sum(x => x.NetPrice);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("SALES ORDER").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken3);
                        c.Item().PaddingTop(4).Text(text =>
                        {
                            text.Span("No. ").FontSize(10).FontColor(Colors.Grey.Darken2);
                            text.Span(o.SalesOrderNumber).FontSize(11).SemiBold();
                        });
                    });
                    row.ConstantItem(120).AlignRight().Column(c =>
                    {
                        c.Item().Text($"Date: {o.OrderDate:dd-MMM-yyyy}").FontSize(9);
                        c.Item().Text($"Status: {o.Status}").FontSize(9);
                    });
                });

                page.Content().PaddingTop(12).Column(main =>
                {
                    main.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(block =>
                    {
                        block.Item().Text("Customer (sold-to)").FontSize(8).FontColor(Colors.Grey.Darken2);
                        block.Item().Text((o.CustomerName ?? "—").Trim()).SemiBold();
                        if (!string.IsNullOrWhiteSpace(o.ShipToAddress))
                        {
                            block.Item().PaddingTop(2).Text("Ship-to:").FontSize(8).FontColor(Colors.Grey.Darken2);
                            block.Item().Text(o.ShipToAddress).FontSize(9);
                        }
                    });

                    main.Item().PaddingTop(10).Text("Line items").FontSize(11).SemiBold();
                    main.Item().PaddingTop(4).Table(t =>
                    {
                        t.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1.2f);
                            cols.RelativeColumn(2.2f);
                            cols.RelativeColumn(0.55f);
                            cols.RelativeColumn(0.45f);
                            cols.RelativeColumn(0.65f);
                            cols.RelativeColumn(0.65f);
                        });

                        t.Header(h =>
                        {
                            static IContainer CellStyle(IContainer c) =>
                                c.DefaultTextStyle(x => x.FontSize(8)).PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Medium);
                            h.Cell().Element(CellStyle).Text("Material");
                            h.Cell().Element(CellStyle).Text("Description");
                            h.Cell().Element(CellStyle).AlignRight().Text("Qty");
                            h.Cell().Element(CellStyle).Text("UoM");
                            h.Cell().Element(CellStyle).AlignRight().Text("Unit price");
                            h.Cell().Element(CellStyle).AlignRight().Text("Line total");
                        });

                        static IContainer B(IContainer c) =>
                            c.DefaultTextStyle(x => x.FontSize(8)).PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

                        foreach (var li in lines)
                        {
                            var uom = li.QuantityUom?.Code ?? "—";
                            t.Cell().Element(B).Text(li.MaterialNumber);
                            t.Cell().Element(B).Text((li.MaterialDescription ?? "—").Trim());
                            t.Cell().Element(B).AlignRight().Text(li.OrderQuantity.ToString("0.####", inv));
                            t.Cell().Element(B).Text(uom);
                            t.Cell().Element(B).AlignRight().Text(li.UnitPrice.ToString("N2", inv));
                            t.Cell().Element(B).AlignRight().Text(li.NetPrice.ToString("N2", inv));
                        }
                    });

                    main.Item().PaddingTop(8).Row(r =>
                    {
                        r.RelativeItem();
                        r.ConstantItem(120).Column(col =>
                        {
                            col.Item().Row(rr =>
                            {
                                rr.RelativeItem().Text("Grand total (PKR)").FontSize(9);
                                rr.ConstantItem(72).AlignRight().Text(total.ToString("N2", inv)).SemiBold();
                            });
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(o.Remarks))
                    {
                        main.Item().PaddingTop(12).Text("Remarks").FontSize(9).SemiBold();
                        main.Item().Text(o.Remarks).FontSize(8);
                    }
                });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.DefaultTextStyle(t => t.FontSize(7).FontColor(Colors.Grey.Medium));
                        x.Span("This sales order was generated from AU ERP.");
                    });
            });
        }).GeneratePdf();
    }
}
