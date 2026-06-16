using System.Text.RegularExpressions;
using ClipboardManager.Data;
using ClipboardManager.Interfaces;
using ClipboardManager.Models;

namespace ClipboardManager.Services;

public sealed class ClipboardTextCaptureService(
    IClipboardRepository repository,
    ILinkMetadataService linkMetadataService) : IClipboardTextCaptureService
{
    private const int MaxUrlPreviewConcurrency = 4;
    private static readonly Regex UrlRegex = new(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled);
    private static readonly char[] UrlLeadingTrimChars = ['(', '[', '{', '<', '"', '\''];
    private static readonly char[] UrlTrailingTrimChars = ['.', ',', ';', ':', '!', '?', ')', ']', '}', '>', '"', '\''];

    public async Task<bool> ShouldCaptureTextAsync(
        string text,
        IReadOnlySet<string> knownTexts,
        IReadOnlySet<string> knownUrlTextValues,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalizedTextUrl = NormalizeUrlCandidate(text);
        if (normalizedTextUrl is not null && knownUrlTextValues.Contains(normalizedTextUrl))
        {
            return false;
        }

        if (knownTexts.Contains(text)
            || knownUrlTextValues.Contains(text)
            || await repository.TextExistsAsync(text, cancellationToken))
        {
            return false;
        }

        var urlCandidates = normalizedTextUrl is null
            ? [text]
            : new[] { text, normalizedTextUrl };
        var existingTextUrl = await repository.FindExistingUrlsAsync(urlCandidates, cancellationToken);
        return !existingTextUrl.Contains(text)
            && (normalizedTextUrl is null || !existingTextUrl.Contains(normalizedTextUrl));
    }

    public IReadOnlyList<string> ExtractUrlCandidates(
        string text,
        IReadOnlySet<string> knownUrls)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return UrlRegex.Matches(text)
            .Select(match => NormalizeUrlCandidate(match.Value))
            .Where(url => url is not null && !knownUrls.Contains(url))
            .Select(url => url!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<UrlModel>> LoadNewUrlMetadataAsync(
        IReadOnlyCollection<string> urls,
        CancellationToken cancellationToken = default)
    {
        if (urls.Count == 0)
        {
            return [];
        }

        var existingUrls = await repository.FindExistingUrlsAsync(urls, cancellationToken);
        var newUrls = urls
            .Where(url => !existingUrls.Contains(url))
            .ToArray();
        if (newUrls.Length == 0)
        {
            return [];
        }

        var metadata = await LoadMetadataAsync(newUrls, cancellationToken);
        return metadata
            .Where(url => url is not null)
            .Select(url => url!)
            .ToArray();
    }

    private async Task<UrlModel?[]> LoadMetadataAsync(
        IReadOnlyCollection<string> urls,
        CancellationToken cancellationToken)
    {
        if (urls.Count == 0)
        {
            return [];
        }

        using var throttle = new SemaphoreSlim(MaxUrlPreviewConcurrency);
        var tasks = urls.Select(async url =>
        {
            await throttle.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return await linkMetadataService.GetMetadataAsync(url, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                throttle.Release();
            }
        });

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static string? NormalizeUrlCandidate(string value)
    {
        var trimmedValue = value
            .Trim()
            .TrimStart(UrlLeadingTrimChars)
            .TrimEnd(UrlTrailingTrimChars);
        if (string.IsNullOrWhiteSpace(trimmedValue))
        {
            return null;
        }

        var normalizedValue = trimmedValue.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? $"https://{trimmedValue}"
            : trimmedValue;

        return Uri.TryCreate(normalizedValue, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.ToString()
            : null;
    }
}
