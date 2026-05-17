using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using ClipboardManager.Data;
using ClipboardManager.Helper;
using ClipboardManager.Models;
using ClipboardManager.Services;

namespace ClipboardManager.ViewModels;

public sealed class MainWindowViewModel : BaseViewModel
{
    private static readonly Regex UrlRegex = new(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled);

    private readonly IClipboardRepository _repository;
    private readonly ILinkMetadataService _linkMetadataService;
    private readonly IShellLauncher _shellLauncher;
    private readonly IClipboardService _clipboardService;
    private readonly ClipboardChangeSuppressor _clipboardChangeSuppressor = new();
    private readonly SemaphoreSlim _clipboardLock = new(1, 1);
    private readonly SemaphoreSlim _persistenceLock = new(1, 1);
    private readonly HashSet<string> _knownImageHashes = [];

    private ClipboardContentSignature? _lastHandledClipboardSignature;
    private bool _isBusy;
    private int _selectedSectionIndex;
    private string _statusText = "Мониторинг буфера обмена активен";
    private DateTime? _lastActivityAt;

    public MainWindowViewModel()
        : this(new ClipboardRepository(), new LinkMetadataService(), new ShellLauncher(), new WpfClipboardService())
    {
    }

    public MainWindowViewModel(
        IClipboardRepository repository,
        ILinkMetadataService linkMetadataService,
        IShellLauncher shellLauncher,
        IClipboardService clipboardService)
    {
        _repository = repository;
        _linkMetadataService = linkMetadataService;
        _shellLauncher = shellLauncher;
        _clipboardService = clipboardService;

        ClearCommand = new AsyncRelayCommand(_ => ClearAsync(), _ => HasClipboardItems());
        CopyCommand = new RelayCommand(Copy, CanCopy);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);
        OpenLinkCommand = new RelayCommand(OpenLink, CanOpenLink);
        OpenFileCommand = new RelayCommand(OpenFile, CanOpenFile);
        SelectSectionCommand = new RelayCommand(SelectSection);

        Files.CollectionChanged += ClipboardCollectionChanged;
        Texts.CollectionChanged += ClipboardCollectionChanged;
        Urls.CollectionChanged += ClipboardCollectionChanged;
        Images.CollectionChanged += ClipboardCollectionChanged;
    }

    public ObservableCollection<FileInfoModel> Files { get; } = [];
    public ObservableCollection<TextModel> Texts { get; } = [];
    public ObservableCollection<UrlModel> Urls { get; } = [];
    public ObservableCollection<ImageModel> Images { get; } = [];

    public int SelectedSectionIndex
    {
        get => _selectedSectionIndex;
        set
        {
            if (_selectedSectionIndex == value)
            {
                return;
            }

            _selectedSectionIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsFilesSectionSelected));
            OnPropertyChanged(nameof(IsTextsSectionSelected));
            OnPropertyChanged(nameof(IsUrlsSectionSelected));
            OnPropertyChanged(nameof(IsImagesSectionSelected));
        }
    }

    public bool IsFilesSectionSelected => SelectedSectionIndex == 0;
    public bool IsTextsSectionSelected => SelectedSectionIndex == 1;
    public bool IsUrlsSectionSelected => SelectedSectionIndex == 2;
    public bool IsImagesSectionSelected => SelectedSectionIndex == 3;

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public DateTime? LastActivityAt
    {
        get => _lastActivityAt;
        private set
        {
            if (_lastActivityAt == value)
            {
                return;
            }

            _lastActivityAt = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LastActivityText));
        }
    }

    public string LastActivityText => LastActivityAt is null
        ? "Пока нет новых записей"
        : $"Последнее обновление: {LastActivityAt:HH:mm:ss}";

    public int FileCount => Files.Count;
    public int TextCount => Texts.Count;
    public int UrlCount => Urls.Count;
    public int ImageCount => Images.Count;
    public int TotalCount => FileCount + TextCount + UrlCount + ImageCount;

    public bool HasFiles => FileCount > 0;
    public bool HasTexts => TextCount > 0;
    public bool HasUrls => UrlCount > 0;
    public bool HasImages => ImageCount > 0;
    public bool HasItems => TotalCount > 0;

    public bool IsFilesEmpty => !HasFiles;
    public bool IsTextsEmpty => !HasTexts;
    public bool IsUrlsEmpty => !HasUrls;
    public bool IsImagesEmpty => !HasImages;
    public bool IsHistoryEmpty => !HasItems;

    public ICommand ClearCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand OpenLinkCommand { get; }
    public ICommand OpenFileCommand { get; }
    public ICommand SelectSectionCommand { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await RunBusyAsync(async () =>
        {
            var data = await _repository.LoadAsync(cancellationToken);

            Files.ReplaceWith(data.Files);
            Texts.ReplaceWith(data.Texts);
            Urls.ReplaceWith(data.Urls);
            _knownImageHashes.Clear();

            foreach (var image in data.Images)
            {
                image.ImageSource = BitmapSourceExtensions.ByteArrayToBitmapSource(image.ImageData);
                AddKnownImageHash(image);
            }

            Images.ReplaceWith(data.Images);
            StatusText = HasItems
                ? $"История загружена: {TotalCount} элементов"
                : "История пуста, можно копировать новые данные";
            CommandManager.InvalidateRequerySuggested();
        });
    }

    public async Task HandleClipboardChangedAsync(CancellationToken cancellationToken = default)
    {
        await _clipboardLock.WaitAsync(cancellationToken);

        try
        {
            var signature = _clipboardService.GetCurrentSignature();
            if (signature is null)
            {
                return;
            }

            if (_clipboardChangeSuppressor.ShouldSuppress(signature))
            {
                _lastHandledClipboardSignature = signature;
                return;
            }

            if (signature == _lastHandledClipboardSignature)
            {
                return;
            }

            _lastHandledClipboardSignature = signature;

            var addedItems = new ClipboardItemsBatch();

            switch (signature.Kind)
            {
                case ClipboardContentKind.FileDropList:
                    ProcessFileDropList(addedItems);
                    break;
                case ClipboardContentKind.Text:
                    await ProcessTextAsync(addedItems, cancellationToken);
                    break;
                case ClipboardContentKind.Image:
                    ProcessImage(signature, addedItems);
                    break;
            }

            if (addedItems.HasItems)
            {
                LastActivityAt = DateTime.Now;
                StatusText = $"Добавлено новых элементов: {addedItems.TotalCount}";
                CommandManager.InvalidateRequerySuggested();

                await PersistAddedItemsAsync(addedItems, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            ShowError("Clipboard processing failed", ex);
        }
        finally
        {
            _clipboardLock.Release();
        }
    }

    public async Task FlushPendingSavesAsync(CancellationToken cancellationToken = default)
    {
        await _clipboardLock.WaitAsync(cancellationToken);

        try
        {
            await PersistUnsavedItemsAsync(cancellationToken);
        }
        finally
        {
            _clipboardLock.Release();
        }
    }

    private void ProcessFileDropList(ClipboardItemsBatch addedItems)
    {
        if (!_clipboardService.ContainsFileDropList())
        {
            return;
        }

        foreach (var file in _clipboardService.GetFileDropList())
        {
            if (!File.Exists(file))
            {
                continue;
            }

            var filePath = Path.GetFullPath(file);
            if (Files.Any(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var fileInfo = new FileInfoModel
            {
                FilePath = filePath,
                Name = Path.GetFileName(filePath)
            };

            Files.Add(fileInfo);
            addedItems.Files.Add(fileInfo);
        }
    }

    private async Task ProcessTextAsync(ClipboardItemsBatch addedItems, CancellationToken cancellationToken)
    {
        if (!_clipboardService.ContainsText())
        {
            return;
        }

        var text = _clipboardService.GetText();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (Texts.Any(x => x.Text == text) || Urls.Any(x => x.Url == text || x.Title == text))
        {
            return;
        }

        var textInfo = new TextModel { Text = text };
        Texts.Add(textInfo);
        addedItems.Texts.Add(textInfo);

        var urls = UrlRegex.Matches(text)
            .Select(match => match.Value.TrimEnd('.', ',', ';', ')', ']'))
            .Where(url => !Urls.Any(x => string.Equals(x.Url, url, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var metadataItems = await Task.WhenAll(
            urls.Select(url => _linkMetadataService.GetMetadataAsync(url, cancellationToken)));

        foreach (var metadata in metadataItems)
        {
            if (metadata is not null && !Urls.Any(x => string.Equals(x.Url, metadata.Url, StringComparison.OrdinalIgnoreCase)))
            {
                Urls.Add(metadata);
                addedItems.Urls.Add(metadata);
            }
        }
    }

    private void ProcessImage(ClipboardContentSignature currentSignature, ClipboardItemsBatch addedItems)
    {
        if (!_clipboardService.ContainsImage())
        {
            return;
        }

        var image = _clipboardService.GetImage();
        if (image is null)
        {
            return;
        }

        var imageSignature = currentSignature.Kind == ClipboardContentKind.Image
            ? currentSignature
            : _clipboardService.CreateImageSignature(image);

        if (!_knownImageHashes.Add(imageSignature.Value))
        {
            return;
        }

        var imageInfo = new ImageModel
        {
            ImageSource = image,
            Name = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture)
        };

        Images.Add(imageInfo);
        addedItems.Images.Add(imageInfo);
    }

    private async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await RunBusyAsync(async () =>
        {
            await _clipboardLock.WaitAsync(cancellationToken);

            try
            {
                await _persistenceLock.WaitAsync(cancellationToken);

                try
                {
                    await _repository.ClearAsync(cancellationToken);
                    Files.Clear();
                    Texts.Clear();
                    Images.Clear();
                    Urls.Clear();
                    _knownImageHashes.Clear();
                    _lastHandledClipboardSignature = null;
                    _clipboardChangeSuppressor.Clear();
                    LastActivityAt = null;
                    StatusText = "История очищена";
                    CommandManager.InvalidateRequerySuggested();
                }
                finally
                {
                    _persistenceLock.Release();
                }
            }
            finally
            {
                _clipboardLock.Release();
            }
        });
    }

    private async Task DeleteAsync(object? parameter)
    {
        try
        {
            await _clipboardLock.WaitAsync();

            try
            {
                await _persistenceLock.WaitAsync();

                try
                {
                    switch (parameter)
                    {
                        case FileInfoModel file:
                            if (file.Id != 0)
                            {
                                await _repository.DeleteFileAsync(file);
                            }
                            Files.Remove(file);
                            break;
                        case TextModel text:
                            if (text.Id != 0)
                            {
                                await _repository.DeleteTextAsync(text);
                            }
                            Texts.Remove(text);
                            break;
                        case ImageModel image:
                            var imageHash = TryCreateImageHash(image);
                            if (image.Id != 0)
                            {
                                await _repository.DeleteImageAsync(image);
                            }
                            Images.Remove(image);
                            if (imageHash is not null)
                            {
                                _knownImageHashes.Remove(imageHash);
                            }
                            break;
                        case UrlModel url:
                            if (url.Id != 0)
                            {
                                await _repository.DeleteUrlAsync(url);
                            }
                            Urls.Remove(url);
                            break;
                    }
                }
                finally
                {
                    _persistenceLock.Release();
                }
            }
            finally
            {
                _clipboardLock.Release();
            }

            StatusText = "Элемент удален";
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex)
        {
            ShowError("Delete failed", ex);
        }
    }

    private void Copy(object? parameter)
    {
        try
        {
            var signature = parameter switch
            {
                FileInfoModel file when File.Exists(file.FilePath) =>
                    _clipboardService.SetFileDropList([file.FilePath]),
                TextModel text when !string.IsNullOrEmpty(text.Text) =>
                    _clipboardService.SetText(text.Text),
                ImageModel { ImageSource: not null } image =>
                    _clipboardService.SetImage(image.ImageSource),
                UrlModel url when !string.IsNullOrWhiteSpace(url.Url) =>
                    _clipboardService.SetText(url.Url),
                _ => null
            };

            if (signature is not null)
            {
                _clipboardChangeSuppressor.Suppress(signature);
                _lastHandledClipboardSignature = signature;
                StatusText = "Скопировано в буфер обмена";
            }
        }
        catch (Exception ex)
        {
            ShowError("Copy failed", ex);
        }
    }

    private void OpenLink(object? parameter)
    {
        if (parameter is UrlModel url)
        {
            TryLaunch(() => _shellLauncher.OpenUrl(url.Url));
            StatusText = "Ссылка открыта";
        }
    }

    private void OpenFile(object? parameter)
    {
        if (parameter is FileInfoModel file)
        {
            TryLaunch(() => _shellLauncher.OpenFile(file.FilePath));
            StatusText = "Файл открыт";
        }
    }

    private void SelectSection(object? parameter)
    {
        if (parameter is int index)
        {
            SelectedSectionIndex = index;
            return;
        }

        if (parameter is string value && int.TryParse(value, CultureInfo.InvariantCulture, out index))
        {
            SelectedSectionIndex = index;
        }
    }

    private async Task PersistAddedItemsAsync(
        ClipboardItemsBatch addedItems,
        CancellationToken cancellationToken)
    {
        if (!addedItems.HasItems)
        {
            return;
        }

        await _persistenceLock.WaitAsync(cancellationToken);

        try
        {
            await PrepareImagesForStorageAsync(addedItems.Images, cancellationToken);

            await _repository.SaveItemsAsync(
                addedItems.Files,
                addedItems.Texts,
                addedItems.Images,
                addedItems.Urls,
                cancellationToken);

            StatusText = $"Автосохранено элементов: {addedItems.TotalCount}";
        }
        catch (Exception ex)
        {
            ShowError("Auto-save failed", ex);
        }
        finally
        {
            _persistenceLock.Release();
        }
    }

    private async Task PersistUnsavedItemsAsync(CancellationToken cancellationToken)
    {
        var unsavedItems = new ClipboardItemsBatch(
            Files.Where(x => x.Id == 0).ToArray(),
            Texts.Where(x => x.Id == 0).ToArray(),
            Images.Where(x => x.Id == 0).ToArray(),
            Urls.Where(x => x.Id == 0).ToArray());

        if (!unsavedItems.HasItems)
        {
            return;
        }

        await PersistAddedItemsAsync(unsavedItems, cancellationToken);
    }

    private static async Task PrepareImagesForStorageAsync(
        IReadOnlyCollection<ImageModel> images,
        CancellationToken cancellationToken)
    {
        var pendingImages = images
            .Where(x => x.Id == 0 && x.ImageSource is not null && x.ImageData.Length == 0)
            .Select(x => (Image: x, Source: x.ImageSource!))
            .ToArray();

        if (pendingImages.Length == 0)
        {
            return;
        }

        foreach (var (_, source) in pendingImages)
        {
            if (source.CanFreeze && !source.IsFrozen)
            {
                source.Freeze();
            }
        }

        var backgroundImages = pendingImages
            .Where(x => x.Source.IsFrozen)
            .ToArray();
        var uiThreadImages = pendingImages
            .Where(x => !x.Source.IsFrozen)
            .ToArray();

        foreach (var (image, source) in uiThreadImages)
        {
            image.ImageData = BitmapSourceExtensions.ConvertBitmapSourceToByteArray(source, ".png");
        }

        if (backgroundImages.Length == 0)
        {
            return;
        }

        var encodedImages = await Task.Run(
            () =>
            {
                return backgroundImages
                    .Select(item =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        return (
                            item.Image,
                            Data: BitmapSourceExtensions.ConvertBitmapSourceToByteArray(item.Source, ".png"));
                    })
                    .ToArray();
            },
            cancellationToken);

        foreach (var (image, imageData) in encodedImages)
        {
            image.ImageData = imageData;
        }
    }

    private bool CanCopy(object? parameter)
    {
        return parameter switch
        {
            FileInfoModel file => File.Exists(file.FilePath),
            TextModel text => !string.IsNullOrEmpty(text.Text),
            ImageModel { ImageSource: not null } => true,
            UrlModel url => !string.IsNullOrWhiteSpace(url.Url),
            _ => false
        };
    }

    private static bool CanDelete(object? parameter)
    {
        return parameter is FileInfoModel or TextModel or ImageModel or UrlModel;
    }

    private static bool CanOpenLink(object? parameter)
    {
        return parameter is UrlModel url
            && Uri.TryCreate(url.Url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool CanOpenFile(object? parameter)
    {
        return parameter is FileInfoModel file && File.Exists(file.FilePath);
    }

    private bool HasClipboardItems()
    {
        return HasItems;
    }

    private void ClipboardCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RaiseClipboardStateChanged();
        CommandManager.InvalidateRequerySuggested();
    }

    private void RaiseClipboardStateChanged()
    {
        OnPropertyChanged(nameof(FileCount));
        OnPropertyChanged(nameof(TextCount));
        OnPropertyChanged(nameof(UrlCount));
        OnPropertyChanged(nameof(ImageCount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(HasTexts));
        OnPropertyChanged(nameof(HasUrls));
        OnPropertyChanged(nameof(HasImages));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsFilesEmpty));
        OnPropertyChanged(nameof(IsTextsEmpty));
        OnPropertyChanged(nameof(IsUrlsEmpty));
        OnPropertyChanged(nameof(IsImagesEmpty));
        OnPropertyChanged(nameof(IsHistoryEmpty));
    }

    private void AddKnownImageHash(ImageModel image)
    {
        var imageHash = TryCreateImageHash(image);
        if (imageHash is not null)
        {
            _knownImageHashes.Add(imageHash);
        }
    }

    private string? TryCreateImageHash(ImageModel image)
    {
        return image.ImageSource is null
            ? null
            : _clipboardService.CreateImageSignature(image.ImageSource).Value;
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            ShowError("Operation failed", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static void TryLaunch(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            ShowError("Open failed", ex);
        }
    }

    private static void ShowError(string title, Exception exception)
    {
        MessageBox.Show(exception.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

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

internal static class ObservableCollectionExtensions
{
    public static void ReplaceWith<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();

        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
