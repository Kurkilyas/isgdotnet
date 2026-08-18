namespace InvoiceTrackingSystemBackend.Exceptions;

/// <summary>Vega (ERP) gibi harici bir sisteme erişilemediğinde fırlatılır. Middleware bunu 503 olarak döner.</summary>
public class ExternalServiceException : Exception
{
    public ExternalServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
