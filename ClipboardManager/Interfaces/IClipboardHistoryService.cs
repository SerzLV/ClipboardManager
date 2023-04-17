using ClipboardManager.Data;
using ClipboardManager.Models;

namespace ClipboardManager.Interfaces;

public interface IClipboardHistoryService
{
    Task<ClipboardData> LoadAllAsync(CancellationToken cancellationToken = default);

    Task<ClipboardHistorySnapshot> LoadInitialPageAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileInfoModel>> LoadFilesPageAsync(
        int offset,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TextModel>> LoadTextsPageAsync(
        int offset,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UrlModel>> LoadUrlsPageAsync(
        int offset,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImageModel>> LoadImagesPageAsync(
        int offset,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SecretModel>> LoadSecretsPageAsync(
        int offset,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task ClearHistoryAsync(CancellationToken cancellationToken = default);
}

public sealed record ClipboardHistorySnapshot(
    ClipboardCounts Counts,
    ClipboardData Data);
