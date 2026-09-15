namespace isgDotnet.Exceptions;

/// <summary>Geçersiz istek/parametre veya ihlal edilen iş kuralı için fırlatılır. Middleware bunu 400 olarak döner.</summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
    {
    }
}
