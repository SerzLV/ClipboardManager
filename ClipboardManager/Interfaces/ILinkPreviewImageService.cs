using System.Windows.Media.Imaging;

namespace ClipboardManager.Interfaces;

public interface ILinkPreviewImageService
{
    BitmapSource? DefaultImageSource { get; }
    bool CanLoadPreview(string imageUrl);
    bool HasCachedPreview(string imageUrl);
    bool TryLoadCachedPreview(
        string imageUrl,
        int decodePixelWidth,
        out BitmapSource? previewImage);
    Task<BitmapSource?> LoadPreviewAsync(
        string imageUrl,
        int decodePixelWidth,
        CancellationToken cancellationToken = default);
    Task<int> ClearCacheAsync(CancellationToken cancellationToken = default);
}
