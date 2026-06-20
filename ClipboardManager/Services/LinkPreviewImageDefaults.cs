namespace ClipboardManager.Services;

internal static class LinkPreviewImageDefaults
{
    public const string ImageUrl = "clipboardmanager://link-preview/default";

    public static bool IsDefault(string? imageUrl)
    {
        return string.Equals(imageUrl?.Trim(), ImageUrl, StringComparison.OrdinalIgnoreCase);
    }
}
