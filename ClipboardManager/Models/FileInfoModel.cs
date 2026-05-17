namespace ClipboardManager.Models;

public sealed class FileInfoModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}
