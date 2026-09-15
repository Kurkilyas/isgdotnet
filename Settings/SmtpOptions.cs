namespace isgDotnet.Settings;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = null!;
    public int Port { get; set; } = 587;
    public string User { get; set; } = null!;
    public string Password { get; set; } = null!;
    public bool EnableSsl { get; set; } = true;
    public string From { get; set; } = null!;
    public string FromName { get; set; } = "Sistem";
}
