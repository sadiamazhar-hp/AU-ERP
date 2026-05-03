using AU_ERP.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AU_ERP.Services;

/// <summary>Renders company name and phone on PDFs when set in Company info.</summary>
public static class DocumentPdfCompanyHeader
{
    public static void Compose(ColumnDescriptor column, CompanyPdfHeader companyHeader)
    {
        if (!companyHeader.HasAny)
            return;

        if (companyHeader.CompanyName is { Length: > 0 } cn)
            column.Item().Text(cn).FontSize(11).SemiBold().FontColor(Colors.Grey.Darken4);

        if (companyHeader.PhoneNumber is { Length: > 0 } ph)
            column.Item().PaddingTop(1).Text($"Tel: {ph}").FontSize(9).FontColor(Colors.Grey.Darken2);

        column.Item().PaddingBottom(6);
    }
}
