using ClipboardManager.Interfaces;
using ClipboardManager.Models;
using ClipboardManager.Services;
using System.Diagnostics;

namespace ClipboardManager.ViewModels;

public sealed partial class MainWindowViewModel
{
    private async Task ProcessTextAsync(
        ClipboardContentSnapshot snapshot,
        ClipboardItemsBatch addedItems,
        CancellationToken cancellationToken)
    {
        var text = snapshot.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (!await _textCaptureService.ShouldCaptureTextAsync(
            text,
            _knownTexts,
            _knownUrlTextValues,
            cancellationToken))
        {
            return;
        }

        var textInfo = new TextModel { Text = text };
        Texts.Add(textInfo);
        _knownTexts.Add(text);
        addedItems.Texts.Add(textInfo);

        var urls = _textCaptureService.ExtractUrlCandidates(text, _knownUrls);
        if (urls.Count == 0)
        {
            return;
        }

        BeginLinkLoading();
        IReadOnlyList<UrlModel> metadataItems;
        try
        {
            metadataItems = await _textCaptureService.LoadNewUrlMetadataAsync(urls, cancellationToken);
        }
        finally
        {
            EndLinkLoading();
        }

        var newUrls = new List<UrlModel>(metadataItems.Count);
        foreach (var metadata in metadataItems)
        {
            if (_knownUrls.Add(metadata.Url))
            {
                AddUrlTextValues(metadata);
                PrepareUrlPreviewImage(metadata);
                newUrls.Add(metadata);
                addedItems.Urls.Add(metadata);
            }
        }

        if (newUrls.Count > 0)
        {
            BeginBulkCollectionUpdate();
            try
            {
                Urls.AddRange(newUrls);
            }
            finally
            {
                EndBulkCollectionUpdate();
            }
        }
    }

    private void OpenLink(object? parameter)
    {
        if (parameter is UrlModel url)
        {
            TryLaunch(() => _shellLauncher.OpenUrl(url.Url));
            SetStatus(text => text.LinkOpenedStatus);
        }
    }

    private static bool CanOpenLink(object? parameter)
    {
        return parameter is UrlModel url
            && Uri.TryCreate(url.Url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private void AddUrlToLookup(UrlModel url)
    {
        if (!string.IsNullOrWhiteSpace(url.Url))
        {
            _knownUrls.Add(url.Url);
        }

        AddUrlTextValues(url);
    }

    private void RebuildUrlLookupIndexes()
    {
        _knownUrls.Clear();
        _knownUrlTextValues.Clear();

        foreach (var url in Urls)
        {
            AddUrlToLookup(url);
        }
    }

    private void AddUrlTextValues(UrlModel url)
    {
        if (!string.IsNullOrWhiteSpace(url.Url))
        {
            _knownUrlTextValues.Add(url.Url);
        }

        if (!string.IsNullOrWhiteSpace(url.Title))
        {
            _knownUrlTextValues.Add(url.Title);
        }
    }

    private void PrepareUrlPreviewImages(IEnumerable<UrlModel> urls)
    {
        foreach (var url in urls)
        {
            PrepareUrlPreviewImage(url);
        }
    }

    private void PrepareUrlPreviewImage(UrlModel url)
    {
        if (!_linkPreviewImageService.CanLoadPreview(url.ImageUrl))
        {
            url.PreviewImageSource ??= _linkPreviewImageService.DefaultImageSource;
            url.IsPreviewImageLoading = false;
            return;
        }

        url.PreviewImageSource ??= _linkPreviewImageService.DefaultImageSource;
        url.IsPreviewImageLoading = !_linkPreviewImageService.HasCachedPreview(url.ImageUrl);
        _ = LoadUrlPreviewImageAsync(url);
    }

    private async Task LoadUrlPreviewImageAsync(UrlModel url)
    {
        try
        {
            await _urlPreviewImageLoadLock.WaitAsync();
            try
            {
                var imageUrl = url.ImageUrl;
                var previewImage = await Task.Run(
                    () => _linkPreviewImageService.LoadPreviewAsync(
                        imageUrl,
                        UrlPreviewImageDecodePixelWidth));
                if (previewImage is not null)
                {
                    url.PreviewImageSource = previewImage;
                }
            }
            finally
            {
                _urlPreviewImageLoadLock.Release();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            url.IsPreviewImageLoading = false;

            if (url.IsPinned)
            {
                RebuildFavorites();
                FavoritesView.Refresh();
            }
        }
    }

    private void QueueStaleLinkRefresh()
    {
        if (LinkRefreshIntervalDays <= 0)
        {
            return;
        }

        _ = RefreshStaleLinksAsync();
    }

    private async Task RefreshStaleLinksAsync()
    {
        if (!await _linkMetadataRefreshLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            BeginLinkLoading();
            var refreshedLinks = await _linkMetadataRefreshService.RefreshStaleLinksAsync(
                LinkRefreshIntervalDays,
                MaxStaleLinkRefreshPerRun);
            ApplyRefreshedLinks(refreshedLinks);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            EndLinkLoading();
            _linkMetadataRefreshLock.Release();
        }
    }

    private void ApplyRefreshedLinks(IReadOnlyList<UrlModel> refreshedLinks)
    {
        if (refreshedLinks.Count == 0 || Urls.Count == 0)
        {
            return;
        }

        var refreshedById = refreshedLinks
            .Where(url => url.Id != 0)
            .ToDictionary(url => url.Id);
        if (refreshedById.Count == 0)
        {
            return;
        }

        var updatedVisibleLinks = 0;
        BeginBulkCollectionUpdate();
        try
        {
            for (var index = 0; index < Urls.Count; index++)
            {
                var existingUrl = Urls[index];
                if (!refreshedById.TryGetValue(existingUrl.Id, out var refreshedUrl))
                {
                    continue;
                }

                var imageChanged = !string.Equals(
                    existingUrl.ImageUrl,
                    refreshedUrl.ImageUrl,
                    StringComparison.OrdinalIgnoreCase);
                if (!imageChanged)
                {
                    refreshedUrl.PreviewImageSource = existingUrl.PreviewImageSource;
                    refreshedUrl.IsPreviewImageLoading = existingUrl.IsPreviewImageLoading;
                }

                Urls[index] = refreshedUrl;
                updatedVisibleLinks++;

                if (imageChanged)
                {
                    PrepareUrlPreviewImage(refreshedUrl);
                }
            }

            if (updatedVisibleLinks > 0)
            {
                RebuildLookupIndexes();
            }
        }
        finally
        {
            EndBulkCollectionUpdate();
        }
    }

    private async Task ClearLinkPreviewCacheAsync()
    {
        var deletedCount = await _linkPreviewImageService.ClearCacheAsync();
        SetStatus(text => text.LinkPreviewCacheClearedStatus(deletedCount));
    }

    private void RemoveUrlFromLookup(UrlModel url)
    {
        RebuildUrlLookupIndexes();
    }

    private bool MatchesUrl(UrlModel url)
    {
        return MatchesSearch(url.Title, url.Url, url.Description);
    }

    private void BeginLinkLoading()
    {
        _activeLinkLoadCount++;
        if (_activeLinkLoadCount == 1)
        {
            RaiseActivityStateChanged();
        }
    }

    private void EndLinkLoading()
    {
        if (_activeLinkLoadCount > 0)
        {
            _activeLinkLoadCount--;
        }

        if (_activeLinkLoadCount == 0)
        {
            RaiseActivityStateChanged();
        }
    }
}
