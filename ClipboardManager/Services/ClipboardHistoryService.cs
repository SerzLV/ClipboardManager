using ClipboardManager.Data;
using ClipboardManager.Interfaces;
using ClipboardManager.Models;

namespace ClipboardManager.Services;

public sealed class ClipboardHistoryService(IClipboardRepository repository) : IClipboardHistoryService
{
    public Task<ClipboardData> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        return repository.LoadAsync(cancellationToken);
    }

    public async Task<ClipboardHistorySnapshot> LoadInitialPageAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var counts = await repository.CountAsync(cancellationToken);
        var data = new ClipboardData(
            await repository.LoadFilesPageAsync(0, batchSize, true, cancellationToken),
            await repository.LoadTextsPageAsync(0, batchSize, true, cancellationToken),
            await repository.LoadImagesPageAsync(0, batchSize, true, cancellationToken),
            await repository.LoadUrlsPageAsync(0, batchSize, true, cancellationToken),
            await repository.LoadSecretsPageAsync(0, batchSize, true, cancellationToken));

        return new ClipboardHistorySnapshot(counts, data);
    }

    public Task<IReadOnlyList<FileInfoModel>> LoadFilesPageAsync(
        int offset,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return repository.LoadFilesPageAsync(offset, batchSize, false, cancellationToken);
    }

    public Task<IReadOnlyList<TextModel>> LoadTextsPageAsync(
        int offset,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return repository.LoadTextsPageAsync(offset, batchSize, false, cancellationToken);
    }

    public Task<IReadOnlyList<UrlModel>> LoadUrlsPageAsync(
        int offset,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return repository.LoadUrlsPageAsync(offset, batchSize, false, cancellationToken);
    }

    public Task<IReadOnlyList<ImageModel>> LoadImagesPageAsync(
        int offset,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return repository.LoadImagesPageAsync(offset, batchSize, false, cancellationToken);
    }

    public Task<IReadOnlyList<SecretModel>> LoadSecretsPageAsync(
        int offset,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return repository.LoadSecretsPageAsync(offset, batchSize, false, cancellationToken);
    }

    public Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        return repository.ClearAsync(cancellationToken);
    }
}
