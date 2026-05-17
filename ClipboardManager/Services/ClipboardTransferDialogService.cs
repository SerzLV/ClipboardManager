using Microsoft.Win32;

namespace ClipboardManager.Services;

public interface IClipboardTransferDialogService
{
    string? ShowExportDialog();
    string? ShowImportDialog();
}

public sealed class ClipboardTransferDialogService : IClipboardTransferDialogService
{
    private const string BackupFilter =
        "Clipboard Manager backup (*.clipboard.json)|*.clipboard.json|JSON files (*.json)|*.json";

    public string? ShowExportDialog()
    {
        var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = ".clipboard.json",
            FileName = $"clipboard-backup-{DateTime.Now:yyyyMMdd-HHmmss}.clipboard.json",
            Filter = BackupFilter,
            OverwritePrompt = true,
            Title = "Экспорт истории буфера обмена"
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }

    public string? ShowImportDialog()
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            DefaultExt = ".clipboard.json",
            Filter = BackupFilter,
            Multiselect = false,
            Title = "Импорт истории буфера обмена"
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }
}
