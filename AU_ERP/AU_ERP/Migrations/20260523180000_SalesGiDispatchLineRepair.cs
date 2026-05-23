using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <summary>
    /// Re-open dispatch on sales GIs that were bulk-marked Sent without stock issue, and sync lines where send already posted inventory.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260523180000_SalesGiDispatchLineRepair")]
    public partial class SalesGiDispatchLineRepair : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Real sends: inventory was deducted but line qty may be stale — align issued/remaining with required.
            migrationBuilder.Sql("""
                UPDATE l
                SET IssuedQty = l.RequiredQty,
                    RemainingQty = 0
                FROM SalesGoodsIssueDocumentLines l
                INNER JOIN SalesGoodsIssueDocuments d ON d.Id = l.SalesGoodsIssueDocumentId
                WHERE d.DispatchStatus = N'Sent'
                  AND d.DispatchSentByUserId IS NOT NULL
                  AND l.RemainingQty > 0.0001;
                """);

            // Migration-only Sent (never dispatched via Stock GI): allow Send to deduct stock properly.
            migrationBuilder.Sql("""
                UPDATE d
                SET DispatchStatus = N'Pending',
                    DispatchSentAt = NULL,
                    DispatchSentByUserId = NULL
                FROM SalesGoodsIssueDocuments d
                WHERE d.DispatchStatus = N'Sent'
                  AND d.Status = N'Pending'
                  AND d.DispatchSentByUserId IS NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM SalesGoodsIssueDocumentLines l
                      WHERE l.SalesGoodsIssueDocumentId = d.Id
                        AND l.IssuedQty > 0.0001);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data repair is not reversed.
        }
    }
}
