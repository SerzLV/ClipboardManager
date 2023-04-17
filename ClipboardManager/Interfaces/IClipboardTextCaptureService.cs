using ClipboardManager.Models;

namespace ClipboardManager.Interfaces;

public interface IClipboardTextCaptureService
{
    Task<bool> ShouldCaptureTextAsync(
        string text,
        IReadOnlySet<string> knownTexts,
        IReadOnlySet<string> knownUrlTextValues,
        CancellationToken cancellationToken = default);

    IReadOnlyList<string> ExtractUrlCandidates(
        string text,
        IReadOnlySet<string> knownUrls);

    Task<IReadOnlyList<UrlModel>> LoadNewUrlMetadataAsync(
        IReadOnlyCollection<string> urls,
        CancellationToken cancellationToken = default);
}
