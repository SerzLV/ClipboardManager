using System.Windows.Media.Imaging;

namespace ClipboardManager.Interfaces;

public interface IClipboardService
{
    Task<ClipboardContentSnapshot?> GetCurrentSnapshotAsync(CancellationToken cancellationToken = default);
    Task<ClipboardContentSignature> SetFileDropListAsync(
        IEnumerable<string> filePaths,
        CancellationToken cancellationToken = default);
    Task<ClipboardContentSignature> SetTextAsync(string text, CancellationToken cancellationToken = default);
    Task<ClipboardContentSignature> SetImageAsync(BitmapSource image, CancellationToken cancellationToken = default);
    Task<bool> ClearIfCurrentSignatureMatchesAsync(
        ClipboardContentSignature signature,
        CancellationToken cancellationToken = default);
    Task<bool> ClearTextIfCurrentTextEqualsAsync(
        string text,
        CancellationToken cancellationToken = default);
    ClipboardContentSignature CreateImageSignature(BitmapSource image);
}

public enum ClipboardContentKind
{
    FileDropList,
    Text,
    Image
}

public sealed record ClipboardContentSignature(ClipboardContentKind Kind, string Value);

public sealed record ClipboardContentSnapshot(
    ClipboardContentKind Kind,
    ClipboardContentSignature Signature,
    IReadOnlyList<string> FilePaths,
    string? Text,
    BitmapSource? Image);
