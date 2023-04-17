using ClipboardManager.Interfaces;
using ClipboardManager.Models;
using HtmlAgilityPack;
using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Text;

namespace ClipboardManager.Services;

public sealed class LinkMetadataService : ILinkMetadataService
{
    private const int MaxHtmlBytes = 1024 * 1024;

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private static readonly string DefaultImage = Path.Combine(
        AppContext.BaseDirectory,
        "Resources",
        "Images",
        "noImage.png");

    public async Task<UrlModel?> GetMetadataAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!TryCreateSupportedUri(url, out var uri))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("ClipboardManager/1.0");

            using var response = await HttpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return CreateFallbackUrl(uri);
            }

            var html = await ReadLimitedStringAsync(response.Content, cancellationToken)
                .ConfigureAwait(false);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var imageUrl = document.DocumentNode
                .SelectSingleNode("//head/meta[@property='og:image']")
                ?.GetAttributeValue("content", string.Empty);

            return new UrlModel
            {
                Url = uri.ToString(),
                Title = Decode(document.DocumentNode.SelectSingleNode("//head/title")?.InnerText) ?? uri.Host,
                Description = Decode(document.DocumentNode
                    .SelectSingleNode("//head/meta[@name='description']")
                    ?.GetAttributeValue("content", string.Empty)) ?? string.Empty,
                ImageUrl = ResolveImageUrl(uri, imageUrl),
                MetadataUpdatedAt = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return CreateFallbackUrl(uri);
        }
    }

    private static UrlModel CreateFallbackUrl(Uri uri)
    {
        return new UrlModel
        {
            Url = uri.ToString(),
            Title = uri.Host,
            ImageUrl = DefaultImage,
            MetadataUpdatedAt = DateTime.UtcNow
        };
    }

    private static bool TryCreateSupportedUri(string url, out Uri uri)
    {
        var normalizedUrl = url.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? $"https://{url}"
            : url;

        if (Uri.TryCreate(normalizedUrl, UriKind.Absolute, out uri!)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return true;
        }

        uri = null!;
        return false;
    }

    private static string ResolveImageUrl(Uri pageUri, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return DefaultImage;
        }

        return Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteImageUri)
            ? absoluteImageUri.ToString()
            : new Uri(pageUri, imageUrl).ToString();
    }

    private static string? Decode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : HtmlEntity.DeEntitize(value).Trim();
    }

    private static async Task<string> ReadLimitedStringAsync(
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

                if (memory.Length + read > MaxHtmlBytes)
                {
                    throw new InvalidDataException("HTML response is too large.");
                }

                memory.Write(buffer, 0, read);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        var encoding = TryGetEncoding(content) ?? Encoding.UTF8;
        return encoding.GetString(memory.GetBuffer(), 0, checked((int)memory.Length));
    }

    private static Encoding? TryGetEncoding(HttpContent content)
    {
        var charset = content.Headers.ContentType?.CharSet;
        if (string.IsNullOrWhiteSpace(charset))
        {
            return null;
        }

        try
        {
            return Encoding.GetEncoding(charset.Trim('"'));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
