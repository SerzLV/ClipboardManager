using System.Globalization;
using System.IO;
using System.Windows.Input;
using ClipboardManager.Interfaces;
using ClipboardManager.Models;
using ClipboardManager.Services;

namespace ClipboardManager.ViewModels;

public sealed partial class MainWindowViewModel
{
    private async Task DeleteAsync(object? parameter)
    {
        try
        {
            await _clipboardLock.WaitAsync();

            try
            {
                await _persistenceLock.WaitAsync();

                try
                {
                    switch (parameter)
                    {
                        case FileInfoModel file:
                            await _itemPersistenceService.DeleteItemAsync(file);
                            Files.Remove(file);
                            RemoveFileFromLookup(file);
                            _totalFileCount = Math.Max(0, _totalFileCount - 1);
                            _filePageOffset = Math.Max(0, _filePageOffset - 1);
                            break;
                        case TextModel text:
                            await _itemPersistenceService.DeleteItemAsync(text);
                            Texts.Remove(text);
                            _knownTexts.Remove(text.Text);
                            _totalTextCount = Math.Max(0, _totalTextCount - 1);
                            _textPageOffset = Math.Max(0, _textPageOffset - 1);
                            break;
                        case ImageModel image:
                            var imageHash = TryCreateImageHash(image);
                            await _itemPersistenceService.DeleteItemAsync(image);
                            Images.Remove(image);
                            if (ReferenceEquals(PreviewImage, image))
                            {
                                CloseImagePreview();
                            }
                            if (imageHash is not null)
                            {
                                _knownImageHashes.Remove(imageHash);
                            }
                            _totalImageCount = Math.Max(0, _totalImageCount - 1);
                            _imagePageOffset = Math.Max(0, _imagePageOffset - 1);
                            break;
                        case UrlModel url:
                            await _itemPersistenceService.DeleteItemAsync(url);
                            Urls.Remove(url);
                            RemoveUrlFromLookup(url);
                            _totalUrlCount = Math.Max(0, _totalUrlCount - 1);
                            _urlPageOffset = Math.Max(0, _urlPageOffset - 1);
                            break;
                        case SecretModel secret:
                            HideSecretCore(secret, true);
                            ForgetSecretCopyTrust(secret);
                            await _itemPersistenceService.DeleteItemAsync(secret);
                            Secrets.Remove(secret);
                            _totalSecretCount = Math.Max(0, _totalSecretCount - 1);
                            _secretPageOffset = Math.Max(0, _secretPageOffset - 1);
                            break;
                    }

                    ResetPageOffsetsFromLoadedCollections();
                    RefreshClipboardViews();
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

            SetStatus(text => text.ItemDeletedStatus);
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex)
        {
            ShowError(_localization.DeleteFailedTitle, ex);
        }
    }

    private async Task CopyAsync(object? parameter)
    {
        if (parameter is SecretModel secret)
        {
            await CopySecretAsync(secret);
            return;
        }

        try
        {
            ClipboardContentSignature? signature = parameter switch
            {
                FileInfoModel file when File.Exists(file.FilePath) =>
                    await _clipboardService.SetFileDropListAsync([file.FilePath]),
                TextModel text when !string.IsNullOrEmpty(text.Text) =>
                    await _clipboardService.SetTextAsync(text.Text),
                UrlModel url when !string.IsNullOrWhiteSpace(url.Url) =>
                    await _clipboardService.SetTextAsync(url.Url),
                _ => null
            };

            if (signature is null && parameter is ImageModel image)
            {
                var imageSource = await EnsureImageSourceAsync(image);
                if (imageSource is not null)
                {
                    signature = await _clipboardService.SetImageAsync(imageSource);
                }
            }

            if (signature is not null)
            {
                _clipboardChangeSuppressor.Suppress(signature);
                _lastHandledClipboardSignature = signature;
                SetStatus(text => text.CopiedStatus);
            }
        }
        catch (Exception ex)
        {
            ShowError(_localization.CopyFailedTitle, ex);
        }
    }

    private void OpenFile(object? parameter)
    {
        if (parameter is FileInfoModel file)
        {
            TryLaunch(() => _shellLauncher.OpenFile(file.FilePath));
            SetStatus(text => text.FileOpenedStatus);
        }
    }

    private async Task OpenFavoriteAsync(object? parameter)
    {
        if (parameter is not FavoriteClipboardItem favorite)
        {
            return;
        }

        switch (favorite.Source)
        {
            case FileInfoModel file:
                OpenFile(file);
                break;
            case UrlModel url:
                OpenLink(url);
                break;
            case ImageModel image:
                await OpenImagePreviewAsync(image);
                break;
        }
    }

    private void SelectSection(object? parameter)
    {
        if (parameter is int index)
        {
            SelectedSectionIndex = index;
            return;
        }

        if (parameter is string value && int.TryParse(value, CultureInfo.InvariantCulture, out index))
        {
            SelectedSectionIndex = index;
        }
    }

    private void ChangeLanguage(object? parameter)
    {
        if (parameter is string languageCode)
        {
            SelectedLanguageCode = languageCode;
        }
    }

    private async Task TogglePinAsync(object? parameter)
    {
        if (parameter is not IPinnedClipboardItem item)
        {
            return;
        }

        var previousValue = item.IsPinned;
        item.IsPinned = !previousValue;
        RebuildFavorites();
        RefreshClipboardViews();

        try
        {
            await _itemPersistenceService.UpdatePinAsync(item);
            SetStatus(item.IsPinned
                ? text => text.AddedToFavoritesStatus
                : text => text.RemovedFromFavoritesStatus);
        }
        catch (Exception ex)
        {
            item.IsPinned = previousValue;
            RebuildFavorites();
            RefreshClipboardViews();
            ShowError(_localization.PinUpdateFailedTitle, ex);
        }
        finally
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private bool CanCopy(object? parameter)
    {
        return parameter switch
        {
            FileInfoModel file => File.Exists(file.FilePath),
            TextModel text => !string.IsNullOrEmpty(text.Text),
            ImageModel image => HasUsableImage(image),
            UrlModel url => !string.IsNullOrWhiteSpace(url.Url),
            SecretModel secret => secret.ProtectedValue.Length > 0,
            _ => false
        };
    }

    private static bool CanDelete(object? parameter)
    {
        return parameter is FileInfoModel or TextModel or ImageModel or UrlModel or SecretModel;
    }

    private static bool CanTogglePin(object? parameter)
    {
        return parameter is IPinnedClipboardItem;
    }

    private static bool CanOpenFile(object? parameter)
    {
        return parameter is FileInfoModel file && File.Exists(file.FilePath);
    }

    private static bool CanOpenFavorite(object? parameter)
    {
        return (parameter is FavoriteClipboardItem { Source: FileInfoModel file } && File.Exists(file.FilePath))
            || (parameter is FavoriteClipboardItem { Source: UrlModel url }
                && Uri.TryCreate(url.Url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            || (parameter is FavoriteClipboardItem { Source: ImageModel image } && HasUsableImage(image));
    }
}
