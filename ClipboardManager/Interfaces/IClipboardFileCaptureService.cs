using ClipboardManager.Models;

namespace ClipboardManager.Interfaces;

public interface IClipboardFileCaptureService
{
    Task<FileInfoModel?> TryCaptureFileAsync(
        string filePath,
        IReadOnlySet<string> knownFilePaths,
        CancellationToken cancellationToken = default);
}
