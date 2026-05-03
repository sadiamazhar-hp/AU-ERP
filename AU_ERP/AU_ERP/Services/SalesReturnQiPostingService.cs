using System.Data;
using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

public sealed class SalesReturnQiPostingService
{
    private readonly AppDbContext _db;
    private readonly SalesReturnQiBomService _bom;
    private const decimal Epsilon = 0.0001m;

    public SalesReturnQiPostingService(AppDbContext db, SalesReturnQiBomService bom)
    {
        _db = db;
        _bom = bom;
    }

    public sealed record PostResult(bool Success, string Message);

    public sealed record QiDispositionInput(
        int LineId,
        decimal QtyBackToStock,
        decimal QtyConvertToRaw,
        decimal QtyScrap);

    public async Task<PostResult> CompleteAsync(int qiId, IReadOnlyList<QiDispositionInput> dispositions, CancellationToken ct = default)
    {
        if (qiId <= 0)
            return new(false, "Invalid quality inspection.");
        if (dispositions == null || dispositions.Count == 0)
            return new(false, "No line dispositions submitted.");

        var strategy = _db.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync<PostResult>(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                    .ConfigureAwait(false);
                try
                {
                    var qi = await _db.SalesReturnQualityInspections
                        .Include(q => q.Lines).ThenInclude(l => l.QuantityUom)
                        .Include(q => q.Lines).ThenInclude(l => l.SalesReturnOrderLine)!.ThenInclude(rol => rol!.SalesInvoiceLine)
                        .Include(q => q.SalesReturnOrder)!.ThenInclude(r => r!.SalesInvoice!)
                        .FirstOrDefaultAsync(q => q.Id == qiId, ct)
                        .ConfigureAwait(false);
                    if (qi == null)
                    {
                        await tx.RollbackAsync(ct).ConfigureAwait(false);
                        return new(false, "Quality inspection not found.");
                    }

                    if (!string.Equals(qi.Status, SalesReturnQualityInspection.StatusPending, StringComparison.OrdinalIgnoreCase))
                    {
                        await tx.RollbackAsync(ct).ConfigureAwait(false);
                        return new(false, "This inspection is not pending.");
                    }

                    Dictionary<int, QiDispositionInput> byLineId;
                    try
                    {
                        byLineId = dispositions.ToDictionary(d => d.LineId, d => d);
                    }
                    catch (ArgumentException)
                    {
                        await tx.RollbackAsync(ct).ConfigureAwait(false);
                        return new(false, "Duplicate line entries in the form.");
                    }

                    if (byLineId.Count != qi.Lines.Count || qi.Lines.Any(l => !byLineId.ContainsKey(l.Id)))
                    {
                        await tx.RollbackAsync(ct).ConfigureAwait(false);
                        return new(false, "Disposition data does not match inspection lines.");
                    }

                    foreach (var ln in qi.Lines)
                    {
                        var row = byLineId[ln.Id];
                        ln.QtyBackToStock = row.QtyBackToStock;
                        ln.QtyConvertToRaw = row.QtyConvertToRaw;
                        ln.QtyScrap = row.QtyScrap;
                        ln.ConvertTargetMaterialNumber = null;
                    }

                    var plantId = (qi.PlantId ?? "").Trim();
                    if (plantId.Length == 0)
                    {
                        await tx.RollbackAsync(ct).ConfigureAwait(false);
                        return new(false, "Plant is not set on this inspection; cannot post inventory.");
                    }

                    if (!await _db.PlantsSamples.AsNoTracking().AnyAsync(p => p.PlantID == plantId, ct).ConfigureAwait(false))
                    {
                        await tx.RollbackAsync(ct).ConfigureAwait(false);
                        return new(false, "Plant is not configured.");
                    }

                    var soItemIds = qi.Lines
                        .Select(l => l.SalesReturnOrderLine?.SalesInvoiceLine?.SourceSalesOrderItemId)
                        .Where(id => id is > 0)
                        .Cast<int>()
                        .Distinct()
                        .ToList();
                    var soItemsById = soItemIds.Count == 0
                        ? new Dictionary<int, SalesOrderItem>()
                        : await _db.SalesOrderItems.AsNoTracking()
                            .Where(i => soItemIds.Contains(i.Id))
                            .ToDictionaryAsync(i => i.Id, ct)
                            .ConfigureAwait(false);

                    foreach (var ln in qi.Lines)
                    {
                        var sum = ln.QtyBackToStock + ln.QtyConvertToRaw + ln.QtyScrap;
                        if (ln.QtyBackToStock < 0 || ln.QtyConvertToRaw < 0 || ln.QtyScrap < 0)
                        {
                            await tx.RollbackAsync(ct).ConfigureAwait(false);
                            return new(false, $"Quantities cannot be negative for material {ln.MaterialNumber}.");
                        }

                        if (Math.Abs(sum - ln.QuantityReturned) > Epsilon)
                        {
                            await tx.RollbackAsync(ct).ConfigureAwait(false);
                            return new(false,
                                $"Disposition must sum to returned quantity for {ln.MaterialNumber} (returned {ln.QuantityReturned:0.####}, sum {sum:0.####}).");
                        }

                        if (ln.QtyBackToStock > Epsilon)
                        {
                            var (_, gErr) = TryResolveGradeFromSalesOrder(ln, soItemsById);
                            if (gErr != null)
                            {
                                await tx.RollbackAsync(ct).ConfigureAwait(false);
                                return new(false, gErr);
                            }
                        }

                        if (ln.QtyConvertToRaw > Epsilon)
                        {
                            if (ln.QuantityUomId is not int fgUomId || fgUomId <= 0)
                            {
                                await tx.RollbackAsync(ct).ConfigureAwait(false);
                                return new(false, $"Line UOM is required for convert-to-raw on {ln.MaterialNumber}.");
                            }

                            var (exOk, exErr, _) = await _bom.ExplodeRohForFgConvertAsync(
                                    ln.MaterialNumber,
                                    ln.QtyConvertToRaw,
                                    fgUomId,
                                    ct)
                                .ConfigureAwait(false);
                            if (!exOk)
                            {
                                await tx.RollbackAsync(ct).ConfigureAwait(false);
                                return new(false, exErr ?? "BOM explosion failed for convert quantity.");
                            }
                        }
                    }

                    var inv = qi.SalesReturnOrder?.SalesInvoice;
                    if (inv == null)
                    {
                        await tx.RollbackAsync(ct).ConfigureAwait(false);
                        return new(false, "Linked invoice not found.");
                    }

                    var now = DateTime.UtcNow;
                    foreach (var ln in qi.Lines.OrderBy(l => l.Id))
                    {
                        var batchKey = StockInventoryBatchKey.Normalize(ln.BatchNumber);
                        var (gradeForBack, _) = ln.QtyBackToStock > Epsilon
                            ? TryResolveGradeFromSalesOrder(ln, soItemsById)
                            : (StockInventoryGradeCodes.FirstQuality, (string?)null);

                        if (ln.QtyBackToStock > Epsilon)
                        {
                            if (ln.QuantityUomId is not int uomId || uomId <= 0)
                            {
                                await tx.RollbackAsync(ct).ConfigureAwait(false);
                                return new(false, $"Line UOM is required for back-to-stock on {ln.MaterialNumber}.");
                            }

                            await UpsertStockAsync(
                                    plantId,
                                    ln.MaterialNumber,
                                    uomId,
                                    gradeForBack,
                                    ln.QtyBackToStock,
                                    batchKey,
                                    now,
                                    ct)
                                .ConfigureAwait(false);
                        }

                        if (ln.QtyScrap > Epsilon)
                        {
                            if (ln.QuantityUomId is not int uomId2 || uomId2 <= 0)
                            {
                                await tx.RollbackAsync(ct).ConfigureAwait(false);
                                return new(false, $"Line UOM is required for scrap on {ln.MaterialNumber}.");
                            }

                            await UpsertStockAsync(
                                    plantId,
                                    ln.MaterialNumber,
                                    uomId2,
                                    StockInventoryGradeCodes.Scrap,
                                    ln.QtyScrap,
                                    batchKey,
                                    now,
                                    ct)
                                .ConfigureAwait(false);
                        }

                        if (ln.QtyConvertToRaw > Epsilon)
                        {
                            if (ln.QuantityUomId is not int fgUom || fgUom <= 0)
                            {
                                await tx.RollbackAsync(ct).ConfigureAwait(false);
                                return new(false, $"Line UOM is required for convert on {ln.MaterialNumber}.");
                            }

                            var (exOk, exErr, exploded) = await _bom.ExplodeRohForFgConvertAsync(
                                    ln.MaterialNumber,
                                    ln.QtyConvertToRaw,
                                    fgUom,
                                    ct)
                                .ConfigureAwait(false);
                            if (!exOk || exploded == null)
                            {
                                await tx.RollbackAsync(ct).ConfigureAwait(false);
                                return new(false, exErr ?? "BOM explosion failed.");
                            }

                            foreach (var comp in exploded)
                            {
                                await UpsertStockAsync(
                                        plantId,
                                        comp.ComponentMaterialNumber,
                                        comp.BaseUomId,
                                        StockInventoryGradeCodes.FirstQuality,
                                        comp.QtyInBaseUom,
                                        "",
                                        now,
                                        ct)
                                    .ConfigureAwait(false);
                            }
                        }
                    }

                    qi.Status = SalesReturnQualityInspection.StatusCompleted;
                    qi.CompletedAt = now;
                    inv.Status = SalesInvoice.StatusReturned;
                    await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                    await tx.CommitAsync(ct).ConfigureAwait(false);
                    return new(true, "Quality inspection completed and inventory updated.");
                }
                catch
                {
                    await tx.RollbackAsync(ct).ConfigureAwait(false);
                    throw;
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new(false, ex.InnerException?.Message ?? ex.Message);
        }
    }

    private static (string grade, string? error) TryResolveGradeFromSalesOrder(
        SalesReturnQualityInspectionLine ln,
        IReadOnlyDictionary<int, SalesOrderItem> soItemsById)
    {
        var sil = ln.SalesReturnOrderLine?.SalesInvoiceLine;
        if (sil?.SourceSalesOrderItemId is not int soiId || soiId <= 0)
        {
            return (StockInventoryGradeCodes.FirstQuality,
                $"Cannot resolve sales price grade for material {ln.MaterialNumber}: invoice line has no source sales order item.");
        }

        if (!soItemsById.TryGetValue(soiId, out var soi))
        {
            return (StockInventoryGradeCodes.FirstQuality,
                $"Cannot resolve sales price grade for material {ln.MaterialNumber}: sales order item {soiId} was not found.");
        }

        return (DeliveryChallanStockService.InventoryGradeFromSalesGrade(soi.SalesPriceGrade), null);
    }

    private async Task UpsertStockAsync(
        string plantId,
        string materialNumber,
        int quantityUomId,
        string grade,
        decimal qty,
        string batchKey,
        DateTime now,
        CancellationToken ct)
    {
        var mat = await _db.CreateMaterialMaster.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MaterialNumber == materialNumber, ct)
            .ConfigureAwait(false);
        if (mat == null)
            throw new InvalidOperationException($"Material '{materialNumber}' not found.");

        var stdCost = await InventoryStandardCostService.ResolveStandardCostPerUomAsync(
                _db,
                materialNumber,
                quantityUomId,
                ct,
                grade,
                plantId)
            .ConfigureAwait(false);

        var status = StockInventoryLine.StatusActive;
        var batchNorm = StockInventoryBatchKey.Normalize(batchKey);
        var line = await _db.StockInventoryLines
            .FirstOrDefaultAsync(
                s => s.PlantID == plantId
                     && s.MaterialNumber == materialNumber
                     && s.QuantityUomId == quantityUomId
                     && s.Status == status
                     && s.Grade == grade
                     && (s.BatchOrLot ?? "") == batchNorm,
                ct)
            .ConfigureAwait(false);

        if (line == null)
        {
            _db.StockInventoryLines.Add(new StockInventoryLine
            {
                MaterialNumber = materialNumber,
                PlantID = plantId,
                QuantityUomId = quantityUomId,
                Status = status,
                Grade = grade,
                Quantity = qty,
                StandardCostPerUom = stdCost,
                StockValue = Math.Round(qty * stdCost, 2, MidpointRounding.AwayFromZero),
                BatchOrLot = batchNorm.Length == 0 ? "" : batchNorm,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            line.Quantity += qty;
            line.StockValue = Math.Round(line.Quantity * line.StandardCostPerUom, 2, MidpointRounding.AwayFromZero);
            if (string.IsNullOrWhiteSpace(line.BatchOrLot) && batchNorm.Length > 0)
                line.BatchOrLot = batchNorm;
            line.UpdatedAt = now;
        }
    }
}
