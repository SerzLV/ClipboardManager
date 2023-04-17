using ClipboardManager.Models;

namespace ClipboardManager.Interfaces;

public interface ILinkMetadataService
{
    Task<UrlModel?> GetMetadataAsync(string url, CancellationToken cancellationToken = default);
}
