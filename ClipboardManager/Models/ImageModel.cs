using System.Windows.Media.Imaging;

namespace ClipboardManager.Models;

public sealed class ImageModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte[] ImageData { get; set; } = [];
    public BitmapSource? ImageSource { get; set; }
}
