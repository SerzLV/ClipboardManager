using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClipboardManager.Helper
{
    public static class BitmapSourceExtensions
    {
        public static byte[] ConvertBitmapSourceToByteArray(BitmapSource bitmapSource, string format)
        {
            if (bitmapSource == null) return null;

            var encoder = GetBitmapEncoder(format);
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        private static BitmapEncoder GetBitmapEncoder(string format)
        {
            switch (format.ToLower())
            {
                case ".bmp":
                    return new BmpBitmapEncoder();
                case ".jpg":
                case ".jpeg":
                    return new JpegBitmapEncoder();
                case ".png":
                    return new PngBitmapEncoder();
                case ".gif":
                    return new GifBitmapEncoder();
                default:
                    throw new NotSupportedException($"Unsupported image format: {format}");
            }
        }

        public static BitmapSource ByteArrayToBitmapSource(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length == 0)
            {
                return null;
            }

            var bitmap = new BitmapImage();

            using (var stream = new MemoryStream(byteArray))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }

            return bitmap;
        }
    }
}
