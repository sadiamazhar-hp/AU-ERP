using System.Globalization;
using AU_ERP.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AU_ERP.Services;

public static class DeliveryChallanPdfService
{
    public static byte[] BuildPdf(DeliveryChallan d, IReadOnlyList<DeliveryChallanItem> items)
    {
        var inv = CultureInfo.InvariantCulture;
        var lines = (items ?? Array.Empty<DeliveryChallanItem>()).OrderBy(x => x.Id).ToList();

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
                        c.Item().Text("DELIVERY CHALLAN").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken3);
                        c.Item().PaddingTop(4).Text(text =>
                        {
                            text.Span("No. ").FontSize(10).FontColor(Colors.Grey.Darken2);
                            text.Span(d.DeliveryChallanNumber).FontSize(11).SemiBold();
                        });
                    });
                    row.ConstantItem(130).AlignRight().Column(cc =>
                    {
                        cc.Item().Text($"Date: {d.DocumentDate:dd-MMM-yyyy}").FontSize(9);
                        cc.Item().Text($"Type: {d.DeliveryType}").FontSize(9);
                    });
                });

                page.Content().PaddingTop(12).Column(main =>
                {
                    main.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(block =>
                    {
                        block.Item().Text("Ship-to").FontSize(8).FontColor(Colors.Grey.Darken2);
                        block.Item().Text((d.ShipToDisplayName ?? "—").Trim()).SemiBold();
                        if (d.Plant != null)
                        {
                            block.Item().PaddingTop(4).Text("Plant").FontSize(8).FontColor(Colors.Grey.Darken2);
                            block.Item().Text(d.Plant.PlantName ?? d.Plant.PlantID).FontSize(9);
                        }
                        if (!string.IsNullOrWhiteSpace(d.ReferenceSalesOrderNumber ?? d.SalesOrder?.SalesOrderNumber))
                        {
                            var so = d.ReferenceSalesOrderNumber ?? d.SalesOrder?.SalesOrderNumber;
                            block.Item().PaddingTop(4).Text("Reference sales order").FontSize(8).FontColor(Colors.Grey.Darken2);
                            block.Item().Text(so!).FontSize(9);
                        }
                    });

                    main.Item().PaddingTop(10).Text("Line items").FontSize(11).SemiBold();
                    main.Item().PaddingTop(4).Table(t =>
                    {
                        t.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(0.9f);
                            cols.RelativeColumn(1.1f);
                            cols.RelativeColumn(1.4f);
                            cols.RelativeColumn(0.55f);
                            cols.RelativeColumn(0.45f);
                            cols.RelativeColumn(0.65f);
                        });

                        t.Header(h =>
                        {
                            static IContainer CellStyle(IContainer c) =>
                                c.DefaultTextStyle(x => x.FontSize(8)).PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Medium);
                            h.Cell().Element(CellStyle).Text("Ref. SO");
                            h.Cell().Element(CellStyle).Text("Material");
                            h.Cell().Element(CellStyle).Text("Description");
                            h.Cell().Element(CellStyle).AlignRight().Text("Qty");
                            h.Cell().Element(CellStyle).Text("UoM");
                            h.Cell().Element(CellStyle).Text("Batch / lot");
                        });

                        static IContainer B(IContainer c) =>
                            c.DefaultTextStyle(x => x.FontSize(8)).PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

                        foreach (var li in lines)
                        {
                            var uom = li.QuantityUom?.Code ?? "—";
                            t.Cell().Element(B).Text((li.ReferenceSalesOrderNumber ?? "—").Trim());
                            t.Cell().Element(B).Text(li.MaterialNumber);
                            t.Cell().Element(B).Text((li.MaterialDescription ?? "—").Trim());
                            t.Cell().Element(B).AlignRight().Text(li.DeliveryQuantity.ToString("0.####", inv));
                            t.Cell().Element(B).Text(uom);
                            t.Cell().Element(B).Text((li.Batch ?? "—").Trim());
                        }
                    });
                });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.DefaultTextStyle(t => t.FontSize(7).FontColor(Colors.Grey.Medium));
                        x.Span("This delivery challan was generated from AU ERP.");
                    });
            });
        }).GeneratePdf();
    }
}
