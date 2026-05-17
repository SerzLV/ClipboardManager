using ClipboardManager.Models;
using HtmlAgilityPack;
using System.IO;

namespace ClipboardManager.Services;

public sealed class LinkMetadataService : ILinkMetadataService
{
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
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(uri.ToString(), cancellationToken);
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
                ImageUrl = ResolveImageUrl(uri, imageUrl)
            };
        }
        catch
        {
            return new UrlModel
            {
                Url = uri.ToString(),
                Title = uri.Host,
                ImageUrl = DefaultImage
            };
        }
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
}
