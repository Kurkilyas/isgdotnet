namespace isgDotnet.Settings;

public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Lokal klasör veya NAS UNC kökü. Örn. C:\temp\invoice-tracking-files veya \\nas\share\invoice-tracking</summary>
    public string RootPath { get; set; } = @"C:\temp\invoice-tracking-files";

    /// <summary>İleride NAS servis hesabı; local'de boş bırakılır.</summary>
    public string? Username { get; set; }

    public string? Password { get; set; }

    public int MaxFileSizeBytes { get; set; } = 2 * 1024 * 1024;
}
