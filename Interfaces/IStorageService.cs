namespace isgDotnet.Interfaces;

public interface IStorageService
{
    Task SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    bool Exists(string relativePath);

    /// <summary>Dosyayı kaynak konumdan hedefe taşır (ör. arşivleme). Kaynak yoksa sessizce geçer.</summary>
    Task MoveAsync(string sourceRelativePath, string destinationRelativePath, CancellationToken cancellationToken = default);
}
