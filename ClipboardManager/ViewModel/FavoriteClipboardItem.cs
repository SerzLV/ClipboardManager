using ClipboardManager.Localization;
using ClipboardManager.Models;

namespace ClipboardManager.ViewModels;

public sealed class FavoriteClipboardItem
{
    private const int TitleLimit = 64;
    private const int DescriptionLimit = 180;
    private readonly LocalizationService _localization;

    public FavoriteClipboardItem(ClipboardItemModel source, LocalizationService localization)
    {
        Source = source;
        _localization = localization;

        switch (source)
        {
            case FileInfoModel file:
                TypeLabel = localization.FileTypeLabel;
                Title = Shorten(file.Name, TitleLimit);
                Subtitle = Shorten(file.FilePath, DescriptionLimit);
                Description = file.FilePath;
                ShowsIconPreview = true;
                break;
            case TextModel text:
                TypeLabel = localization.TextTypeLabel;
                Title = Shorten(GetFirstLine(text.Text), TitleLimit);
                Subtitle = localization.TextRecordLabel;
                Description = Shorten(text.Text, DescriptionLimit);
                ShowsTextPreview = true;
                break;
            case UrlModel url:
                TypeLabel = localization.LinkTypeLabel;
                Title = Shorten(string.IsNullOrWhiteSpace(url.Title) ? url.Url : url.Title, TitleLimit);
                Subtitle = Shorten(url.Url, DescriptionLimit);
                Description = Shorten(url.Description, DescriptionLimit);
                PreviewSource = url.PreviewImageSource;
                ShowsImagePreview = PreviewSource is not null;
                ShowsIconPreview = !ShowsImagePreview;
                HasOpenAction = true;
                break;
            case ImageModel image:
                TypeLabel = localization.ImageTypeLabel;
                Title = Shorten(image.Name, TitleLimit);
                Subtitle = localization.ImageTypeLabel;
                Description = image.Name;
                PreviewSource = image.ThumbnailSource ?? image.ImageSource;
                ShowsImagePreview = PreviewSource is not null;
                ShowsIconPreview = !ShowsImagePreview;
                break;
            case SecretModel secret:
                TypeLabel = localization.SecretTypeLabel;
                Title = Shorten(secret.Name, TitleLimit);
                Subtitle = localization.SecretRecordLabel;
                Description = SecretModel.MaskedValue;
                ShowsTextPreview = true;
                break;
            default:
                TypeLabel = localization.ItemTypeLabel;
                Title = localization.FavoriteRecordLabel;
                Subtitle = string.Empty;
                Description = string.Empty;
                ShowsIconPreview = true;
                break;
        }

        HasOpenAction = HasOpenAction || source is FileInfoModel or ImageModel;
    }

    public ClipboardItemModel Source { get; }
    public string TypeLabel { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string Description { get; }
    public object? PreviewSource { get; }
    public bool ShowsImagePreview { get; }
    public bool ShowsStaticImagePreview => ShowsImagePreview && !HasImagePreviewAction;
    public bool ShowsTextPreview { get; }
    public bool ShowsIconPreview { get; }
    public bool HasOpenAction { get; }
    public bool HasSaveImageAction => Source is ImageModel image
        && (image.ImageSource is not null || image.ImageData.Length > 0);
    public bool HasImagePreviewAction => Source is ImageModel image
        && (image.ImageSource is not null || image.ImageData.Length > 0);

    private string GetFirstLine(string value)
    {
        return value
            .ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? _localization.TextRecordLabel;
    }

    private static string Shorten(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmedValue = value.Trim();
        return trimmedValue.Length <= maxLength
            ? trimmedValue
            : string.Concat(trimmedValue.AsSpan(0, maxLength - 1), "…");
    }
}
