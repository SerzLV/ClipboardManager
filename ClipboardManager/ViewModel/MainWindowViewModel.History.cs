using System.Windows.Input;

namespace ClipboardManager.ViewModels;

public sealed partial class MainWindowViewModel
{
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await RunBusyAsync(async () =>
        {
            var batchSize = HistoryBatchSize;
            var snapshot = await _historyService.LoadInitialPageAsync(batchSize, cancellationToken);
            SetTotalCounts(snapshot.Counts);

            var data = snapshot.Data;
            var imageHashes = await PrepareLoadedImagesAsync(data.Images, cancellationToken);
            PrepareUrlPreviewImages(data.Urls);

            BeginBulkCollectionUpdate();
            try
            {
                Files.ReplaceRange(data.Files);
                Texts.ReplaceRange(data.Texts);
                Urls.ReplaceRange(data.Urls);
                Secrets.ReplaceRange(data.Secrets);
                _knownImageHashes.Clear();

                foreach (var imageHash in imageHashes)
                {
                    _knownImageHashes.Add(imageHash);
                }

                Images.ReplaceRange(data.Images);
                RebuildLookupIndexes();
                ResetPageOffsets(batchSize);
            }
            finally
            {
                EndBulkCollectionUpdate();
            }

            SetStatus(HasItems
                ? text => text.HistoryLoadedStatus(HistoryTotalCount)
                : text => text.HistoryEmptyStatus);
            CommandManager.InvalidateRequerySuggested();
            QueueStaleLinkRefresh();
        });
    }

    private async Task LoadMoreAsync(object? parameter)
    {
        if (parameter is not string sectionKey || IsLoadingMoreHistory)
        {
            return;
        }

        IsLoadingMoreHistory = true;
        try
        {
            var batchSize = HistoryBatchSize;
            switch (sectionKey)
            {
                case FilesSectionKey when _filePageOffset < _totalFileCount:
                    await LoadMoreFilesAsync(batchSize);
                    break;
                case TextsSectionKey when _textPageOffset < _totalTextCount:
                    await LoadMoreTextsAsync(batchSize);
                    break;
                case UrlsSectionKey when _urlPageOffset < _totalUrlCount:
                    await LoadMoreUrlsAsync(batchSize);
                    break;
                case ImagesSectionKey when _imagePageOffset < _totalImageCount:
                    await LoadMoreImagesAsync(batchSize);
                    break;
                case SecretsSectionKey when _secretPageOffset < _totalSecretCount:
                    await LoadMoreSecretsAsync(batchSize);
                    break;
            }
        }
        finally
        {
            IsLoadingMoreHistory = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private bool CanLoadMore(object? parameter)
    {
        if (IsLoadingMoreHistory || IsBusy || parameter is not string sectionKey)
        {
            return false;
        }

        return sectionKey switch
        {
            FilesSectionKey => _filePageOffset < _totalFileCount,
            TextsSectionKey => _textPageOffset < _totalTextCount,
            UrlsSectionKey => _urlPageOffset < _totalUrlCount,
            ImagesSectionKey => _imagePageOffset < _totalImageCount,
            SecretsSectionKey => _secretPageOffset < _totalSecretCount,
            _ => false
        };
    }

    private async Task LoadMoreFilesAsync(int batchSize)
    {
        var files = await _historyService.LoadFilesPageAsync(_filePageOffset, batchSize);
        _filePageOffset += files.Count;
        var unloadedFiles = GetUnloadedItems(files, Files, file => file.Id);
        if (unloadedFiles.Length == 0)
        {
            return;
        }

        AddLoadedItems(() => Files.AddRange(unloadedFiles));
    }

    private async Task LoadMoreTextsAsync(int batchSize)
    {
        var texts = await _historyService.LoadTextsPageAsync(_textPageOffset, batchSize);
        _textPageOffset += texts.Count;
        var unloadedTexts = GetUnloadedItems(texts, Texts, text => text.Id);
        if (unloadedTexts.Length == 0)
        {
            return;
        }

        AddLoadedItems(() => Texts.AddRange(unloadedTexts));
    }

    private async Task LoadMoreUrlsAsync(int batchSize)
    {
        var urls = await _historyService.LoadUrlsPageAsync(_urlPageOffset, batchSize);
        _urlPageOffset += urls.Count;
        var unloadedUrls = GetUnloadedItems(urls, Urls, url => url.Id);
        if (unloadedUrls.Length == 0)
        {
            return;
        }

        PrepareUrlPreviewImages(unloadedUrls);
        AddLoadedItems(() => Urls.AddRange(unloadedUrls));
    }

    private async Task LoadMoreImagesAsync(int batchSize)
    {
        var images = await _historyService.LoadImagesPageAsync(_imagePageOffset, batchSize);
        _imagePageOffset += images.Count;
        var unloadedImages = GetUnloadedItems(images, Images, image => image.Id);
        if (unloadedImages.Length == 0)
        {
            return;
        }

        var imageHashes = await PrepareLoadedImagesAsync(unloadedImages, CancellationToken.None);
        AddLoadedItems(() =>
        {
            Images.AddRange(unloadedImages);
            foreach (var imageHash in imageHashes)
            {
                _knownImageHashes.Add(imageHash);
            }
        });
    }

    private async Task LoadMoreSecretsAsync(int batchSize)
    {
        var secrets = await _historyService.LoadSecretsPageAsync(_secretPageOffset, batchSize);
        _secretPageOffset += secrets.Count;
        var unloadedSecrets = GetUnloadedItems(secrets, Secrets, secret => secret.Id);
        if (unloadedSecrets.Length == 0)
        {
            return;
        }

        AddLoadedItems(() => Secrets.AddRange(unloadedSecrets));
    }

    private void ResetPageOffsets(int batchSize)
    {
        _filePageOffset = Math.Min(Math.Max(0, batchSize), _totalFileCount);
        _textPageOffset = Math.Min(Math.Max(0, batchSize), _totalTextCount);
        _urlPageOffset = Math.Min(Math.Max(0, batchSize), _totalUrlCount);
        _imagePageOffset = Math.Min(Math.Max(0, batchSize), _totalImageCount);
        _secretPageOffset = Math.Min(Math.Max(0, batchSize), _totalSecretCount);
    }

    private void ResetHistoryPageOffsets()
    {
        _filePageOffset = 0;
        _textPageOffset = 0;
        _urlPageOffset = 0;
        _imagePageOffset = 0;
    }

    private void ResetPageOffsetsFromLoadedCollections()
    {
        _filePageOffset = Math.Min(_filePageOffset, _totalFileCount);
        _textPageOffset = Math.Min(_textPageOffset, _totalTextCount);
        _urlPageOffset = Math.Min(_urlPageOffset, _totalUrlCount);
        _imagePageOffset = Math.Min(_imagePageOffset, _totalImageCount);
        _secretPageOffset = Math.Min(_secretPageOffset, _totalSecretCount);
    }

    private void AddLoadedItems(Action addItems)
    {
        BeginBulkCollectionUpdate();
        try
        {
            addItems();
            RebuildLookupIndexes();
            ResetPageOffsetsFromLoadedCollections();
        }
        finally
        {
            EndBulkCollectionUpdate();
        }
    }

    private static T[] GetUnloadedItems<T>(
        IEnumerable<T> items,
        IEnumerable<T> loadedItems,
        Func<T, int> getId)
    {
        var loadedIds = loadedItems
            .Select(getId)
            .Where(id => id > 0)
            .ToHashSet();

        return items
            .Where(item =>
            {
                var id = getId(item);
                return id <= 0 || !loadedIds.Contains(id);
            })
            .ToArray();
    }
}
