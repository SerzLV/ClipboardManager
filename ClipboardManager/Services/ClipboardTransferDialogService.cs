using Microsoft.Win32;
using ClipboardManager.Interfaces;
using ClipboardManager.Localization;
using System.IO;

namespace ClipboardManager.Services;

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

    public string? ShowSaveImageDialog(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = ".png",
            FileName = CreateSafeImageFileName(suggestedFileName),
            Filter = _localization.ImageSaveFilter,
            OverwritePrompt = true,
            Title = _localization.SaveImageDialogTitle
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }

    private static string CreateSafeImageFileName(string suggestedFileName)
    {
        var fileName = Path.GetFileNameWithoutExtension(suggestedFileName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = $"clipboard-image-{DateTime.Now:yyyyMMdd-HHmmss}";
        }

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return fileName;
    }
}
