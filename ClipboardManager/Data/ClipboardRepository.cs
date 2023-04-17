using ClipboardManager.Models;
using ClipboardManager.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClipboardManager.Data;

public sealed record ClipboardData
{
    public ClipboardData(
        IReadOnlyList<FileInfoModel> files,
        IReadOnlyList<TextModel> texts,
        IReadOnlyList<ImageModel> images,
        IReadOnlyList<UrlModel> urls)
        : this(files, texts, images, urls, [])
    {
    }

    public ClipboardData(
        IReadOnlyList<FileInfoModel> files,
        IReadOnlyList<TextModel> texts,
        IReadOnlyList<ImageModel> images,
        IReadOnlyList<UrlModel> urls,
        IReadOnlyList<SecretModel> secrets)
    {
        Files = files;
        Texts = texts;
        Images = images;
        Urls = urls;
        Secrets = secrets;
    }

    public IReadOnlyList<FileInfoModel> Files { get; init; }
    public IReadOnlyList<TextModel> Texts { get; init; }
    public IReadOnlyList<ImageModel> Images { get; init; }
    public IReadOnlyList<UrlModel> Urls { get; init; }
    public IReadOnlyList<SecretModel> Secrets { get; init; }
}

public sealed record ClipboardCounts(
    int Files,
    int Texts,
    int Images,
    int Urls,
    int Secrets)
{
    public int HistoryTotal => Files + Texts + Images + Urls;
    public int Total => HistoryTotal + Secrets;
}

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
        var secrets = await context.Secrets.AsNoTracking().ToListAsync(cancellationToken);

        return new ClipboardData(files, texts, images, urls, secrets);
    }

    public async Task<ClipboardCounts> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);

        return new ClipboardCounts(
            await context.Files.CountAsync(cancellationToken),
            await context.Texts.CountAsync(cancellationToken),
            await context.Images.CountAsync(cancellationToken),
            await context.Urls.CountAsync(cancellationToken),
            await context.Secrets.CountAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<FileInfoModel>> LoadFilesPageAsync(
        int skip,
        int take,
        bool includePinned,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);
        return await LoadPageAsync(
            context.Files.AsNoTracking(),
            skip,
            take,
            includePinned,
            file => file.Id,
            cancellationToken);
    }

    public async Task<IReadOnlyList<TextModel>> LoadTextsPageAsync(
        int skip,
        int take,
        bool includePinned,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);
        return await LoadPageAsync(
            context.Texts.AsNoTracking(),
            skip,
            take,
            includePinned,
            text => text.Id,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ImageModel>> LoadImagesPageAsync(
        int skip,
        int take,
        bool includePinned,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);
        return await LoadPageAsync(
            context.Images.AsNoTracking(),
            skip,
            take,
            includePinned,
            image => image.Id,
            cancellationToken);
    }

    public async Task<IReadOnlyList<UrlModel>> LoadUrlsPageAsync(
        int skip,
        int take,
        bool includePinned,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);
        return await LoadPageAsync(
            context.Urls.AsNoTracking(),
            skip,
            take,
            includePinned,
            url => url.Id,
            cancellationToken);
    }

    public async Task<IReadOnlyList<UrlModel>> LoadStaleUrlsAsync(
        DateTime staleBeforeUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        take = Math.Max(0, take);
        if (take == 0)
        {
            return [];
        }

        await using var context = await CreateContextAsync(cancellationToken);
        return await context.Urls
            .AsNoTracking()
            .Where(url => url.MetadataUpdatedAt == null || url.MetadataUpdatedAt < staleBeforeUtc)
            .OrderBy(url => url.MetadataUpdatedAt ?? DateTime.MinValue)
            .ThenByDescending(url => url.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecretModel>> LoadSecretsPageAsync(
        int skip,
        int take,
        bool includePinned,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);
        return await LoadPageAsync(
            context.Secrets.AsNoTracking(),
            skip,
            take,
            includePinned,
            secret => secret.Id,
            cancellationToken);
    }

    public async Task<bool> FileExistsAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);
        return await context.Files
            .AsNoTracking()
            .AnyAsync(file => file.FilePath == filePath, cancellationToken);
    }

    public async Task<bool> TextExistsAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);
        return await context.Texts
            .AsNoTracking()
            .AnyAsync(item => item.Text == text, cancellationToken);
    }

    public async Task<IReadOnlySet<string>> FindExistingUrlsAsync(
        IReadOnlyCollection<string> urls,
        CancellationToken cancellationToken = default)
    {
        if (urls.Count == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var candidates = urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (candidates.Length == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        await using var context = await CreateContextAsync(cancellationToken);
        var existingUrls = await context.Urls
            .AsNoTracking()
            .Where(url => candidates.Contains(url.Url))
            .Select(url => url.Url)
            .ToArrayAsync(cancellationToken);

        return existingUrls.ToHashSet(StringComparer.OrdinalIgnoreCase);
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

    public async Task UpdateUrlMetadataAsync(
        UrlModel url,
        CancellationToken cancellationToken = default)
    {
        if (url.Id == 0)
        {
            return;
        }

        await using var context = await CreateContextAsync(cancellationToken);
        await context.Urls
            .Where(x => x.Id == url.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Title, url.Title)
                    .SetProperty(x => x.Description, url.Description)
                    .SetProperty(x => x.ImageUrl, url.ImageUrl)
                    .SetProperty(x => x.MetadataUpdatedAt, url.MetadataUpdatedAt),
                cancellationToken);
    }

    public async Task SaveSecretFromTextAsync(
        SecretModel secret,
        TextModel sourceText,
        IReadOnlyCollection<UrlModel> relatedUrls,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        context.Secrets.Add(secret);

        if (sourceText.Id != 0)
        {
            context.Texts.Attach(sourceText);
            context.Texts.Remove(sourceText);
        }

        var relatedUrlIds = relatedUrls
            .Select(url => url.Id)
            .Where(id => id != 0)
            .ToArray();

        if (relatedUrlIds.Length > 0)
        {
            await context.Urls
                .Where(url => relatedUrlIds.Contains(url.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteSecretAsync(SecretModel secret, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(cancellationToken);
        await context.Secrets
            .Where(x => x.Id == secret.Id)
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
            case SecretModel secret when secret.Id != 0:
                await context.Secrets
                    .Where(x => x.Id == secret.Id)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(x => x.IsPinned, secret.IsPinned),
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
        await EnsureUrlMetadataUpdatedAtColumnAsync(context, cancellationToken);
        await EnsureSecretsTableAsync(context, cancellationToken);
        await EnsurePinnedColumnAsync(context, "Secrets", cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_Files_FilePath ON Files (FilePath);",
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_Urls_Url ON Urls (Url);",
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_Urls_MetadataUpdatedAt ON Urls (MetadataUpdatedAt);",
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_Secrets_Name ON Secrets (Name);",
            cancellationToken);
    }

    private static async Task EnsureSecretsTableAsync(
        ClipboardDbContext context,
        CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS Secrets (
                Id INTEGER NOT NULL CONSTRAINT PK_Secrets PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                ProtectedValue BLOB NOT NULL,
                CreatedAt TEXT NOT NULL,
                IsPinned INTEGER NOT NULL DEFAULT 0
            );
            """,
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

    private static async Task EnsureUrlMetadataUpdatedAtColumnAsync(
        ClipboardDbContext context,
        CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(context, "Urls", "MetadataUpdatedAt", cancellationToken))
        {
            return;
        }

        await context.Database.ExecuteSqlRawAsync(
            "ALTER TABLE Urls ADD COLUMN MetadataUpdatedAt TEXT NULL;",
            cancellationToken);
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
            "Secrets" => "ALTER TABLE Secrets ADD COLUMN IsPinned INTEGER NOT NULL DEFAULT 0;",
            _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, "Unsupported table name.")
        };
    }

    private static async Task<List<T>> LoadPageAsync<T>(
        IQueryable<T> source,
        int skip,
        int take,
        bool includePinned,
        Func<T, int> getId,
        CancellationToken cancellationToken)
        where T : ClipboardItemModel
    {
        skip = Math.Max(0, skip);
        take = Math.Max(0, take);

        if (take == 0)
        {
            return [];
        }

        var page = await source
            .OrderByDescending(item => EF.Property<int>(item, "Id"))
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        if (!includePinned)
        {
            return page;
        }

        var pinned = await source
            .Where(item => item.IsPinned)
            .OrderByDescending(item => EF.Property<int>(item, "Id"))
            .ToListAsync(cancellationToken);

        if (pinned.Count == 0)
        {
            return page;
        }

        var knownIds = new HashSet<int>();
        var mergedItems = new List<T>(page.Count + pinned.Count);
        foreach (var item in page.Concat(pinned))
        {
            if (knownIds.Add(getId(item)))
            {
                mergedItems.Add(item);
            }
        }

        return mergedItems;
    }
}
