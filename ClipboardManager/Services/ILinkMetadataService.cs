using ClipboardManager.Models;

namespace ClipboardManager.Services;

public interface ILinkMetadataService
{
    Task<UrlModel?> GetMetadataAsync(string url, CancellationToken cancellationToken = default);
}
