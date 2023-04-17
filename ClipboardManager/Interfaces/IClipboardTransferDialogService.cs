namespace ClipboardManager.Interfaces;

public interface IClipboardTransferDialogService
{
    string? ShowExportDialog();
    string? ShowImportDialog();
    string? ShowSaveImageDialog(string suggestedFileName);
}
