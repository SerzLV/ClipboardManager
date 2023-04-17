namespace ClipboardManager.Interfaces;

public interface IClipboardItemLookupService
{
    Task<bool> FileExistsAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    Task<bool> TextExistsAsync(
        string text,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> FindExistingUrlsAsync(
        IReadOnlyCollection<string> urls,
        CancellationToken cancellationToken = default);
}
