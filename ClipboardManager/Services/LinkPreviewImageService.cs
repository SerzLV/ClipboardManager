using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
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

    private static readonly string DefaultImagePath = Path.Combine(
        AppContext.BaseDirectory,
        "Resources",
        "Images",
        "noImage.png");

    private static readonly string CacheDirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ApplicationDataDirectoryName,
        CacheDirectoryName,
        LinkPreviewCacheDirectoryName);

    public LinkPreviewImageService()
    {
        DefaultImageSource = LoadLocalImage(DefaultImagePath, 96);
    }

    public BitmapSource? DefaultImageSource { get; }

    public bool CanLoadPreview(string imageUrl)
    {
        return Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps
                || uri.Scheme == Uri.UriSchemeFile);
    }

    public bool HasCachedPreview(string imageUrl)
    {
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
                foreach (var filePath in Directory.EnumerateFiles(CacheDirectoryPath, "*.img"))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        File.Delete(filePath);
                        deletedCount++;
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

        File.Move(temporaryFilePath, cacheFilePath, true);
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
