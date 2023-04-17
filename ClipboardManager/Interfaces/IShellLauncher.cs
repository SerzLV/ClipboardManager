namespace ClipboardManager.Interfaces;

public interface IShellLauncher
{
    void OpenFile(string filePath);
    void OpenUrl(string url);
}
