using System.Diagnostics;
using System.IO;
using ClipboardManager.Interfaces;

namespace ClipboardManager.Services;

public sealed class ShellLauncher : IShellLauncher
{
    public void OpenFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            Start(filePath);
        }
    }

    public void OpenUrl(string url)
    {
        Start(url);
    }

    private static void Start(string target)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = target,
            UseShellExecute = true
        });
    }
}
