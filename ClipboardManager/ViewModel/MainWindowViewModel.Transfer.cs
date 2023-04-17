using System.IO;
using System.Windows.Input;
using ClipboardManager.Data;
using ClipboardManager.Helper;
using ClipboardManager.Interfaces;
using ClipboardManager.Models;

namespace ClipboardManager.ViewModels;

public sealed partial class MainWindowViewModel
{
    private async Task ExportAsync(CancellationToken cancellationToken = default)
    {
        var filePath = _transferDialogService.ShowExportDialog();
        if (filePath is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            SetStatus(text => text.ExportingHistoryStatus);
            await FlushPendingSavesAsync(cancellationToken);

            var data = await _historyService.LoadAllAsync(cancellationToken);
            await _transferService.ExportAsync(data, filePath, cancellationToken);
            SetStatus(text => text.HistoryExportedStatus(HistoryTotalCount));
        });
    }

    private async Task ImportAsync(CancellationToken cancellationToken = default)
    {
        var filePath = _transferDialogService.ShowImportDialog();
        if (filePath is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            SetStatus(text => text.ImportingHistoryStatus);
            var importedData = await _transferService.ImportAsync(filePath, cancellationToken);
            var result = await MergeImportedDataAsync(importedData, cancellationToken);

            SetStatus(result.PinnedUpdatedCount > 0
                ? text => text.ImportedWithPinnedStatus(result.AddedCount, result.PinnedUpdatedCount)
                : text => text.ImportedItemsStatus(result.AddedCount));
        });
    }

    private async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await RunBusyAsync(async () =>
        {
            await _clipboardLock.WaitAsync(cancellationToken);

            try
            {
                await _persistenceLock.WaitAsync(cancellationToken);

                try
                {
                    await _historyService.ClearHistoryAsync(cancellationToken);

                    BeginBulkCollectionUpdate();
                    try
                    {
                        Files.Clear();
                        Texts.Clear();
                        Images.Clear();
                        Urls.Clear();
                        ClearLookupIndexes();
                        _knownImageHashes.Clear();
                        SetHistoryCounts(0, 0, 0, 0);
                        ResetHistoryPageOffsets();
                    }
                    finally
                    {
                        EndBulkCollectionUpdate();
                    }

                    _lastHandledClipboardSignature = null;
                    _clipboardChangeSuppressor.Clear();
                    LastActivityAt = null;
                    SetStatus(text => text.HistoryClearedStatus);
                    CommandManager.InvalidateRequerySuggested();
                }
                finally
                {
                    _persistenceLock.Release();
                }
            }
            finally
            {
                _clipboardLock.Release();
            }
        });
    }

    private async Task<ImportMergeResult> MergeImportedDataAsync(
        ClipboardData importedData,
        CancellationToken cancellationToken)
    {
        var addedItems = new ClipboardItemsBatch();
        var importedImageHashes = new List<string>();
        var pinnedUpdatedCount = 0;

        await _clipboardLock.WaitAsync(cancellationToken);

        try
        {
            await _persistenceLock.WaitAsync(cancellationToken);

            try
            {
                pinnedUpdatedCount += await MergeImportedFilesAsync(importedData.Files, addedItems, cancellationToken);
                pinnedUpdatedCount += await MergeImportedTextsAsync(importedData.Texts, addedItems, cancellationToken);
                pinnedUpdatedCount += await MergeImportedUrlsAsync(importedData.Urls, addedItems, cancellationToken);
                pinnedUpdatedCount += await MergeImportedImagesAsync(
                    importedData.Images,
                    addedItems,
                    importedImageHashes,
                    cancellationToken);

                await _itemPersistenceService.SaveItemsAsync(
                    addedItems.Files,
                    addedItems.Texts,
                    addedItems.Images,
                    addedItems.Urls,
                    cancellationToken);

                BeginBulkCollectionUpdate();
                try
                {
                    PrepareUrlPreviewImages(addedItems.Urls);
                    Files.AddRange(addedItems.Files);
                    Texts.AddRange(addedItems.Texts);
                    Urls.AddRange(addedItems.Urls);
                    Images.AddRange(addedItems.Images);

                    foreach (var imageHash in importedImageHashes)
                    {
                        _knownImageHashes.Add(imageHash);
                    }

                    RebuildLookupIndexes();
                    AddToTotalCounts(addedItems);
                    ResetPageOffsetsFromLoadedCollections();
                }
                finally
                {
                    EndBulkCollectionUpdate();
                }
            }
            finally
            {
                _persistenceLock.Release();
            }
        }
        finally
        {
            _clipboardLock.Release();
        }

        CommandManager.InvalidateRequerySuggested();

        return new ImportMergeResult(addedItems.TotalCount, pinnedUpdatedCount);
    }

    private async Task<int> MergeImportedFilesAsync(
        IReadOnlyList<FileInfoModel> importedFiles,
        ClipboardItemsBatch addedItems,
        CancellationToken cancellationToken)
    {
        var paths = Files
            .Select(file => file.FilePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pinnedUpdatedCount = 0;

        foreach (var importedFile in importedFiles)
        {
            var filePath = NormalizeImportedFilePath(importedFile.FilePath);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                continue;
            }

            var existingFile = Files.FirstOrDefault(
                file => string.Equals(file.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (existingFile is not null)
            {
                pinnedUpdatedCount += await PinExistingIfNeededAsync(
                    existingFile,
                    importedFile.IsPinned,
                    cancellationToken);
                continue;
            }

            if (!paths.Add(filePath))
            {
                continue;
            }

            if (await _itemLookupService.FileExistsAsync(filePath, cancellationToken))
            {
                continue;
            }

            importedFile.Id = 0;
            importedFile.FilePath = filePath;
            importedFile.Name = string.IsNullOrWhiteSpace(importedFile.Name)
                ? Path.GetFileName(filePath)
                : importedFile.Name;
            addedItems.Files.Add(importedFile);
        }

        return pinnedUpdatedCount;
    }

    private async Task<int> MergeImportedTextsAsync(
        IReadOnlyList<TextModel> importedTexts,
        ClipboardItemsBatch addedItems,
        CancellationToken cancellationToken)
    {
        var texts = Texts
            .Select(text => text.Text)
            .ToHashSet(StringComparer.Ordinal);
        var pinnedUpdatedCount = 0;

        foreach (var importedText in importedTexts)
        {
            if (string.IsNullOrWhiteSpace(importedText.Text))
            {
                continue;
            }

            var existingText = Texts.FirstOrDefault(text => text.Text == importedText.Text);
            if (existingText is not null)
            {
                pinnedUpdatedCount += await PinExistingIfNeededAsync(
                    existingText,
                    importedText.IsPinned,
                    cancellationToken);
                continue;
            }

            if (!texts.Add(importedText.Text))
            {
                continue;
            }

            if (await _itemLookupService.TextExistsAsync(importedText.Text, cancellationToken))
            {
                continue;
            }

            importedText.Id = 0;
            addedItems.Texts.Add(importedText);
        }

        return pinnedUpdatedCount;
    }

    private async Task<int> MergeImportedUrlsAsync(
        IReadOnlyList<UrlModel> importedUrls,
        ClipboardItemsBatch addedItems,
        CancellationToken cancellationToken)
    {
        var urls = Urls
            .Select(url => url.Url)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingUrls = await _itemLookupService.FindExistingUrlsAsync(
            importedUrls
                .Select(url => url.Url)
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .ToArray(),
            cancellationToken);
        var pinnedUpdatedCount = 0;

        foreach (var importedUrl in importedUrls)
        {
            if (string.IsNullOrWhiteSpace(importedUrl.Url))
            {
                continue;
            }

            var existingUrl = Urls.FirstOrDefault(
                url => string.Equals(url.Url, importedUrl.Url, StringComparison.OrdinalIgnoreCase));
            if (existingUrl is not null)
            {
                pinnedUpdatedCount += await PinExistingIfNeededAsync(
                    existingUrl,
                    importedUrl.IsPinned,
                    cancellationToken);
                continue;
            }

            if (!urls.Add(importedUrl.Url))
            {
                continue;
            }

            if (existingUrls.Contains(importedUrl.Url))
            {
                continue;
            }

            importedUrl.Id = 0;
            importedUrl.Title = string.IsNullOrWhiteSpace(importedUrl.Title)
                ? importedUrl.Url
                : importedUrl.Title;
            importedUrl.MetadataUpdatedAt ??= DateTime.UtcNow;
            addedItems.Urls.Add(importedUrl);
        }

        return pinnedUpdatedCount;
    }

    private async Task<int> MergeImportedImagesAsync(
        IReadOnlyList<ImageModel> importedImages,
        ClipboardItemsBatch addedItems,
        ICollection<string> importedImageHashes,
        CancellationToken cancellationToken)
    {
        var existingImages = Images.ToArray();
        var imageByHash = await Task.Run(
            () =>
            {
                var hashes = new Dictionary<string, ImageModel>(StringComparer.Ordinal);
                foreach (var image in existingImages)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var imageHash = TryCreateImageHash(image);
                    if (imageHash is not null)
                    {
                        hashes.TryAdd(imageHash, image);
                    }
                }

                return hashes;
            },
            cancellationToken);
        var importedImagesWithHashes = await Task.Run(
            () =>
            {
                return importedImages
                    .Select(image =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return (Image: image, Hash: TryCreateImageHash(image));
                    })
                    .ToArray();
            },
            cancellationToken);

        var pinnedUpdatedCount = 0;

        foreach (var (importedImage, imageHash) in importedImagesWithHashes)
        {
            if (!HasUsableImage(importedImage) || importedImage.ImageData.Length == 0)
            {
                continue;
            }

            if (imageHash is null)
            {
                continue;
            }

            if (imageByHash.TryGetValue(imageHash, out var existingImage))
            {
                pinnedUpdatedCount += await PinExistingIfNeededAsync(
                    existingImage,
                    importedImage.IsPinned,
                    cancellationToken);
                continue;
            }

            if (importedImageHashes.Contains(imageHash))
            {
                continue;
            }

            importedImage.Id = 0;
            importedImageHashes.Add(imageHash);
            addedItems.Images.Add(importedImage);
        }

        return pinnedUpdatedCount;
    }

    private async Task<int> PinExistingIfNeededAsync(
        IPinnedClipboardItem existingItem,
        bool shouldPin,
        CancellationToken cancellationToken)
    {
        if (!shouldPin || existingItem.IsPinned)
        {
            return 0;
        }

        existingItem.IsPinned = true;
        await _itemPersistenceService.UpdatePinAsync(existingItem, cancellationToken);
        return 1;
    }

    private static string NormalizeImportedFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetFullPath(filePath.Trim());
        }
        catch
        {
            return filePath.Trim();
        }
    }
}
