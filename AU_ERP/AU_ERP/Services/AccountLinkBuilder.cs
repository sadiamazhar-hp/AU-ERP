namespace AU_ERP.Services;

public static class AccountLinkBuilder
{
    public static string BaseUrl(IConfiguration configuration)
    {
        var url = configuration["App:PublicBaseUrl"]?.Trim();
        if (string.IsNullOrEmpty(url))
            url = "https://localhost:7099";
        return url.TrimEnd('/');
    }

    public static string PasswordSetLink(IConfiguration configuration, string userId, string code)
    {
        var baseUrl = BaseUrl(configuration);
        var uid = Uri.EscapeDataString(userId);
        var c = Uri.EscapeDataString(code);
        return $"{baseUrl}/Account/SetPassword?userId={uid}&code={c}";
    }
}
