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

        if (knownTexts.Contains(text)
            || knownUrlTextValues.Contains(text)
            || await repository.TextExistsAsync(text, cancellationToken))
        {
            return false;
        }

        var existingTextUrl = await repository.FindExistingUrlsAsync([text], cancellationToken);
        return !existingTextUrl.Contains(text);
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
            .Select(match => match.Value.TrimEnd('.', ',', ';', ')', ']'))
            .Where(url => !knownUrls.Contains(url))
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
}
