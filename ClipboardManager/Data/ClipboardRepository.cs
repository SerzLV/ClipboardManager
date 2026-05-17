using ClipboardManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClipboardManager.Data;

public interface IClipboardRepository
{
    Task<ClipboardData> LoadAsync(CancellationToken cancellationToken = default);
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
    Task UpdatePinAsync(IPinnedClipboardItem item, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

public sealed record ClipboardData(
    IReadOnlyList<FileInfoModel> Files,
    IReadOnlyList<TextModel> Texts,
    IReadOnlyList<ImageModel> Images,
    IReadOnlyList<UrlModel> Urls);

public sealed class ClipboardRepository : IClipboardRepository
{
    private static readonly SemaphoreSlim DatabaseInitializationLock = new(1, 1);
    private static bool _databaseInitialized;

    public async Task<ClipboardData> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);

        var files = await context.Files.AsNoTracking().ToListAsync(cancellationToken);
        var texts = await context.Texts.AsNoTracking().ToListAsync(cancellationToken);
        var images = await context.Images.AsNoTracking().ToListAsync(cancellationToken);
        var urls = await context.Urls.AsNoTracking().ToListAsync(cancellationToken);

        return new ClipboardData(files, texts, images, urls);
    }

    public async Task SaveItemsAsync(
        IReadOnlyCollection<FileInfoModel> files,
        IReadOnlyCollection<TextModel> texts,
        IReadOnlyCollection<ImageModel> images,
        IReadOnlyCollection<UrlModel> urls,
        CancellationToken cancellationToken = default)
    {
        var newFiles = files.Where(x => x.Id == 0).ToArray();
        var newTexts = texts.Where(x => x.Id == 0).ToArray();
        var newImages = images.Where(x => x.Id == 0).ToArray();
        var newUrls = urls.Where(x => x.Id == 0).ToArray();

        if (newFiles.Length == 0
            && newTexts.Length == 0
            && newImages.Length == 0
            && newUrls.Length == 0)
        {
            return;
        }

        await using var context = await CreateContextAsync(cancellationToken);

        context.ChangeTracker.AutoDetectChangesEnabled = false;
        context.Files.AddRange(newFiles);
        context.Texts.AddRange(newTexts);
        context.Images.AddRange(newImages);
        context.Urls.AddRange(newUrls);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteFileAsync(FileInfoModel file, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);
        await context.Files
            .Where(x => x.Id == file.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteTextAsync(TextModel text, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);
        await context.Texts
            .Where(x => x.Id == text.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteImageAsync(ImageModel image, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);
        await context.Images
            .Where(x => x.Id == image.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteUrlAsync(UrlModel url, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);
        await context.Urls
            .Where(x => x.Id == url.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task UpdatePinAsync(
        IPinnedClipboardItem item,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);

        switch (item)
        {
            case FileInfoModel file when file.Id != 0:
                await context.Files
                    .Where(x => x.Id == file.Id)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(x => x.IsPinned, file.IsPinned),
                        cancellationToken);
                break;
            case TextModel text when text.Id != 0:
                await context.Texts
                    .Where(x => x.Id == text.Id)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(x => x.IsPinned, text.IsPinned),
                        cancellationToken);
                break;
            case ImageModel image when image.Id != 0:
                await context.Images
                    .Where(x => x.Id == image.Id)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(x => x.IsPinned, image.IsPinned),
                        cancellationToken);
                break;
            case UrlModel url when url.Id != 0:
                await context.Urls
                    .Where(x => x.Id == url.Id)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(x => x.IsPinned, url.IsPinned),
                        cancellationToken);
                break;
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.Files.ExecuteDeleteAsync(cancellationToken);
        await context.Texts.ExecuteDeleteAsync(cancellationToken);
        await context.Images.ExecuteDeleteAsync(cancellationToken);
        await context.Urls.ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<ClipboardDbContext> CreateContextAsync(CancellationToken cancellationToken)
    {
        var context = new ClipboardDbContext();

        try
        {
            await EnsureDatabaseCreatedAsync(context, cancellationToken);
            return context;
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }

    private static async Task EnsureDatabaseCreatedAsync(
        ClipboardDbContext context,
        CancellationToken cancellationToken)
    {
        if (_databaseInitialized)
        {
            return;
        }

        await DatabaseInitializationLock.WaitAsync(cancellationToken);

        try
        {
            if (!_databaseInitialized)
            {
                await context.Database.EnsureCreatedAsync(cancellationToken);
                await ConfigureDatabaseAsync(context, cancellationToken);
                _databaseInitialized = true;
            }
        }
        finally
        {
            DatabaseInitializationLock.Release();
        }
    }

    private static async Task ConfigureDatabaseAsync(
        ClipboardDbContext context,
        CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
        await EnsurePinnedColumnAsync(context, "Files", cancellationToken);
        await EnsurePinnedColumnAsync(context, "Texts", cancellationToken);
        await EnsurePinnedColumnAsync(context, "Images", cancellationToken);
        await EnsurePinnedColumnAsync(context, "Urls", cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_Files_FilePath ON Files (FilePath);",
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_Urls_Url ON Urls (Url);",
            cancellationToken);
    }

    private static async Task EnsurePinnedColumnAsync(
        ClipboardDbContext context,
        string tableName,
        CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(context, tableName, "IsPinned", cancellationToken))
        {
            return;
        }

        await context.Database.ExecuteSqlRawAsync(GetAddPinnedColumnSql(tableName), cancellationToken);
    }

    private static async Task<bool> ColumnExistsAsync(
        ClipboardDbContext context,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName});";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static string GetAddPinnedColumnSql(string tableName)
    {
        return tableName switch
        {
            "Files" => "ALTER TABLE Files ADD COLUMN IsPinned INTEGER NOT NULL DEFAULT 0;",
            "Texts" => "ALTER TABLE Texts ADD COLUMN IsPinned INTEGER NOT NULL DEFAULT 0;",
            "Images" => "ALTER TABLE Images ADD COLUMN IsPinned INTEGER NOT NULL DEFAULT 0;",
            "Urls" => "ALTER TABLE Urls ADD COLUMN IsPinned INTEGER NOT NULL DEFAULT 0;",
            _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, "Unsupported table name.")
        };
    }
}
