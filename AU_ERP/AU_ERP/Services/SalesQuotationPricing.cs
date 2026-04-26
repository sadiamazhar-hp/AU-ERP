using System.Text.Json;
using AU_ERP.Models;

namespace AU_ERP.Services;

public static class SalesQuotationPricing
{
    public static IReadOnlyList<int> ParseIdList(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<int>();
        var r = new List<int>();
        foreach (var p in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(p, out var v) && v > 0) r.Add(v);
        }
        return r;
    }

    public static string JoinIds(IEnumerable<int> ids) => string.Join(",", ids);

    /// <summary>Parse {"1":"10.5","2":"0"} from posted JSON.</summary>
    public static IReadOnlyDictionary<int, decimal> ParseChargeValuesJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<int, decimal>();
        try
        {
            var d = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json.Trim());
            if (d == null) return new Dictionary<int, decimal>();
            var m = new Dictionary<int, decimal>();
            foreach (var kv in d)
            {
                if (!int.TryParse(kv.Key, out var id) || id <= 0) continue;
                m[id] = ParseDecimalElement(kv.Value);
            }
            return m;
        }
        catch
        {
            return new Dictionary<int, decimal>();
        }
    }

    private static decimal ParseDecimalElement(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Number) return el.GetDecimal();
        if (el.ValueKind == JsonValueKind.String
            && decimal.TryParse(el.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var s))
            return s;
        return 0m;
    }

    /// <summary>Apply a user-entered value using charge master: numeric = flat amount, percentage = % of <paramref name="running"/> (not compounded inside one step for percentage base = running before this charge).</summary>
    public static decimal ApplyUserChargeValue(Charge ch, decimal running, decimal userValue)
    {
        if (ch.ValueType == Charge.TypeNumeric)
        {
            var abs = Math.Abs(userValue);
            return ch.Sign == Charge.SignAdd ? running + abs : running - abs;
        }

        var pct = Math.Max(0, userValue);
        var delta = Math.Round(running * (pct / 100m), 4, MidpointRounding.AwayFromZero);
        return ch.Sign == Charge.SignAdd ? running + delta : running - delta;
    }

    /// <summary>Resolve effective percentage: user value, else <see cref="Charge.DefaultPercent"/>, else 0.</summary>
    public static decimal ResolvePercentageInput(Charge ch, IReadOnlyDictionary<int, decimal> values, int chargeId, bool hasKey)
    {
        if (hasKey && values.TryGetValue(chargeId, out var v)) return v;
        if (ch.DefaultPercent is { } p) return p;
        return 0m;
    }

    /// <summary>Resolve effective numeric: user value, else 0.</summary>
    public static decimal ResolveNumericInput(IReadOnlyDictionary<int, decimal> values, int chargeId, bool hasKey)
    {
        if (hasKey && values.TryGetValue(chargeId, out var v)) return v;
        return 0m;
    }

    /// <summary>At quantity × unit (optionally with line discount via <paramref name="discPct"/>; UI uses 0), apply item charge columns in order. Returns (net / sub, line total with charges).</summary>
    public static (decimal subAfterDisc, decimal lineTotal) ComputeLineWithChargeValues(
        IReadOnlyList<int> itemColumnOrder,
        IReadOnlyDictionary<int, Charge> byId,
        IReadOnlyDictionary<int, decimal> lineValues,
        decimal qty, decimal unitPrice, decimal discPct)
    {
        var baseAmt = Math.Round(qty * unitPrice, 4, MidpointRounding.AwayFromZero);
        var afterDisc = discPct is > 0 and <= 100
            ? Math.Round(baseAmt * (1m - discPct / 100m), 4, MidpointRounding.AwayFromZero)
            : baseAmt;
        var running = afterDisc;
        foreach (var cid in itemColumnOrder)
        {
            if (!byId.TryGetValue(cid, out var ch)) continue;
            var has = lineValues.ContainsKey(cid);
            decimal eff;
            if (ch.ValueType == Charge.TypePercentage)
            {
                eff = ResolvePercentageInput(ch, lineValues, cid, has);
                if (eff == 0) continue;
            }
            else
            {
                eff = ResolveNumericInput(lineValues, cid, has);
                if (eff == 0) continue;
            }
            running = ApplyUserChargeValue(ch, running, eff);
        }
        running = Math.Round(running, 4, MidpointRounding.AwayFromZero);
        return (afterDisc, running);
    }

    /// <summary>Apply document-level charge values in order to document subtotal (sum of line totals).</summary>
    public static decimal ApplyQuotationChargeValues(
        IReadOnlyList<int> docChargeOrder,
        IReadOnlyDictionary<int, Charge> byId,
        IReadOnlyDictionary<int, decimal> docValues,
        decimal documentSubtotal)
    {
        var running = documentSubtotal;
        foreach (var cid in docChargeOrder)
        {
            if (!byId.TryGetValue(cid, out var ch)) continue;
            var has = docValues.ContainsKey(cid);
            decimal eff;
            if (ch.ValueType == Charge.TypePercentage)
            {
                eff = ResolvePercentageInput(ch, docValues, cid, has);
                if (eff == 0) continue;
            }
            else
            {
                eff = ResolveNumericInput(docValues, cid, has);
                if (eff == 0) continue;
            }
            running = ApplyUserChargeValue(ch, running, eff);
        }
        return Math.Round(running, 4, MidpointRounding.AwayFromZero);
    }

    public static bool SchemaHasBaseCharge(IReadOnlyDictionary<int, Charge> byId) =>
        byId.Values.Any(c => c.Symbol.Equals("BASE", StringComparison.OrdinalIgnoreCase));

    public static bool IsBaseCharge(Charge c) => c.Symbol.Equals("BASE", StringComparison.OrdinalIgnoreCase);

    public static bool IsDiscountCharge(Charge c) =>
        c.ValueType == Charge.TypePercentage && c.Sign == Charge.SignSubtract;

    public static bool IsTaxCharge(Charge c) =>
        c.ValueType == Charge.TypePercentage && c.Sign == Charge.SignAdd;

    public static (decimal subAfterDisc, decimal tax, decimal lineTotal) ComputeLineAmounts(
        IReadOnlyCollection<int> itemApplied,
        IReadOnlyDictionary<int, Charge> byId,
        int? lineTaxChargeId,
        decimal qty, decimal unitPrice, decimal discPct)
    {
        var applied = new HashSet<int>(itemApplied);
        var hasBase = !SchemaHasBaseCharge(byId) || byId.Values.Any(c => IsBaseCharge(c) && applied.Contains(c.Id));
        if (!hasBase) return (0, 0, 0);
        var baseAmt = Math.Round(qty * unitPrice, 4, MidpointRounding.AwayFromZero);
        var discApplies = byId.Values.Any(c => IsDiscountCharge(c) && applied.Contains(c.Id));
        var afterDisc = discApplies
            ? Math.Round(baseAmt * (1m - (discPct / 100m)), 4, MidpointRounding.AwayFromZero)
            : baseAmt;
        decimal tax = 0;
        if (lineTaxChargeId is int taxId && taxId > 0
            && applied.Contains(taxId)
            && byId.TryGetValue(taxId, out var tCh)
            && IsTaxCharge(tCh)
            && tCh.DefaultPercent is decimal p)
        {
            tax = Math.Round(afterDisc * (p / 100m), 4, MidpointRounding.AwayFromZero);
        }
        var lineTotal = Math.Round(afterDisc + tax, 4, MidpointRounding.AwayFromZero);
        return (afterDisc, tax, lineTotal);
    }
}
