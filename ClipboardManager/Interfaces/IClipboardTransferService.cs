using ClipboardManager.Data;

namespace ClipboardManager.Interfaces;

public interface IClipboardTransferService
{
    Task ExportAsync(
        ClipboardData data,
        string filePath,
        CancellationToken cancellationToken = default);

    Task<ClipboardData> ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
