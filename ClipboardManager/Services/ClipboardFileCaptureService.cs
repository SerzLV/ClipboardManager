using System.IO;
using ClipboardManager.Data;
using ClipboardManager.Interfaces;
using ClipboardManager.Models;

namespace ClipboardManager.Services;

public sealed class ClipboardFileCaptureService(IClipboardRepository repository) : IClipboardFileCaptureService
{
    public async Task<FileInfoModel?> TryCaptureFileAsync(
        string filePath,
        IReadOnlySet<string> knownFilePaths,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var normalizedFilePath = Path.GetFullPath(filePath);
        if (knownFilePaths.Contains(normalizedFilePath)
            || await repository.FileExistsAsync(normalizedFilePath, cancellationToken))
        {
            return null;
        }

        return new FileInfoModel
        {
            FilePath = normalizedFilePath,
            Name = Path.GetFileName(normalizedFilePath)
        };
    }
}
