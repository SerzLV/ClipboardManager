using System.Windows.Media.Imaging;

namespace ClipboardManager.Models;

public sealed class ImageModel : ClipboardItemModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte[] ImageData { get; set; } = [];

    private BitmapSource? _imageSource;
    private BitmapSource? _thumbnailSource;

    public BitmapSource? ImageSource
    {
        get => _imageSource;
        set
        {
            if (_imageSource == value)
            {
                return;
            }

            _imageSource = value;
            OnPropertyChanged();
        }
    }

    public BitmapSource? ThumbnailSource
    {
        get => _thumbnailSource;
        set
        {
            if (_thumbnailSource == value)
            {
                return;
            }

            _thumbnailSource = value;
            OnPropertyChanged();
        }
    }
}
