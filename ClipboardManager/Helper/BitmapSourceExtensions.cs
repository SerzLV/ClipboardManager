using System.IO;
using System.Windows.Media.Imaging;

namespace ClipboardManager.Helper;

public static class BitmapSourceExtensions
{
    public static byte[] ConvertBitmapSourceToByteArray(BitmapSource? bitmapSource, string format)
    {
        if (bitmapSource is null)
        {
            return [];
        }

        var encoder = GetBitmapEncoder(format);
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    public static BitmapSource? ByteArrayToBitmapSource(byte[]? byteArray)
    {
        if (byteArray is null || byteArray.Length == 0)
        {
            return null;
        }

        var bitmap = new BitmapImage();

        using var stream = new MemoryStream(byteArray);
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();

        return bitmap;
    }

    private static BitmapEncoder GetBitmapEncoder(string format)
    {
        return format.ToLowerInvariant() switch
        {
            ".bmp" => new BmpBitmapEncoder(),
            ".jpg" or ".jpeg" => new JpegBitmapEncoder(),
            ".png" => new PngBitmapEncoder(),
            ".gif" => new GifBitmapEncoder(),
            _ => throw new NotSupportedException($"Unsupported image format: {format}")
        };
    }
}
