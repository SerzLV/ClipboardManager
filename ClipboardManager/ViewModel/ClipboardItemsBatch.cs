using ClipboardManager.Models;

namespace ClipboardManager.ViewModels;

internal sealed class ClipboardItemsBatch
{
    public ClipboardItemsBatch()
        : this([], [], [], [])
    {
    }

    public ClipboardItemsBatch(
        IReadOnlyCollection<FileInfoModel> files,
        IReadOnlyCollection<TextModel> texts,
        IReadOnlyCollection<ImageModel> images,
        IReadOnlyCollection<UrlModel> urls)
    {
        Files = [.. files];
        Texts = [.. texts];
        Images = [.. images];
        Urls = [.. urls];
    }

    public List<FileInfoModel> Files { get; }
    public List<TextModel> Texts { get; }
    public List<ImageModel> Images { get; }
    public List<UrlModel> Urls { get; }

    public int TotalCount => Files.Count + Texts.Count + Images.Count + Urls.Count;
    public bool HasItems => TotalCount > 0;
}

internal readonly record struct ImportMergeResult(int AddedCount, int PinnedUpdatedCount);
