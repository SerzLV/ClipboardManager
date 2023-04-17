using ClipboardManager.Models;

namespace ClipboardManager.Interfaces;

public interface ILinkMetadataRefreshService
{
    Task<IReadOnlyList<UrlModel>> RefreshStaleLinksAsync(
        int maxAgeDays,
        int maxItems,
        CancellationToken cancellationToken = default);
}
