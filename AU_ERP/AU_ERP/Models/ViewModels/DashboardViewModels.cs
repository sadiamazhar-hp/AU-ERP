namespace AU_ERP.Models.ViewModels;

/// <summary>Main dashboard: KPIs and chart payloads per module; sections shown by department access.</summary>
public sealed class DashboardPageVm
{
    public string UserDisplayName { get; init; } = "";
    public IReadOnlyList<string> DepartmentNames { get; init; } = Array.Empty<string>();
    public string TodayLabel { get; init; } = "";

    public bool ShowAdmin { get; init; }
    public bool ShowStore { get; init; }
    public bool ShowSales { get; init; }
    public bool ShowProduction { get; init; }
    public bool HasAnyModule { get; init; }

    public AdminModuleStats? Admin { get; init; }
    public StoreModuleStats? Store { get; init; }
    public SalesModuleStats? Sales { get; init; }
    public ProductionModuleStats? Production { get; init; }
}

public sealed class AdminModuleStats
{
    public int MaterialCount { get; init; }
    public int BusinessPartnerCount { get; init; }
    public int BomCount { get; init; }
    public int WorkCentreCount { get; init; }
    public int RoutingCount { get; init; }
    public int UserCount { get; init; }
    /// <summary>Labels for horizontal bar: Materials, BPs, BOMs, Work centres, Routings, Users.</summary>
    public IReadOnlyList<string> BarLabels { get; init; } = Array.Empty<string>();
    public IReadOnlyList<int> BarValues { get; init; } = Array.Empty<int>();
    /// <summary>Materials by material type (top 6 + Other).</summary>
    public IReadOnlyList<LabelCountDto> MaterialByType { get; init; } = Array.Empty<LabelCountDto>();
}

public sealed class StoreModuleStats
{
    public int ActiveStockLineCount { get; init; }
    public int DistinctMaterialCount { get; init; }
    public decimal TotalStockValue { get; init; }
    public IReadOnlyList<LabelCountDto> ValueByGrade { get; init; } = Array.Empty<LabelCountDto>();
}

public sealed class SalesModuleStats
{
    public int QuotationDraft { get; init; }
    public int QuotationSent { get; init; }
    public int OrderOpen { get; init; }
    public int OrderConfirmed { get; init; }
    public int DeliveryChallanCount { get; init; }
    public IReadOnlyList<LabelCountDto> QuotationByMonth { get; init; } = Array.Empty<LabelCountDto>();
    public IReadOnlyList<LabelCountDto> SalesOrderByMonth { get; init; } = Array.Empty<LabelCountDto>();
}

public sealed class ProductionModuleStats
{
    public int MrpBomCount { get; init; }
    public IReadOnlyDictionary<string, int> ProductionOrdersByStatus { get; init; } = new Dictionary<string, int>();
    public int OperationTrackingOpen { get; init; }
    public IReadOnlyList<LabelCountDto> ProductionOrdersByStatusChart { get; init; } = Array.Empty<LabelCountDto>();
}

public sealed class LabelCountDto
{
    public string Label { get; init; } = "";
    public decimal Value { get; init; }
    public int Count { get; init; }
}
