using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using ClipboardManager.Helper;

namespace ClipboardManager.Services;

public sealed class WpfClipboardService : IClipboardService
{
    private const char FileSeparator = '\u001F';
    private const int RetryCount = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(40);

    public ClipboardContentSignature? GetCurrentSignature()
    {
        if (ContainsFileDropList())
        {
            var filePaths = GetFileDropList();
            if (filePaths.Count > 0)
            {
                return CreateFileDropListSignature(filePaths);
            }
        }

        if (ContainsImage())
        {
            var image = GetImage();
            if (image is not null)
            {
                return CreateImageSignature(image);
            }
        }

        if (ContainsText())
        {
            var text = GetText();
            if (!string.IsNullOrEmpty(text))
            {
                return new ClipboardContentSignature(ClipboardContentKind.Text, text);
            }
        }

        return null;
    }

    public bool ContainsFileDropList()
    {
        return ExecuteWithRetry(Clipboard.ContainsFileDropList);
    }

    public IReadOnlyList<string> GetFileDropList()
    {
        return ExecuteWithRetry(() => Clipboard.GetFileDropList().Cast<string>().ToArray());
    }

    public bool ContainsText()
    {
        return ExecuteWithRetry(Clipboard.ContainsText);
    }

    public string? GetText()
    {
        return ContainsText()
            ? ExecuteWithRetry(Clipboard.GetText)
            : null;
    }

    public bool ContainsImage()
    {
        return ExecuteWithRetry(Clipboard.ContainsImage);
    }

    public BitmapSource? GetImage()
    {
        if (!ContainsImage())
        {
            return null;
        }

        var image = ExecuteWithRetry(Clipboard.GetImage);
        FreezeIfPossible(image);
        return image;
    }

    public ClipboardContentSignature SetFileDropList(IEnumerable<string> filePaths)
    {
        var normalizedPaths = filePaths
            .Where(File.Exists)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedPaths.Length == 0)
        {
            throw new InvalidOperationException("There are no existing files to copy.");
        }

        var collection = new StringCollection();
        collection.AddRange(normalizedPaths);

        ExecuteWithRetry(() => Clipboard.SetFileDropList(collection));
        return CreateFileDropListSignature(normalizedPaths);
    }

    public ClipboardContentSignature SetText(string text)
    {
        ExecuteWithRetry(() => Clipboard.SetText(text));
        return new ClipboardContentSignature(ClipboardContentKind.Text, text);
    }

    public ClipboardContentSignature SetImage(BitmapSource image)
    {
        ExecuteWithRetry(() => Clipboard.SetImage(image));
        return CreateImageSignature(image);
    }

    public ClipboardContentSignature CreateImageSignature(BitmapSource image)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        AppendInt(hash, image.PixelWidth);
        AppendInt(hash, image.PixelHeight);
        AppendInt(hash, image.Format.BitsPerPixel);
        AppendString(hash, image.Format.ToString());

        if (image.Palette is not null)
        {
            foreach (var color in image.Palette.Colors)
            {
                hash.AppendData([color.A, color.R, color.G, color.B]);
            }
        }

        if (image.Format.BitsPerPixel <= 0)
        {
            hash.AppendData(BitmapSourceExtensions.ConvertBitmapSourceToByteArray(image, ".png"));
            return new ClipboardContentSignature(ClipboardContentKind.Image, Convert.ToHexString(hash.GetHashAndReset()));
        }

        var stride = checked((image.PixelWidth * image.Format.BitsPerPixel + 7) / 8);
        var pixels = new byte[checked(stride * image.PixelHeight)];
        image.CopyPixels(pixels, stride, 0);
        hash.AppendData(pixels);

        return new ClipboardContentSignature(ClipboardContentKind.Image, Convert.ToHexString(hash.GetHashAndReset()));
    }

    private static ClipboardContentSignature CreateFileDropListSignature(IEnumerable<string> filePaths)
    {
        var value = string.Join(
            FileSeparator,
            filePaths
                .Select(Path.GetFullPath)
                .Order(StringComparer.OrdinalIgnoreCase)
                .Select(path => path.ToUpperInvariant()));

        return new ClipboardContentSignature(ClipboardContentKind.FileDropList, value);
    }

    private static T ExecuteWithRetry<T>(Func<T> action)
    {
        for (var attempt = 1; attempt <= RetryCount; attempt++)
        {
            try
            {
                return action();
            }
            catch (Exception ex) when (IsTransientClipboardException(ex) && attempt < RetryCount)
            {
                Thread.Sleep(RetryDelay);
            }
        }

        return action();
    }

    private static void ExecuteWithRetry(Action action)
    {
        ExecuteWithRetry(() =>
        {
            action();
            return true;
        });
    }

    private static bool IsTransientClipboardException(Exception exception)
    {
        return exception is ExternalException or COMException;
    }

    private static void FreezeIfPossible(BitmapSource? image)
    {
        if (image?.CanFreeze == true && !image.IsFrozen)
        {
            image.Freeze();
        }
    }

    private static void AppendInt(IncrementalHash hash, int value)
    {
        hash.AppendData(BitConverter.GetBytes(value));
    }

    private static void AppendString(IncrementalHash hash, string value)
    {
        hash.AppendData(Encoding.UTF8.GetBytes(value));
    }
}
