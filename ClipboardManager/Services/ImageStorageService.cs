using System.IO;
using System.Windows.Media.Imaging;
using ClipboardManager.Helper;
using ClipboardManager.Interfaces;
using ClipboardManager.Models;

namespace ClipboardManager.Services;

public sealed class ImageStorageService : IImageStorageService
{
    public async Task PrepareForStorageAsync(
        IReadOnlyCollection<ImageModel> images,
        CancellationToken cancellationToken = default)
    {
        var pendingImages = images
            .Where(x => x.Id == 0 && x.ImageSource is not null && x.ImageData.Length == 0)
            .Select(x => (Image: x, Source: x.ImageSource!))
            .ToArray();

        if (pendingImages.Length == 0)
        {
            return;
        }

        foreach (var (_, source) in pendingImages)
        {
            if (source.CanFreeze && !source.IsFrozen)
            {
                source.Freeze();
            }
        }

        foreach (var (image, source) in pendingImages)
        {
            image.ThumbnailSource ??= source;
        }

        var backgroundImages = pendingImages
            .Where(x => x.Source.IsFrozen)
            .ToArray();
        var uiThreadImages = pendingImages
            .Where(x => !x.Source.IsFrozen)
            .ToArray();

        foreach (var (image, source) in uiThreadImages)
        {
            image.ImageData = BitmapSourceExtensions.ConvertBitmapSourceToByteArray(source, ".png");
        }

        if (backgroundImages.Length == 0)
        {
            return;
        }

        var encodedImages = await Task.Run(
            () =>
            {
                return backgroundImages
                    .Select(item =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        return (
                            item.Image,
                            Data: BitmapSourceExtensions.ConvertBitmapSourceToByteArray(item.Source, ".png"));
                    })
                    .ToArray();
            },
            cancellationToken);

        foreach (var (image, imageData) in encodedImages)
        {
            image.ImageData = imageData;
        }
    }

    public async Task<byte[]> GetImageDataForFileAsync(
        ImageModel image,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var format = GetImageSaveFormat(filePath);
        if (format == ".png" && image.ImageData.Length > 0)
        {
            return image.ImageData;
        }

        var imageSource = await GetImageSourceAsync(image, cancellationToken);
        if (imageSource is null)
        {
            return [];
        }

        var imageData = imageSource.IsFrozen
            ? await Task.Run(
                () => BitmapSourceExtensions.ConvertBitmapSourceToByteArray(imageSource, format),
                cancellationToken)
            : BitmapSourceExtensions.ConvertBitmapSourceToByteArray(imageSource, format);

        if (format == ".png")
        {
            image.ImageData = imageData;
        }

        return imageData;
    }

    private static async Task<BitmapSource?> GetImageSourceAsync(
        ImageModel image,
        CancellationToken cancellationToken)
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

        if (imageSource is not null)
        {
            image.ImageSource = imageSource;
        }

        return imageSource;
    }

    private static string GetImageSaveFormat(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".bmp" => ".bmp",
            ".jpg" or ".jpeg" => ".jpg",
            _ => ".png"
        };
    }
}
