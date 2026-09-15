using isgDotnet.Constants;

namespace isgDotnet.Interfaces;

public interface IEmailService
{
    Task SendAsync(
        EmailType type,
        IEnumerable<string> toEmails,
        IEnumerable<string>? ccEmails = null,
        IReadOnlyDictionary<string, string>? placeholders = null);
}
