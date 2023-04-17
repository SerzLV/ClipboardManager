using ClipboardManager.Interfaces;
using ClipboardManager.Models;

namespace ClipboardManager.Services;

public sealed class LinkMetadataRefreshService(
    IClipboardRepository repository,
    ILinkMetadataService metadataService) : ILinkMetadataRefreshService
{
    public async Task<IReadOnlyList<UrlModel>> RefreshStaleLinksAsync(
        int maxAgeDays,
        int maxItems,
        CancellationToken cancellationToken = default)
    {
        if (maxAgeDays <= 0 || maxItems <= 0)
        {
            return [];
        }

        var staleBeforeUtc = DateTime.UtcNow.AddDays(-maxAgeDays);
        var staleLinks = await repository.LoadStaleUrlsAsync(
            staleBeforeUtc,
            maxItems,
            cancellationToken).ConfigureAwait(false);
        if (staleLinks.Count == 0)
        {
            return [];
        }

        var refreshedLinks = new List<UrlModel>(staleLinks.Count);
        foreach (var staleLink in staleLinks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var refreshedLink = await RefreshOneAsync(staleLink, cancellationToken)
                .ConfigureAwait(false);
            await repository.UpdateUrlMetadataAsync(refreshedLink, cancellationToken)
                .ConfigureAwait(false);
            refreshedLinks.Add(refreshedLink);
        }

        return refreshedLinks;
    }

    private async Task<UrlModel> RefreshOneAsync(
        UrlModel staleLink,
        CancellationToken cancellationToken)
    {
        var refreshedLink = await metadataService.GetMetadataAsync(staleLink.Url, cancellationToken)
            .ConfigureAwait(false);
        if (refreshedLink is null)
        {
            return MarkChecked(staleLink);
        }

        if (LooksLikeFallbackMetadata(staleLink.Url, refreshedLink))
        {
            return MarkChecked(staleLink, refreshedLink.MetadataUpdatedAt);
        }

        return new UrlModel
        {
            Id = staleLink.Id,
            Url = staleLink.Url,
            Title = string.IsNullOrWhiteSpace(refreshedLink.Title)
                ? staleLink.Title
                : refreshedLink.Title,
            Description = refreshedLink.Description,
            ImageUrl = string.IsNullOrWhiteSpace(refreshedLink.ImageUrl)
                ? staleLink.ImageUrl
                : refreshedLink.ImageUrl,
            IsPinned = staleLink.IsPinned,
            MetadataUpdatedAt = refreshedLink.MetadataUpdatedAt ?? DateTime.UtcNow
        };
    }

    private static UrlModel MarkChecked(UrlModel link, DateTime? checkedAt = null)
    {
        link.MetadataUpdatedAt = checkedAt ?? DateTime.UtcNow;
        return link;
    }

    private static bool LooksLikeFallbackMetadata(string url, UrlModel metadata)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && string.Equals(metadata.Title, uri.Host, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(metadata.Description);
    }
}
