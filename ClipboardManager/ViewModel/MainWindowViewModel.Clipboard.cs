using System.Globalization;
using System.Windows.Input;
using ClipboardManager.Interfaces;
using ClipboardManager.Models;
using ClipboardManager.Services;

namespace ClipboardManager.ViewModels;

public sealed partial class MainWindowViewModel
{
    public async Task HandleClipboardChangedAsync(CancellationToken cancellationToken = default)
    {
        await _clipboardLock.WaitAsync(cancellationToken);

        try
        {
            var snapshot = await _clipboardService.GetCurrentSnapshotAsync(cancellationToken);
            if (snapshot is null)
            {
                return;
            }

            var signature = snapshot.Signature;
            if (_clipboardChangeSuppressor.ShouldSuppress(signature))
            {
                _lastHandledClipboardSignature = signature;
                return;
            }

            if (signature == _lastHandledClipboardSignature)
            {
                return;
            }

            _lastHandledClipboardSignature = signature;

            var addedItems = new ClipboardItemsBatch();

            switch (signature.Kind)
            {
                case ClipboardContentKind.FileDropList:
                    await ProcessFileDropListAsync(snapshot, addedItems, cancellationToken);
                    break;
                case ClipboardContentKind.Text:
                    await ProcessTextAsync(snapshot, addedItems, cancellationToken);
                    break;
                case ClipboardContentKind.Image:
                    ProcessImage(snapshot, addedItems);
                    break;
            }

            if (addedItems.HasItems)
            {
                AddToTotalCounts(addedItems);
                LastActivityAt = DateTime.Now;
                SetStatus(text => text.AddedNewItemsStatus(addedItems.TotalCount));
                CommandManager.InvalidateRequerySuggested();

                await PersistAddedItemsAsync(addedItems, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            ShowError(_localization.ClipboardProcessingFailedTitle, ex);
        }
        finally
        {
            _clipboardLock.Release();
        }
    }

    public async Task FlushPendingSavesAsync(CancellationToken cancellationToken = default)
    {
        await _clipboardLock.WaitAsync(cancellationToken);

        try
        {
            await PersistUnsavedItemsAsync(cancellationToken);
            await ClearPendingSecretClipboardAsync(cancellationToken);
        }
        finally
        {
            _clipboardLock.Release();
        }
    }

    private async Task ProcessFileDropListAsync(
        ClipboardContentSnapshot snapshot,
        ClipboardItemsBatch addedItems,
        CancellationToken cancellationToken)
    {
        foreach (var file in snapshot.FilePaths)
        {
            var fileInfo = await _fileCaptureService.TryCaptureFileAsync(file, _knownFilePaths, cancellationToken);
            if (fileInfo is null)
            {
                continue;
            }

            Files.Add(fileInfo);
            _knownFilePaths.Add(fileInfo.FilePath);
            addedItems.Files.Add(fileInfo);
        }
    }

    private void ProcessImage(ClipboardContentSnapshot snapshot, ClipboardItemsBatch addedItems)
    {
        var image = snapshot.Image;
        if (image is null)
        {
            return;
        }

        if (!_knownImageHashes.Add(snapshot.Signature.Value))
        {
            return;
        }

        var imageInfo = new ImageModel
        {
            ImageSource = image,
            ThumbnailSource = image,
            Name = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture)
        };

        Images.Add(imageInfo);
        addedItems.Images.Add(imageInfo);
    }

    private async Task PersistAddedItemsAsync(
        ClipboardItemsBatch addedItems,
        CancellationToken cancellationToken)
    {
        if (!addedItems.HasItems)
        {
            return;
        }

        await _persistenceLock.WaitAsync(cancellationToken);

        try
        {
            await _itemPersistenceService.SaveItemsAsync(
                addedItems.Files,
                addedItems.Texts,
                addedItems.Images,
                addedItems.Urls,
                cancellationToken);

            SetStatus(text => text.AutoSavedItemsStatus(addedItems.TotalCount));
        }
        catch (Exception ex)
        {
            ShowError(_localization.AutoSaveFailedTitle, ex);
        }
        finally
        {
            _persistenceLock.Release();
        }
    }

    private async Task PersistUnsavedItemsAsync(CancellationToken cancellationToken)
    {
        var unsavedItems = new ClipboardItemsBatch(
            Files.Where(x => x.Id == 0).ToArray(),
            Texts.Where(x => x.Id == 0).ToArray(),
            Images.Where(x => x.Id == 0).ToArray(),
            Urls.Where(x => x.Id == 0).ToArray());

        if (!unsavedItems.HasItems)
        {
            return;
        }

        await PersistAddedItemsAsync(unsavedItems, cancellationToken);
    }
}
