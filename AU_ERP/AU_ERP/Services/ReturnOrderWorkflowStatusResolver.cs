using AU_ERP.Models;

namespace AU_ERP.Services;

public static class ReturnOrderWorkflowStatus
{
    public const string All = "All";
    public const string RodPending = "ROD Pending";
    public const string DcPending = "DC pending";
    public const string Completed = "Completed";
}

public static class ReturnOrderWorkflowStatusResolver
{
    public static string Resolve(string? qiStatus, DateTime? deliveryCompletedAt)
    {
        if (!string.Equals(qiStatus, SalesReturnQualityInspection.StatusCompleted, StringComparison.OrdinalIgnoreCase))
            return ReturnOrderWorkflowStatus.RodPending;
        if (!deliveryCompletedAt.HasValue)
            return ReturnOrderWorkflowStatus.DcPending;
        return ReturnOrderWorkflowStatus.Completed;
    }

    public static string BadgeClass(string status) =>
        status switch
        {
            ReturnOrderWorkflowStatus.RodPending => "ro-badge-ro-pending",
            ReturnOrderWorkflowStatus.DcPending => "ro-badge-dc-pending",
            ReturnOrderWorkflowStatus.Completed => "ro-badge-completed",
            _ => "ro-badge-ro-pending"
        };
}
