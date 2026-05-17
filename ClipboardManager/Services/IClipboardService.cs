using System.Windows.Media.Imaging;

namespace ClipboardManager.Services;

public interface IClipboardService
{
    ClipboardContentSignature? GetCurrentSignature();
    bool ContainsFileDropList();
    IReadOnlyList<string> GetFileDropList();
    bool ContainsText();
    string? GetText();
    bool ContainsImage();
    BitmapSource? GetImage();
    ClipboardContentSignature SetFileDropList(IEnumerable<string> filePaths);
    ClipboardContentSignature SetText(string text);
    ClipboardContentSignature SetImage(BitmapSource image);
    ClipboardContentSignature CreateImageSignature(BitmapSource image);
}

public enum ClipboardContentKind
{
    FileDropList,
    Text,
    Image
}

public sealed record ClipboardContentSignature(ClipboardContentKind Kind, string Value);
