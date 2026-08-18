namespace InvoiceTrackingSystemBackend.Settings;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = null!;
    public string Issuer { get; set; } = "InvoiceTrackingSystem";
    public string Audience { get; set; } = "InvoiceTrackingSystem.Frontend";
    public int AccessTokenExpiryMinutes { get; set; } = 15;
    public int RefreshTokenExpiryDays { get; set; } = 2;
}
