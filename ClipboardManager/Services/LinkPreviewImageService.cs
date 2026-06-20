using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClipboardManager.Helper;
using ClipboardManager.Interfaces;

namespace ClipboardManager.Services;

public sealed class LinkPreviewImageService : ILinkPreviewImageService
{
    private const int MaxImageBytes = 2 * 1024 * 1024;
    private const string ApplicationDataDirectoryName = "ClipboardManager";
    private const string CacheDirectoryName = "Cache";
    private const string LinkPreviewCacheDirectoryName = "LinkPreviews";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private static readonly string CacheDirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ApplicationDataDirectoryName,
        CacheDirectoryName,
        LinkPreviewCacheDirectoryName);

    public LinkPreviewImageService()
    {
        DefaultImageSource = CreateDefaultImageSource();
    }

    public BitmapSource? DefaultImageSource { get; }

    public bool CanLoadPreview(string imageUrl)
    {
        if (LinkPreviewImageDefaults.IsDefault(imageUrl))
        {
            return false;
        }

        return Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps
                || uri.Scheme == Uri.UriSchemeFile);
    }

    public bool HasCachedPreview(string imageUrl)
    {
        if (LinkPreviewImageDefaults.IsDefault(imageUrl))
        {
            return true;
        }

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeFile)
        {
            return File.Exists(uri.LocalPath);
        }

        return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && File.Exists(GetCacheFilePath(uri));
    }

    public bool TryLoadCachedPreview(
        string imageUrl,
        int decodePixelWidth,
        out BitmapSource? previewImage)
    {
        previewImage = null;

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        try
        {
            if (uri.Scheme == Uri.UriSchemeFile)
            {
                previewImage = LoadLocalImage(uri.LocalPath, decodePixelWidth);
                return previewImage is not null;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            var cacheFilePath = GetCacheFilePath(uri);
            if (!File.Exists(cacheFilePath))
            {
                return false;
            }

            previewImage = LoadLocalImage(cacheFilePath, decodePixelWidth);
            return previewImage is not null;
        }
        catch
        {
            previewImage = null;
            return false;
        }
    }

    public async Task<BitmapSource?> LoadPreviewAsync(
        string imageUrl,
        int decodePixelWidth,
        CancellationToken cancellationToken = default)
    {
        if (LinkPreviewImageDefaults.IsDefault(imageUrl))
        {
            return DefaultImageSource;
        }

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return DefaultImageSource;
        }

        try
        {
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                if (TryLoadCachedPreview(imageUrl, decodePixelWidth, out var cachedPreview)
                    && cachedPreview is not null)
                {
                    return cachedPreview;
                }

                return await LoadRemoteImageAsync(uri, decodePixelWidth, cancellationToken)
                    .ConfigureAwait(false) ?? DefaultImageSource;
            }

            return uri.Scheme == Uri.UriSchemeFile
                ? LoadLocalImage(uri.LocalPath, decodePixelWidth) ?? DefaultImageSource
                : DefaultImageSource;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            if (TryLoadCachedPreview(imageUrl, decodePixelWidth, out var cachedPreview))
            {
                return cachedPreview;
            }

            return DefaultImageSource;
        }
    }

    public Task<int> ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                if (!Directory.Exists(CacheDirectoryPath))
                {
                    return 0;
                }

                var deletedCount = 0;
                foreach (var filePath in EnumerateCacheFiles())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        File.Delete(filePath);
                        if (filePath.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
                        {
                            deletedCount++;
                        }
                    }
                    catch
                    {
                    }
                }

                return deletedCount;
            },
            cancellationToken);
    }

    private static async Task<BitmapSource?> LoadRemoteImageAsync(
        Uri uri,
        int decodePixelWidth,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd("ClipboardManager/1.0");

        using var response = await HttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var imageData = await ReadLimitedBytesAsync(response.Content, cancellationToken)
            .ConfigureAwait(false);
        var previewImage = BitmapSourceExtensions.ByteArrayToBitmapSource(imageData, decodePixelWidth);
        if (previewImage is not null)
        {
            await WriteCacheAsync(uri, imageData, cancellationToken).ConfigureAwait(false);
        }

        return previewImage;
    }

    private static BitmapSource? LoadLocalImage(string filePath, int decodePixelWidth)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var imageData = File.ReadAllBytes(filePath);
        return BitmapSourceExtensions.ByteArrayToBitmapSource(imageData, decodePixelWidth);
    }

    private static BitmapSource CreateDefaultImageSource()
    {
        const int size = 96;

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var backgroundBrush = new SolidColorBrush(Color.FromRgb(240, 253, 250));
            var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(153, 246, 228)), 1.5);
            var cardBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            var mutedPen = new Pen(new SolidColorBrush(Color.FromRgb(148, 163, 184)), 2);
            var accentBrush = new SolidColorBrush(Color.FromRgb(20, 184, 166));
            var accentPen = new Pen(accentBrush, 3);

            context.DrawRoundedRectangle(
                backgroundBrush,
                borderPen,
                new Rect(0.75, 0.75, size - 1.5, size - 1.5),
                18,
                18);

            context.DrawRoundedRectangle(
                cardBrush,
                mutedPen,
                new Rect(23, 26, 50, 42),
                7,
                7);

            context.DrawEllipse(
                accentBrush,
                null,
                new Point(60, 40),
                4,
                4);

            var mountain = new StreamGeometry();
            using (var geometry = mountain.Open())
            {
                geometry.BeginFigure(new Point(29, 63), true, true);
                geometry.LineTo(new Point(42, 49), true, false);
                geometry.LineTo(new Point(51, 58), true, false);
                geometry.LineTo(new Point(57, 52), true, false);
                geometry.LineTo(new Point(68, 64), true, false);
            }

            mountain.Freeze();
            context.DrawGeometry(accentBrush, null, mountain);

            context.DrawLine(accentPen, new Point(33, 76), new Point(63, 76));
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static async Task WriteCacheAsync(
        Uri uri,
        byte[] imageData,
        CancellationToken cancellationToken)
    {
        if (imageData.Length == 0)
        {
            return;
        }

        Directory.CreateDirectory(CacheDirectoryPath);

        var cacheFilePath = GetCacheFilePath(uri);
        var temporaryFilePath = $"{cacheFilePath}.{Guid.NewGuid():N}.tmp";

        await File.WriteAllBytesAsync(temporaryFilePath, imageData, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            File.Move(temporaryFilePath, cacheFilePath, true);
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryFilePath);
        }
    }

    private static IEnumerable<string> EnumerateCacheFiles()
    {
        return Directory.EnumerateFiles(CacheDirectoryPath, "*.img")
            .Concat(Directory.EnumerateFiles(CacheDirectoryPath, "*.tmp"));
    }

    private static void TryDeleteTemporaryFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
        }
    }

    private static string GetCacheFilePath(Uri uri)
    {
        var normalizedUrl = uri.AbsoluteUri;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedUrl));
        return Path.Combine(CacheDirectoryPath, $"{Convert.ToHexString(hash)}.img");
    }

    private static async Task<byte[]> ReadLimitedBytesAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        using var memory = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(8192);

        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(buffer, cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                if (memory.Length + read > MaxImageBytes)
                {
                    throw new InvalidDataException("Link preview image is too large.");
                }

                memory.Write(buffer, 0, read);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return memory.ToArray();
    }
}
