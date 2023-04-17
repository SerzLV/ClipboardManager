namespace ClipboardManager.Models;

public sealed class TextModel : ClipboardItemModel
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
}
