namespace ClipboardManager.Models;

public sealed class FileInfoModel : ClipboardItemModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}
