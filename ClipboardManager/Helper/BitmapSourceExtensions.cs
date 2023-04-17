using System.IO;
using System.Windows.Media;
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
        var source = PrepareBitmapSourceForEncoder(bitmapSource, format);
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    public static BitmapSource? ByteArrayToBitmapSource(byte[]? byteArray, int decodePixelWidth = 0)
    {
        if (byteArray is null || byteArray.Length == 0)
        {
            return null;
        }

        var bitmap = new BitmapImage();

        using var stream = new MemoryStream(byteArray);
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        if (decodePixelWidth > 0)
        {
            bitmap.DecodePixelWidth = decodePixelWidth;
        }

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

    private static BitmapSource PrepareBitmapSourceForEncoder(BitmapSource bitmapSource, string format)
    {
        if (!IsJpegFormat(format) || bitmapSource.Format == PixelFormats.Bgr24)
        {
            return bitmapSource;
        }

        var convertedBitmap = new FormatConvertedBitmap(
            bitmapSource,
            PixelFormats.Bgr24,
            null,
            0);

        if (convertedBitmap.CanFreeze && !convertedBitmap.IsFrozen)
        {
            convertedBitmap.Freeze();
        }

        return convertedBitmap;
    }

    private static bool IsJpegFormat(string format)
    {
        return format.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || format.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }
}
