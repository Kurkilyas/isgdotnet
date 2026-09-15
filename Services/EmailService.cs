using System.Net;
using System.Net.Mail;
using isgDotnet.Constants;
using isgDotnet.Exceptions;
using isgDotnet.Interfaces;
using isgDotnet.Settings;
using Microsoft.Extensions.Options;

namespace isgDotnet.Services;

public class EmailService : IEmailService
{
    private readonly SmtpOptions _smtp;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpOptions> smtp, ILogger<EmailService> logger)
    {
        _smtp = smtp.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        EmailType type,
        IEnumerable<string> toEmails,
        IEnumerable<string>? ccEmails = null,
        IReadOnlyDictionary<string, string>? placeholders = null)
    {
        var toList = NormalizeAddresses(toEmails);
        if (toList.Count == 0)
        {
            throw new BadRequestException("En az bir alıcı e-posta adresi gereklidir.");
        }

        var ccList = NormalizeAddresses(ccEmails)
            .Where(email => !toList.Contains(email, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var (subject, body) = BuildTemplate(type, placeholders ?? new Dictionary<string, string>());

        using var message = new MailMessage
        {
            From = new MailAddress(_smtp.From, _smtp.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        foreach (var email in toList)
        {
            message.To.Add(email);
        }

        foreach (var email in ccList)
        {
            message.CC.Add(email);
        }

        try
        {
            using var client = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                EnableSsl = _smtp.EnableSsl,
                Credentials = new NetworkCredential(_smtp.User, _smtp.Password)
            };

            await client.SendMailAsync(message);
            _logger.LogInformation(
                "E-posta gönderildi. Tür: {Type}, To: {To}, Cc: {Cc}",
                type,
                string.Join(", ", toList),
                string.Join(", ", ccList));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-posta gönderilemedi. Tür: {Type}, To: {To}", type, string.Join(", ", toList));
            throw new ExternalServiceException("E-posta şu anda gönderilemedi.", ex);
        }
    }

    private static List<string> NormalizeAddresses(IEnumerable<string>? emails)
    {
        if (emails is null)
        {
            return [];
        }

        return emails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static (string Subject, string Body) BuildTemplate(
        EmailType type,
        IReadOnlyDictionary<string, string> placeholders)
    {
        var (subject, body) = type switch
        {
            EmailType.EmailVerification => (
                "E-posta doğrulama kodu",
                "<p>Merhaba {{FullName}},</p><p>Doğrulama kodunuz: <strong>{{Code}}</strong></p><p>Kod {{ExpireMinutes}} dakika geçerlidir.</p>"),
            EmailType.PasswordReset => (
                "Şifre sıfırlama kodu",
                "<p>Merhaba {{FullName}},</p><p>Şifre sıfırlama kodunuz: <strong>{{Code}}</strong></p><p>Kod {{ExpireMinutes}} dakika geçerlidir.</p>"),
            EmailType.LoginTwoFactor => (
                "Giriş doğrulama kodu",
                "<p>Merhaba {{FullName}},</p><p>Giriş kodunuz: <strong>{{Code}}</strong></p><p>Kod {{ExpireMinutes}} dakika geçerlidir.</p>"),
            EmailType.AccountLocked => (
                "Hesabınız kilitlendi",
                "<p>Merhaba {{FullName}},</p><p>Hesabınız art arda hatalı giriş nedeniyle geçici olarak kilitlendi.</p>"),
            _ => throw new InvalidOperationException($"Tanımsız e-posta türü: {type}")
        };

        return (Apply(subject, placeholders), Apply(body, placeholders));
    }

    private static string Apply(string template, IReadOnlyDictionary<string, string> placeholders)
    {
        var result = template;
        foreach (var (key, value) in placeholders)
        {
            result = result.Replace("{{" + key + "}}", value, StringComparison.Ordinal);
        }

        return result;
    }
}
