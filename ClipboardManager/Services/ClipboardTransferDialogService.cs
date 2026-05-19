using Microsoft.Win32;
using ClipboardManager.Localization;

namespace ClipboardManager.Services;

public interface IClipboardTransferDialogService
{
    string? ShowExportDialog();
    string? ShowImportDialog();
}

public sealed class ClipboardTransferDialogService : IClipboardTransferDialogService
{
    private readonly LocalizationService _localization;

    public ClipboardTransferDialogService(LocalizationService localization)
    {
        _localization = localization;
    }

    public string? ShowExportDialog()
    {
        var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = ".clipboard.json",
            FileName = $"clipboard-backup-{DateTime.Now:yyyyMMdd-HHmmss}.clipboard.json",
            Filter = _localization.BackupFilter,
            OverwritePrompt = true,
            Title = _localization.ExportDialogTitle
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
            Filter = _localization.BackupFilter,
            Multiselect = false,
            Title = _localization.ImportDialogTitle
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }
}
