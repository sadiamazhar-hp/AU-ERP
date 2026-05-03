namespace AU_ERP.Services;

/// <summary>Normalizes batch/lot for stock line identity (empty string = no batch).</summary>
public static class StockInventoryBatchKey
{
    public static string Normalize(string? batchOrLot) =>
        string.IsNullOrWhiteSpace(batchOrLot) ? "" : batchOrLot.Trim();
}
