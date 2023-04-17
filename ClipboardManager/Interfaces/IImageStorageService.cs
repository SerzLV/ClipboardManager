using ClipboardManager.Models;

namespace ClipboardManager.Interfaces;

public interface IImageStorageService
{
    Task PrepareForStorageAsync(
        IReadOnlyCollection<ImageModel> images,
        CancellationToken cancellationToken = default);

    Task<byte[]> GetImageDataForFileAsync(
        ImageModel image,
        string filePath,
        CancellationToken cancellationToken = default);
}
