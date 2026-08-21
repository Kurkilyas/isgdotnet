using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces;
using InvoiceTrackingSystemBackend.Settings;
using Microsoft.Extensions.Options;

namespace InvoiceTrackingSystemBackend.Services;

public class StorageService : IStorageService
{
    private readonly string _rootPath;

    public StorageService(IOptions<StorageOptions> options)
    {
        _rootPath = options.Value.RootPath.TrimEnd('\\', '/');
    }

    public async Task SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default)
    {
        var fullPath = Resolve(relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Resolve(relativePath);
        if (!File.Exists(fullPath))
        {
            throw new NotFoundException("Dosya bulunamadı.");
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public bool Exists(string relativePath)
    {
        return File.Exists(Resolve(relativePath));
    }

    private string Resolve(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new BadRequestException("Dosya yolu geçersiz.");
        }

        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart('\\', '/');
        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new BadRequestException("Dosya yolu geçersiz.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalized));
        var rootFull = Path.GetFullPath(_rootPath);
        if (!fullPath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Dosya yolu geçersiz.");
        }

        return fullPath;
    }
}
