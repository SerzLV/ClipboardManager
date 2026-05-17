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

    public async Task<ClipboardContentSnapshot?> GetCurrentSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (await ContainsFileDropListAsync(cancellationToken))
        {
            var filePaths = await GetFileDropListAsync(cancellationToken);
            if (filePaths.Count > 0)
            {
                return new ClipboardContentSnapshot(
                    ClipboardContentKind.FileDropList,
                    CreateFileDropListSignature(filePaths),
                    filePaths,
                    null,
                    null);
            }
        }

        if (await ContainsImageAsync(cancellationToken))
        {
            var image = await GetImageAsync(cancellationToken);
            if (image is not null)
            {
                var signature = image.IsFrozen
                    ? await Task.Run(() => CreateImageSignature(image), cancellationToken)
                    : CreateImageSignature(image);

                return new ClipboardContentSnapshot(
                    ClipboardContentKind.Image,
                    signature,
                    [],
                    null,
                    image);
            }
        }

        if (await ContainsTextAsync(cancellationToken))
        {
            var text = await GetTextAsync(cancellationToken);
            if (!string.IsNullOrEmpty(text))
            {
                return new ClipboardContentSnapshot(
                    ClipboardContentKind.Text,
                    new ClipboardContentSignature(ClipboardContentKind.Text, text),
                    [],
                    text,
                    null);
            }
        }

        return null;
    }

    public Task<ClipboardContentSignature> SetFileDropListAsync(
        IEnumerable<string> filePaths,
        CancellationToken cancellationToken = default)
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

        return ExecuteWithRetryAsync(
            () =>
            {
                Clipboard.SetFileDropList(collection);
                return CreateFileDropListSignature(normalizedPaths);
            },
            cancellationToken);
    }

    public Task<ClipboardContentSignature> SetTextAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithRetryAsync(
            () =>
            {
                Clipboard.SetText(text);
                return new ClipboardContentSignature(ClipboardContentKind.Text, text);
            },
            cancellationToken);
    }

    public Task<ClipboardContentSignature> SetImageAsync(
        BitmapSource image,
        CancellationToken cancellationToken = default)
    {
        return SetImageCoreAsync(image, cancellationToken);
    }

    private async Task<ClipboardContentSignature> SetImageCoreAsync(
        BitmapSource image,
        CancellationToken cancellationToken)
    {
        await ExecuteWithRetryAsync(
            () =>
            {
                Clipboard.SetImage(image);
                return true;
            },
            cancellationToken);

        return image.IsFrozen
            ? await Task.Run(() => CreateImageSignature(image), cancellationToken)
            : CreateImageSignature(image);
    }

    private static Task<bool> ContainsFileDropListAsync(CancellationToken cancellationToken)
    {
        return ExecuteWithRetryAsync(Clipboard.ContainsFileDropList, cancellationToken);
    }

    private static Task<IReadOnlyList<string>> GetFileDropListAsync(CancellationToken cancellationToken)
    {
        return ExecuteWithRetryAsync(
            () => (IReadOnlyList<string>)Clipboard.GetFileDropList().Cast<string>().ToArray(),
            cancellationToken);
    }

    private static Task<bool> ContainsTextAsync(CancellationToken cancellationToken)
    {
        return ExecuteWithRetryAsync(Clipboard.ContainsText, cancellationToken);
    }

    private static Task<string?> GetTextAsync(CancellationToken cancellationToken)
    {
        return ExecuteWithRetryAsync(() => (string?)Clipboard.GetText(), cancellationToken);
    }

    private static Task<bool> ContainsImageAsync(CancellationToken cancellationToken)
    {
        return ExecuteWithRetryAsync(Clipboard.ContainsImage, cancellationToken);
    }

    private static async Task<BitmapSource?> GetImageAsync(CancellationToken cancellationToken)
    {
        var image = await ExecuteWithRetryAsync(Clipboard.GetImage, cancellationToken);
        FreezeIfPossible(image);
        return image;
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

    private static async Task<T> ExecuteWithRetryAsync<T>(
        Func<T> action,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= RetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return action();
            }
            catch (Exception ex) when (IsTransientClipboardException(ex) && attempt < RetryCount)
            {
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }

        return action();
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
