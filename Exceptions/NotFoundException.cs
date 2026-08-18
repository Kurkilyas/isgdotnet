namespace InvoiceTrackingSystemBackend.Exceptions;

/// <summary>İstenen kayıt bulunamadığında fırlatılır. Middleware bunu 404 olarak döner.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
