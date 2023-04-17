using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ClipboardManager.Helper;
using ClipboardManager.Models;

namespace ClipboardManager.ViewModels;

public sealed partial class MainWindowViewModel
{
    private async Task OpenImagePreviewAsync(object? parameter)
    {
        var image = ResolveImageParameter(parameter);
        if (image is null)
        {
            return;
        }

        var imageSource = await EnsureImageSourceAsync(image);
        if (imageSource is null)
        {
            return;
        }

        PreviewImage = image;
        PreviewImageSource = imageSource;
        PreviewImageName = image.Name;
        PreviewZoom = 1.0;
        IsImagePreviewOpen = true;
        SetStatus(text => text.ImageOpenedStatus);
    }

    private async Task SaveImageAsync(object? parameter)
    {
        var image = ResolveImageParameter(parameter);
        if (image is null)
        {
            return;
        }

        try
        {
            var filePath = _transferDialogService.ShowSaveImageDialog(image.Name);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            var imageData = await _imageStorageService.GetImageDataForFileAsync(image, filePath);
            if (imageData.Length == 0)
            {
                return;
            }

            await File.WriteAllBytesAsync(filePath, imageData);
            SetStatus(text => text.ImageSavedStatus);
        }
        catch (Exception ex)
        {
            ShowError(_localization.SaveImageFailedTitle, ex);
        }
    }

    private void CloseImagePreview()
    {
        IsImagePreviewOpen = false;
        PreviewImage = null;
        PreviewImageSource = null;
        PreviewImageName = string.Empty;
        PreviewZoom = 1.0;
    }

    private void ZoomImage(double delta)
    {
        PreviewZoom += delta;
    }

    private static bool CanOpenImagePreview(object? parameter)
    {
        var image = ResolveImageParameter(parameter);
        return image is not null && HasUsableImage(image);
    }

    private static bool CanSaveImage(object? parameter)
    {
        var image = ResolveImageParameter(parameter);
        return image is not null && HasUsableImage(image);
    }

    private static bool HasUsableImage(ImageModel image)
    {
        return image.ImageSource is not null || image.ImageData.Length > 0;
    }

    private static ImageModel? ResolveImageParameter(object? parameter)
    {
        return parameter switch
        {
            ImageModel image => image,
            FavoriteClipboardItem { Source: ImageModel image } => image,
            _ => null
        };
    }

    private bool CanZoomInImage()
    {
        return IsImagePreviewOpen && PreviewZoom < MaxImagePreviewZoom;
    }

    private bool CanZoomOutImage()
    {
        return IsImagePreviewOpen && PreviewZoom > MinImagePreviewZoom;
    }

    private string? TryCreateImageHash(ImageModel image)
    {
        var imageSource = image.ImageSource ?? BitmapSourceExtensions.ByteArrayToBitmapSource(image.ImageData);
        return imageSource is null
            ? null
            : _clipboardService.CreateImageSignature(imageSource).Value;
    }

    private async Task<BitmapSource?> EnsureImageSourceAsync(
        ImageModel image,
        CancellationToken cancellationToken = default)
    {
        if (image.ImageSource is not null)
        {
            return image.ImageSource;
        }

        if (image.ImageData.Length == 0)
        {
            return null;
        }

        var imageSource = await Task.Run(
            () => BitmapSourceExtensions.ByteArrayToBitmapSource(image.ImageData),
            cancellationToken);

        if (imageSource is null)
        {
            return null;
        }

        image.ImageSource = imageSource;
        image.ThumbnailSource ??= BitmapSourceExtensions.ByteArrayToBitmapSource(
            image.ImageData,
            ImageThumbnailDecodePixelWidth);
        CommandManager.InvalidateRequerySuggested();

        return imageSource;
    }
}
