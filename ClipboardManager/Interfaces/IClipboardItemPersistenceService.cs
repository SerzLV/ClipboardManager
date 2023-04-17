using ClipboardManager.Models;

namespace ClipboardManager.Interfaces;

public interface IClipboardItemPersistenceService
{
    Task SaveItemsAsync(
        IReadOnlyCollection<FileInfoModel> files,
        IReadOnlyCollection<TextModel> texts,
        IReadOnlyCollection<ImageModel> images,
        IReadOnlyCollection<UrlModel> urls,
        CancellationToken cancellationToken = default);

    Task DeleteItemAsync(
        object item,
        CancellationToken cancellationToken = default);

    Task UpdatePinAsync(
        IPinnedClipboardItem item,
        CancellationToken cancellationToken = default);

    Task SaveSecretFromTextAsync(
        SecretModel secret,
        TextModel sourceText,
        IReadOnlyCollection<UrlModel> relatedUrls,
        CancellationToken cancellationToken = default);
}
