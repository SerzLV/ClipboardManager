using ClipboardManager.Data;
using ClipboardManager.Interfaces;

namespace ClipboardManager.Services;

public sealed class ClipboardItemLookupService(IClipboardRepository repository) : IClipboardItemLookupService
{
    public Task<bool> FileExistsAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        return repository.FileExistsAsync(filePath, cancellationToken);
    }

    public Task<bool> TextExistsAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        return repository.TextExistsAsync(text, cancellationToken);
    }

    public Task<IReadOnlySet<string>> FindExistingUrlsAsync(
        IReadOnlyCollection<string> urls,
        CancellationToken cancellationToken = default)
    {
        return repository.FindExistingUrlsAsync(urls, cancellationToken);
    }
}
