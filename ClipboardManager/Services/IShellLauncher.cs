namespace ClipboardManager.Services;

public interface IShellLauncher
{
    void OpenFile(string filePath);
    void OpenUrl(string url);
}
