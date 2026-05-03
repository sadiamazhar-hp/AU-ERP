using AU_ERP.Models;

namespace AU_ERP.Services;

/// <summary>Maps BP <see cref="BusinessPartnerMasterSample.PaymentMethods"/> (e.g. Cash, Cheque, Online) to normalized payment method keys.</summary>
public static class SalesPaymentMethodHelper
{
    /// <summary>Returns <see cref="SalesPayment.MethodCash"/>, cheque, or online.</summary>
    public static string SuggestFromBusinessPartner(string? paymentMethods)
    {
        var s = (paymentMethods ?? "").Trim();
        if (s.Length == 0)
            return SalesPayment.MethodCash;

        if (s.Contains("cheque", StringComparison.OrdinalIgnoreCase))
            return SalesPayment.MethodCheque;
        if (s.Contains("online", StringComparison.OrdinalIgnoreCase))
            return SalesPayment.MethodOnline;
        if (s.Contains("cash", StringComparison.OrdinalIgnoreCase))
            return SalesPayment.MethodCash;

        return SalesPayment.MethodCash;
    }

    public static bool IsValidMethod(string? method)
    {
        var m = (method ?? "").Trim().ToLowerInvariant();
        return m is SalesPayment.MethodCash or SalesPayment.MethodCheque or SalesPayment.MethodOnline;
    }

    public static string NormalizeMethod(string? method)
    {
        var m = (method ?? "").Trim().ToLowerInvariant();
        if (m == SalesPayment.MethodCheque) return SalesPayment.MethodCheque;
        if (m == SalesPayment.MethodOnline) return SalesPayment.MethodOnline;
        return SalesPayment.MethodCash;
    }
}
