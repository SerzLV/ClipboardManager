using System.IO;
using System.Globalization;
using System.Text.Json;
using ClipboardManager.Data;
using ClipboardManager.Helper;
using ClipboardManager.Localization;
using ClipboardManager.Models;

namespace ClipboardManager.Services;

public interface IClipboardTransferService
{
    Task ExportAsync(
        ClipboardData data,
        string filePath,
        CancellationToken cancellationToken = default);

    Task<ClipboardData> ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}

public sealed class ClipboardTransferService : IClipboardTransferService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly LocalizationService _localization;

    public ClipboardTransferService(LocalizationService localization)
    {
        _localization = localization;
    }

    public async Task ExportAsync(
        ClipboardData data,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var document = await Task.Run(() => CreateDocument(data), cancellationToken);

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }

    public async Task<ClipboardData> ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await using var stream = File.OpenRead(filePath);
        var document = await JsonSerializer.DeserializeAsync<ClipboardExportDocument>(
            stream,
            JsonOptions,
            cancellationToken);

        if (document is null || document.Version < 1)
        {
            throw new InvalidDataException(_localization.UnsupportedImportFormatMessage);
        }

        return await Task.Run(() => CreateData(document), cancellationToken);
    }

    private static ClipboardExportDocument CreateDocument(ClipboardData data)
    {
        return new ClipboardExportDocument
        {
            Version = 1,
            ExportedAt = DateTimeOffset.Now,
            Files = data.Files
                .Select(file => new ClipboardFileExportItem
                {
                    Name = file.Name,
                    FilePath = file.FilePath,
                    IsPinned = file.IsPinned
                })
                .ToList(),
            Texts = data.Texts
                .Select(text => new ClipboardTextExportItem
                {
                    Text = text.Text,
                    IsPinned = text.IsPinned
                })
                .ToList(),
            Images = data.Images
                .Select(image => new ClipboardImageExportItem
                {
                    Name = image.Name,
                    ImageData = image.ImageData,
                    IsPinned = image.IsPinned
                })
                .ToList(),
            Urls = data.Urls
                .Select(url => new ClipboardUrlExportItem
                {
                    Url = url.Url,
                    Title = url.Title,
                    Description = url.Description,
                    ImageUrl = url.ImageUrl,
                    IsPinned = url.IsPinned
                })
                .ToList()
        };
    }

    private static ClipboardData CreateData(ClipboardExportDocument document)
    {
        var files = document.Files
            .Select(file => new FileInfoModel
            {
                Name = file.Name,
                FilePath = file.FilePath,
                IsPinned = file.IsPinned
            })
            .Where(file => !string.IsNullOrWhiteSpace(file.FilePath))
            .ToArray();

        var texts = document.Texts
            .Select(text => new TextModel
            {
                Text = text.Text,
                IsPinned = text.IsPinned
            })
            .Where(text => !string.IsNullOrWhiteSpace(text.Text))
            .ToArray();

        var images = document.Images
            .Where(image => image.ImageData.Length > 0)
            .Select(image => new ImageModel
            {
                Name = string.IsNullOrWhiteSpace(image.Name)
                    ? DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture)
                    : image.Name,
                ImageData = image.ImageData,
                ImageSource = BitmapSourceExtensions.ByteArrayToBitmapSource(image.ImageData),
                IsPinned = image.IsPinned
            })
            .Where(image => image.ImageSource is not null)
            .ToArray();

        var urls = document.Urls
            .Select(url => new UrlModel
            {
                Url = url.Url,
                Title = url.Title,
                Description = url.Description,
                ImageUrl = url.ImageUrl,
                IsPinned = url.IsPinned
            })
            .Where(url => !string.IsNullOrWhiteSpace(url.Url))
            .ToArray();

        return new ClipboardData(files, texts, images, urls);
    }

    private sealed class ClipboardExportDocument
    {
        public int Version { get; set; }
        public DateTimeOffset ExportedAt { get; set; }
        public List<ClipboardFileExportItem> Files { get; set; } = [];
        public List<ClipboardTextExportItem> Texts { get; set; } = [];
        public List<ClipboardImageExportItem> Images { get; set; } = [];
        public List<ClipboardUrlExportItem> Urls { get; set; } = [];
    }

    private sealed class ClipboardFileExportItem
    {
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
    }

    private sealed class ClipboardTextExportItem
    {
        public string Text { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
    }

    private sealed class ClipboardImageExportItem
    {
        public string Name { get; set; } = string.Empty;
        public byte[] ImageData { get; set; } = [];
        public bool IsPinned { get; set; }
    }

    private sealed class ClipboardUrlExportItem
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
    }
}
