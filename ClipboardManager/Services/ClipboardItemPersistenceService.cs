using ClipboardManager.Data;
using ClipboardManager.Interfaces;
using ClipboardManager.Models;

namespace ClipboardManager.Services;

public sealed class ClipboardItemPersistenceService(
    IClipboardRepository repository,
    IImageStorageService imageStorageService) : IClipboardItemPersistenceService
{
    public async Task SaveItemsAsync(
        IReadOnlyCollection<FileInfoModel> files,
        IReadOnlyCollection<TextModel> texts,
        IReadOnlyCollection<ImageModel> images,
        IReadOnlyCollection<UrlModel> urls,
        CancellationToken cancellationToken = default)
    {
        await imageStorageService.PrepareForStorageAsync(images, cancellationToken);
        await repository.SaveItemsAsync(files, texts, images, urls, cancellationToken);
    }

    public Task DeleteItemAsync(
        object item,
        CancellationToken cancellationToken = default)
    {
        return item switch
        {
            FileInfoModel file when file.Id != 0 => repository.DeleteFileAsync(file, cancellationToken),
            TextModel text when text.Id != 0 => repository.DeleteTextAsync(text, cancellationToken),
            ImageModel image when image.Id != 0 => repository.DeleteImageAsync(image, cancellationToken),
            UrlModel url when url.Id != 0 => repository.DeleteUrlAsync(url, cancellationToken),
            SecretModel secret when secret.Id != 0 => repository.DeleteSecretAsync(secret, cancellationToken),
            _ => Task.CompletedTask
        };
    }

    public Task UpdatePinAsync(
        IPinnedClipboardItem item,
        CancellationToken cancellationToken = default)
    {
        return repository.UpdatePinAsync(item, cancellationToken);
    }

    public Task SaveSecretFromTextAsync(
        SecretModel secret,
        TextModel sourceText,
        IReadOnlyCollection<UrlModel> relatedUrls,
        CancellationToken cancellationToken = default)
    {
        return repository.SaveSecretFromTextAsync(secret, sourceText, relatedUrls, cancellationToken);
    }
}
