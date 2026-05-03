using System.Globalization;
using AU_ERP.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AU_ERP.Services;

public sealed class GoodReceiptPdfService
{
    private readonly CompanyInfoService _companyInfo;

    public GoodReceiptPdfService(CompanyInfoService companyInfo) => _companyInfo = companyInfo;

    public async Task<byte[]> BuildPdfAsync(GoodReceiptDocument doc, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var companyHeader = await _companyInfo.GetPdfHeaderAsync(ct).ConfigureAwait(false);

        var inv = CultureInfo.InvariantCulture;
        var po = doc.ProductionOrder;
        var docNo = (doc.DocumentNumber ?? "").Trim();
        var batch = (doc.BatchNo ?? "").Trim();

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
                        DocumentPdfCompanyHeader.Compose(c, companyHeader);
                        c.Item().Text("GOOD RECEIPT").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken3);
                        c.Item().PaddingTop(4).Text(text =>
                        {
                            text.Span("Doc No. ").FontSize(10).FontColor(Colors.Grey.Darken2);
                            text.Span(docNo.Length == 0 ? "—" : docNo).FontSize(11).SemiBold();
                        });
                    });
                    row.ConstantItem(160).AlignRight().Column(cc =>
                    {
                        cc.Item().Text($"Date: {doc.DocumentDate:dd-MMM-yyyy}").FontSize(9);
                        cc.Item().Text($"Status: {(doc.IsPosted ? "Posted" : "Draft")}").FontSize(9);
                    });
                });

                page.Content().PaddingTop(12).Column(main =>
                {
                    main.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(block =>
                    {
                        block.Item().Text("Production order").FontSize(8).FontColor(Colors.Grey.Darken2);
                        block.Item().Text(po?.ProductionNumber.ToString(inv) ?? po?.Id.ToString(inv) ?? "—").SemiBold();

                        block.Item().PaddingTop(4).Text("Material").FontSize(8).FontColor(Colors.Grey.Darken2);
                        block.Item().Text($"{po?.FinishedMaterialNumber ?? "—"}  {(po?.FinishedMaterial?.Description ?? "").Trim()}".Trim()).FontSize(9);

                        block.Item().PaddingTop(4).Text("Batch No").FontSize(8).FontColor(Colors.Grey.Darken2);
                        block.Item().Text(batch.Length == 0 ? "—" : batch).FontSize(9);
                    });

                    main.Item().PaddingTop(10).Text("Quantities").FontSize(11).SemiBold();

                    main.Item().PaddingTop(4).Table(t =>
                    {
                        t.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1.2f);
                            cols.RelativeColumn(0.8f);
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.DefaultTextStyle(x => x.FontSize(9).SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Medium);

                        static IContainer Cell(IContainer c) =>
                            c.DefaultTextStyle(x => x.FontSize(9)).PaddingVertical(4);

                        t.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("Type");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                        });

                        void Row(string label, decimal qty)
                        {
                            t.Cell().Element(Cell).Text(label);
                            t.Cell().Element(Cell).AlignRight().Text(qty.ToString("0.####", inv));
                        }

                        Row("Produced Qty", doc.ProducedQty);
                        Row("Grade A", doc.QtyFirstQuality);
                        Row("Grade B", doc.QtySecondQuality);
                        Row("Grade C", doc.QtyThirdQuality);
                        Row("Scrap", doc.RejectedScrapQty);
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Generated ").FontSize(8).FontColor(Colors.Grey.Darken2);
                    x.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm", inv)).FontSize(8);
                });
            });
        }).GeneratePdf();

        return bytes;
    }
}

