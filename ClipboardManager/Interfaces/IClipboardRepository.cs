using ClipboardManager.Data;
using ClipboardManager.Models;

namespace ClipboardManager.Interfaces;

public interface IClipboardRepository
{
    Task<ClipboardData> LoadAsync(CancellationToken cancellationToken = default);
    Task<ClipboardCounts> CountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FileInfoModel>> LoadFilesPageAsync(
        int skip,
        int take,
        bool includePinned,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TextModel>> LoadTextsPageAsync(
        int skip,
        int take,
        bool includePinned,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImageModel>> LoadImagesPageAsync(
        int skip,
        int take,
        bool includePinned,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UrlModel>> LoadUrlsPageAsync(
        int skip,
        int take,
        bool includePinned,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UrlModel>> LoadStaleUrlsAsync(
        DateTime staleBeforeUtc,
        int take,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecretModel>> LoadSecretsPageAsync(
        int skip,
        int take,
        bool includePinned,
        CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> TextExistsAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> FindExistingUrlsAsync(
        IReadOnlyCollection<string> urls,
        CancellationToken cancellationToken = default);
    Task SaveItemsAsync(
        IReadOnlyCollection<FileInfoModel> files,
        IReadOnlyCollection<TextModel> texts,
        IReadOnlyCollection<ImageModel> images,
        IReadOnlyCollection<UrlModel> urls,
        CancellationToken cancellationToken = default);
    Task DeleteFileAsync(FileInfoModel file, CancellationToken cancellationToken = default);
    Task DeleteTextAsync(TextModel text, CancellationToken cancellationToken = default);
    Task DeleteImageAsync(ImageModel image, CancellationToken cancellationToken = default);
    Task DeleteUrlAsync(UrlModel url, CancellationToken cancellationToken = default);
    Task UpdateUrlMetadataAsync(UrlModel url, CancellationToken cancellationToken = default);
    Task SaveSecretFromTextAsync(
        SecretModel secret,
        TextModel sourceText,
        IReadOnlyCollection<UrlModel> relatedUrls,
        CancellationToken cancellationToken = default);
    Task DeleteSecretAsync(SecretModel secret, CancellationToken cancellationToken = default);
    Task UpdatePinAsync(IPinnedClipboardItem item, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
