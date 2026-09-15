namespace isgDotnet.Exceptions;

/// <summary>Mevcut durumla çakışan bir işlem yapılmak istendiğinde fırlatılır (örn. zaten onaylanmış kayıt). Middleware bunu 409 olarak döner.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
