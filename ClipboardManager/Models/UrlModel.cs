using System.Windows.Media.Imaging;

namespace ClipboardManager.Models;

public sealed class UrlModel : ClipboardItemModel
{
    private bool _isPreviewImageLoading;
    private BitmapSource? _previewImageSource;

    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime? MetadataUpdatedAt { get; set; }

    public BitmapSource? PreviewImageSource
    {
        get => _previewImageSource;
        set
        {
            if (_previewImageSource == value)
            {
                return;
            }

            _previewImageSource = value;
            OnPropertyChanged();
        }
    }

    public bool IsPreviewImageLoading
    {
        get => _isPreviewImageLoading;
        set
        {
            if (_isPreviewImageLoading == value)
            {
                return;
            }

            _isPreviewImageLoading = value;
            OnPropertyChanged();
        }
    }
}
