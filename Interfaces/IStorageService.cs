namespace InvoiceTrackingSystemBackend.Interfaces;

public interface IStorageService
{
    Task SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    bool Exists(string relativePath);
}
