using ClipboardManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClipboardManager.Data;

public interface IClipboardRepository
{
    Task<ClipboardData> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveNewItemsAsync(
        IEnumerable<FileInfoModel> files,
        IEnumerable<TextModel> texts,
        IEnumerable<ImageModel> images,
        IEnumerable<UrlModel> urls,
        CancellationToken cancellationToken = default);
    Task DeleteFileAsync(FileInfoModel file, CancellationToken cancellationToken = default);
    Task DeleteTextAsync(TextModel text, CancellationToken cancellationToken = default);
    Task DeleteImageAsync(ImageModel image, CancellationToken cancellationToken = default);
    Task DeleteUrlAsync(UrlModel url, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

public sealed record ClipboardData(
    IReadOnlyList<FileInfoModel> Files,
    IReadOnlyList<TextModel> Texts,
    IReadOnlyList<ImageModel> Images,
    IReadOnlyList<UrlModel> Urls);

public sealed class ClipboardRepository : IClipboardRepository
{
    public async Task<ClipboardData> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);

        var files = await context.Files.AsNoTracking().ToListAsync(cancellationToken);
        var texts = await context.Texts.AsNoTracking().ToListAsync(cancellationToken);
        var images = await context.Images.AsNoTracking().ToListAsync(cancellationToken);
        var urls = await context.Urls.AsNoTracking().ToListAsync(cancellationToken);

        return new ClipboardData(files, texts, images, urls);
    }

    public async Task SaveNewItemsAsync(
        IEnumerable<FileInfoModel> files,
        IEnumerable<TextModel> texts,
        IEnumerable<ImageModel> images,
        IEnumerable<UrlModel> urls,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);

        context.Files.AddRange(files.Where(x => x.Id == 0));
        context.Texts.AddRange(texts.Where(x => x.Id == 0));
        context.Images.AddRange(images.Where(x => x.Id == 0));
        context.Urls.AddRange(urls.Where(x => x.Id == 0));

        await context.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteFileAsync(FileInfoModel file, CancellationToken cancellationToken = default)
    {
        return DeleteAsync(file, cancellationToken);
    }

    public Task DeleteTextAsync(TextModel text, CancellationToken cancellationToken = default)
    {
        return DeleteAsync(text, cancellationToken);
    }

    public Task DeleteImageAsync(ImageModel image, CancellationToken cancellationToken = default)
    {
        return DeleteAsync(image, cancellationToken);
    }

    public Task DeleteUrlAsync(UrlModel url, CancellationToken cancellationToken = default)
    {
        return DeleteAsync(url, cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);

        await context.Files.ExecuteDeleteAsync(cancellationToken);
        await context.Texts.ExecuteDeleteAsync(cancellationToken);
        await context.Images.ExecuteDeleteAsync(cancellationToken);
        await context.Urls.ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task DeleteAsync<T>(T item, CancellationToken cancellationToken)
        where T : class
    {
        await using var context = await CreateContextAsync(cancellationToken);
        context.Set<T>().Remove(item);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<ClipboardDbContext> CreateContextAsync(CancellationToken cancellationToken)
    {
        var context = new ClipboardDbContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);
        return context;
    }
}
