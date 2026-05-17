using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Data;
using ClipboardManager.Data;
using ClipboardManager.Helper;
using ClipboardManager.Models;
using ClipboardManager.Services;

namespace ClipboardManager.ViewModels;

public sealed class MainWindowViewModel : BaseViewModel
{
    private const double MinImagePreviewZoom = 0.25;
    private const double MaxImagePreviewZoom = 4.0;
    private const double ImagePreviewZoomStep = 0.25;
    private static readonly Regex UrlRegex = new(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled);

    private readonly IClipboardRepository _repository;
    private readonly ILinkMetadataService _linkMetadataService;
    private readonly IShellLauncher _shellLauncher;
    private readonly IClipboardService _clipboardService;
    private readonly IUserNotificationService _notificationService;
    private readonly IClipboardTransferService _transferService;
    private readonly IClipboardTransferDialogService _transferDialogService;
    private readonly ClipboardChangeSuppressor _clipboardChangeSuppressor = new();
    private readonly SemaphoreSlim _clipboardLock = new(1, 1);
    private readonly SemaphoreSlim _persistenceLock = new(1, 1);
    private readonly HashSet<string> _knownImageHashes = [];

    private ClipboardContentSignature? _lastHandledClipboardSignature;
    private bool _isBusy;
    private int _selectedSectionIndex;
    private string _statusText = "Мониторинг буфера обмена активен";
    private string _searchText = string.Empty;
    private ImageModel? _previewImage;
    private object? _previewImageSource;
    private string _previewImageName = string.Empty;
    private double _previewZoom = 1.0;
    private bool _isImagePreviewOpen;
    private DateTime? _lastActivityAt;
    private int _filteredFileCount;
    private int _filteredTextCount;
    private int _filteredUrlCount;
    private int _filteredImageCount;
    private int _filteredFavoriteCount;

    public MainWindowViewModel(
        IClipboardRepository repository,
        ILinkMetadataService linkMetadataService,
        IShellLauncher shellLauncher,
        IClipboardService clipboardService,
        IUserNotificationService notificationService,
        IClipboardTransferService transferService,
        IClipboardTransferDialogService transferDialogService)
    {
        _repository = repository;
        _linkMetadataService = linkMetadataService;
        _shellLauncher = shellLauncher;
        _clipboardService = clipboardService;
        _notificationService = notificationService;
        _transferService = transferService;
        _transferDialogService = transferDialogService;

        ClearCommand = new AsyncRelayCommand(_ => ClearAsync(), _ => HasClipboardItems());
        ImportCommand = new AsyncRelayCommand(_ => ImportAsync());
        ExportCommand = new AsyncRelayCommand(_ => ExportAsync(), _ => HasClipboardItems());
        CopyCommand = new AsyncRelayCommand(CopyAsync, CanCopy);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);
        TogglePinCommand = new AsyncRelayCommand(TogglePinAsync, CanTogglePin);
        OpenLinkCommand = new RelayCommand(OpenLink, CanOpenLink);
        OpenFileCommand = new RelayCommand(OpenFile, CanOpenFile);
        OpenFavoriteCommand = new RelayCommand(OpenFavorite, CanOpenFavorite);
        OpenImagePreviewCommand = new RelayCommand(OpenImagePreview, CanOpenImagePreview);
        CloseImagePreviewCommand = new RelayCommand(_ => CloseImagePreview(), _ => IsImagePreviewOpen);
        ZoomInImageCommand = new RelayCommand(_ => ZoomImage(ImagePreviewZoomStep), _ => CanZoomInImage());
        ZoomOutImageCommand = new RelayCommand(_ => ZoomImage(-ImagePreviewZoomStep), _ => CanZoomOutImage());
        ResetImageZoomCommand = new RelayCommand(_ => PreviewZoom = 1.0, _ => IsImagePreviewOpen && Math.Abs(PreviewZoom - 1.0) > 0.001);
        SelectSectionCommand = new RelayCommand(SelectSection);
        ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty, _ => IsSearchActive);

        FilesView = CreateClipboardView(Files, MatchesFile);
        TextsView = CreateClipboardView(Texts, MatchesText);
        UrlsView = CreateClipboardView(Urls, MatchesUrl);
        ImagesView = CreateClipboardView(Images, MatchesImage);
        FavoritesView = CreateFavoritesView();

        Files.CollectionChanged += ClipboardCollectionChanged;
        Texts.CollectionChanged += ClipboardCollectionChanged;
        Urls.CollectionChanged += ClipboardCollectionChanged;
        Images.CollectionChanged += ClipboardCollectionChanged;

        RefreshFilteredCounts();
    }

    public ObservableRangeCollection<FileInfoModel> Files { get; } = [];
    public ObservableRangeCollection<TextModel> Texts { get; } = [];
    public ObservableRangeCollection<UrlModel> Urls { get; } = [];
    public ObservableRangeCollection<ImageModel> Images { get; } = [];
    public ObservableRangeCollection<FavoriteClipboardItem> Favorites { get; } = [];

    public ICollectionView FilesView { get; }
    public ICollectionView TextsView { get; }
    public ICollectionView UrlsView { get; }
    public ICollectionView ImagesView { get; }
    public ICollectionView FavoritesView { get; }

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
            OnPropertyChanged(nameof(IsFavoritesSectionSelected));
        }
    }

    public bool IsFavoritesSectionSelected => SelectedSectionIndex == 0;
    public bool IsFilesSectionSelected => SelectedSectionIndex == 1;
    public bool IsTextsSectionSelected => SelectedSectionIndex == 2;
    public bool IsUrlsSectionSelected => SelectedSectionIndex == 3;
    public bool IsImagesSectionSelected => SelectedSectionIndex == 4;

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

    public string SearchText
    {
        get => _searchText;
        set
        {
            var normalizedValue = value ?? string.Empty;
            if (_searchText == normalizedValue)
            {
                return;
            }

            _searchText = normalizedValue;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSearchActive));
            OnPropertyChanged(nameof(IsSearchPlaceholderVisible));
            OnPropertyChanged(nameof(SearchResultText));
            OnPropertyChanged(nameof(FilesEmptyTitle));
            OnPropertyChanged(nameof(FilesEmptyDescription));
            OnPropertyChanged(nameof(FavoritesEmptyTitle));
            OnPropertyChanged(nameof(FavoritesEmptyDescription));
            OnPropertyChanged(nameof(TextsEmptyTitle));
            OnPropertyChanged(nameof(TextsEmptyDescription));
            OnPropertyChanged(nameof(UrlsEmptyTitle));
            OnPropertyChanged(nameof(UrlsEmptyDescription));
            OnPropertyChanged(nameof(ImagesEmptyTitle));
            OnPropertyChanged(nameof(ImagesEmptyDescription));

            RefreshClipboardViews();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool IsSearchActive => !string.IsNullOrWhiteSpace(SearchText);
    public bool IsSearchPlaceholderVisible => !IsSearchActive;

    public string SearchResultText => IsSearchActive
        ? $"Найдено: {FilteredTotalCount} из {HistoryTotalCount}"
        : string.Empty;

    public int FileCount => IsSearchActive ? _filteredFileCount : Files.Count;
    public int TextCount => IsSearchActive ? _filteredTextCount : Texts.Count;
    public int UrlCount => IsSearchActive ? _filteredUrlCount : Urls.Count;
    public int ImageCount => IsSearchActive ? _filteredImageCount : Images.Count;
    public int FavoriteCount => IsSearchActive ? _filteredFavoriteCount : Favorites.Count;
    public int TotalCount => HistoryTotalCount;
    public int FilteredTotalCount => _filteredFileCount + _filteredTextCount + _filteredUrlCount + _filteredImageCount;
    public int HistoryTotalCount => Files.Count + Texts.Count + Urls.Count + Images.Count;

    public bool HasFavorites => FavoriteCount > 0;
    public bool HasFiles => FileCount > 0;
    public bool HasTexts => TextCount > 0;
    public bool HasUrls => UrlCount > 0;
    public bool HasImages => ImageCount > 0;
    public bool HasItems => HistoryTotalCount > 0;

    public bool IsFavoritesEmpty => !HasFavorites;
    public bool IsFilesEmpty => !HasFiles;
    public bool IsTextsEmpty => !HasTexts;
    public bool IsUrlsEmpty => !HasUrls;
    public bool IsImagesEmpty => !HasImages;
    public bool IsHistoryEmpty => !HasItems;

    public string FavoritesEmptyTitle => IsSearchActive ? "В избранном ничего не найдено" : "Избранного пока нет";
    public string FavoritesEmptyDescription => IsSearchActive
        ? "Попробуйте другой запрос или посмотрите исходные вкладки."
        : "Нажмите звезду на важной записи, чтобы она появилась здесь.";
    public string FilesEmptyTitle => IsSearchActive ? "В файлах ничего не найдено" : "Файлов пока нет";
    public string FilesEmptyDescription => IsSearchActive
        ? "Попробуйте другой запрос или проверьте соседние разделы."
        : "Скопируйте файл в системе, и он появится здесь.";
    public string TextsEmptyTitle => IsSearchActive ? "В тексте ничего не найдено" : "Текстовых записей пока нет";
    public string TextsEmptyDescription => IsSearchActive
        ? "Поиск проверяет содержимое сохраненных текстовых фрагментов."
        : "Скопируйте любой текст, чтобы добавить его в историю.";
    public string UrlsEmptyTitle => IsSearchActive ? "В ссылках ничего не найдено" : "Ссылок пока нет";
    public string UrlsEmptyDescription => IsSearchActive
        ? "Поиск идет по адресу, заголовку и описанию ссылки."
        : "Скопируйте URL, чтобы увидеть карточку ссылки.";
    public string ImagesEmptyTitle => IsSearchActive ? "В изображениях ничего не найдено" : "Изображений пока нет";
    public string ImagesEmptyDescription => IsSearchActive
        ? "Для изображений поиск проверяет имя сохраненной записи."
        : "Скопируйте картинку, чтобы добавить ее в галерею.";

    public ICommand ClearCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand TogglePinCommand { get; }
    public ICommand OpenLinkCommand { get; }
    public ICommand OpenFileCommand { get; }
    public ICommand OpenFavoriteCommand { get; }
    public ICommand OpenImagePreviewCommand { get; }
    public ICommand CloseImagePreviewCommand { get; }
    public ICommand ZoomInImageCommand { get; }
    public ICommand ZoomOutImageCommand { get; }
    public ICommand ResetImageZoomCommand { get; }
    public ICommand SelectSectionCommand { get; }
    public ICommand ClearSearchCommand { get; }

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

    public ImageModel? PreviewImage
    {
        get => _previewImage;
        private set
        {
            if (_previewImage == value)
            {
                return;
            }

            _previewImage = value;
            OnPropertyChanged();
        }
    }

    public object? PreviewImageSource
    {
        get => _previewImageSource;
        private set
        {
            if (_previewImageSource == value)
            {
                return;
            }

            _previewImageSource = value;
            OnPropertyChanged();
        }
    }

    public string PreviewImageName
    {
        get => _previewImageName;
        private set
        {
            if (_previewImageName == value)
            {
                return;
            }

            _previewImageName = value;
            OnPropertyChanged();
        }
    }

    public double PreviewZoom
    {
        get => _previewZoom;
        private set
        {
            var boundedValue = Math.Clamp(value, MinImagePreviewZoom, MaxImagePreviewZoom);
            if (Math.Abs(_previewZoom - boundedValue) < 0.001)
            {
                return;
            }

            _previewZoom = boundedValue;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewZoomText));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string PreviewZoomText => $"{PreviewZoom:P0}";

    public bool IsImagePreviewOpen
    {
        get => _isImagePreviewOpen;
        private set
        {
            if (_isImagePreviewOpen == value)
            {
                return;
            }

            _isImagePreviewOpen = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await RunBusyAsync(async () =>
        {
            var data = await _repository.LoadAsync(cancellationToken);
            var imageHashes = await PrepareLoadedImagesAsync(data.Images, cancellationToken);

            Files.ReplaceRange(data.Files);
            Texts.ReplaceRange(data.Texts);
            Urls.ReplaceRange(data.Urls);
            _knownImageHashes.Clear();

            foreach (var imageHash in imageHashes)
            {
                _knownImageHashes.Add(imageHash);
            }

            Images.ReplaceRange(data.Images);
            RebuildFavorites();
            RefreshClipboardViews();
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
            var snapshot = await _clipboardService.GetCurrentSnapshotAsync(cancellationToken);
            if (snapshot is null)
            {
                return;
            }

            var signature = snapshot.Signature;
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
                    ProcessFileDropList(snapshot, addedItems);
                    break;
                case ClipboardContentKind.Text:
                    await ProcessTextAsync(snapshot, addedItems, cancellationToken);
                    break;
                case ClipboardContentKind.Image:
                    ProcessImage(snapshot, addedItems);
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

    private void ProcessFileDropList(
        ClipboardContentSnapshot snapshot,
        ClipboardItemsBatch addedItems)
    {
        foreach (var file in snapshot.FilePaths)
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

    private async Task ProcessTextAsync(
        ClipboardContentSnapshot snapshot,
        ClipboardItemsBatch addedItems,
        CancellationToken cancellationToken)
    {
        var text = snapshot.Text;
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

    private void ProcessImage(ClipboardContentSnapshot snapshot, ClipboardItemsBatch addedItems)
    {
        var image = snapshot.Image;
        if (image is null)
        {
            return;
        }

        if (!_knownImageHashes.Add(snapshot.Signature.Value))
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

    private async Task ExportAsync(CancellationToken cancellationToken = default)
    {
        var filePath = _transferDialogService.ShowExportDialog();
        if (filePath is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            StatusText = "Экспорт истории...";
            await FlushPendingSavesAsync(cancellationToken);

            var data = new ClipboardData(
                Files.ToArray(),
                Texts.ToArray(),
                Images.ToArray(),
                Urls.ToArray());

            await _transferService.ExportAsync(data, filePath, cancellationToken);
            StatusText = $"История экспортирована: {HistoryTotalCount} элементов";
        });
    }

    private async Task ImportAsync(CancellationToken cancellationToken = default)
    {
        var filePath = _transferDialogService.ShowImportDialog();
        if (filePath is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            StatusText = "Импорт истории...";
            var importedData = await _transferService.ImportAsync(filePath, cancellationToken);
            var result = await MergeImportedDataAsync(importedData, cancellationToken);

            StatusText = result.PinnedUpdatedCount > 0
                ? $"Импортировано новых: {result.AddedCount}, обновлено избранное: {result.PinnedUpdatedCount}"
                : $"Импортировано новых элементов: {result.AddedCount}";
        });
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
                            if (ReferenceEquals(PreviewImage, image))
                            {
                                CloseImagePreview();
                            }
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

    private async Task CopyAsync(object? parameter)
    {
        try
        {
            ClipboardContentSignature? signature = parameter switch
            {
                FileInfoModel file when File.Exists(file.FilePath) =>
                    await _clipboardService.SetFileDropListAsync([file.FilePath]),
                TextModel text when !string.IsNullOrEmpty(text.Text) =>
                    await _clipboardService.SetTextAsync(text.Text),
                ImageModel { ImageSource: not null } image =>
                    await _clipboardService.SetImageAsync(image.ImageSource),
                UrlModel url when !string.IsNullOrWhiteSpace(url.Url) =>
                    await _clipboardService.SetTextAsync(url.Url),
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

    private void OpenFavorite(object? parameter)
    {
        if (parameter is not FavoriteClipboardItem favorite)
        {
            return;
        }

        switch (favorite.Source)
        {
            case FileInfoModel file:
                OpenFile(file);
                break;
            case UrlModel url:
                OpenLink(url);
                break;
            case ImageModel image:
                OpenImagePreview(image);
                break;
        }
    }

    private void OpenImagePreview(object? parameter)
    {
        var image = parameter switch
        {
            ImageModel imageModel => imageModel,
            FavoriteClipboardItem { Source: ImageModel imageModel } => imageModel,
            _ => null
        };

        if (image?.ImageSource is null)
        {
            return;
        }

        PreviewImage = image;
        PreviewImageSource = image.ImageSource;
        PreviewImageName = image.Name;
        PreviewZoom = 1.0;
        IsImagePreviewOpen = true;
        StatusText = "Изображение открыто";
    }

    private void CloseImagePreview()
    {
        IsImagePreviewOpen = false;
        PreviewImage = null;
        PreviewImageSource = null;
        PreviewImageName = string.Empty;
        PreviewZoom = 1.0;
    }

    private void ZoomImage(double delta)
    {
        PreviewZoom += delta;
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

    private async Task TogglePinAsync(object? parameter)
    {
        if (parameter is not IPinnedClipboardItem item)
        {
            return;
        }

        var previousValue = item.IsPinned;
        item.IsPinned = !previousValue;
        RebuildFavorites();
        RefreshClipboardViews();

        try
        {
            await _repository.UpdatePinAsync(item);
            StatusText = item.IsPinned
                ? "Добавлено в избранное"
                : "Удалено из избранного";
        }
        catch (Exception ex)
        {
            item.IsPinned = previousValue;
            RebuildFavorites();
            RefreshClipboardViews();
            ShowError("Pin update failed", ex);
        }
        finally
        {
            CommandManager.InvalidateRequerySuggested();
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

    private async Task<ImportMergeResult> MergeImportedDataAsync(
        ClipboardData importedData,
        CancellationToken cancellationToken)
    {
        var addedItems = new ClipboardItemsBatch();
        var importedImageHashes = new List<string>();
        var pinnedUpdatedCount = 0;

        await _clipboardLock.WaitAsync(cancellationToken);

        try
        {
            await _persistenceLock.WaitAsync(cancellationToken);

            try
            {
                pinnedUpdatedCount += await MergeImportedFilesAsync(importedData.Files, addedItems, cancellationToken);
                pinnedUpdatedCount += await MergeImportedTextsAsync(importedData.Texts, addedItems, cancellationToken);
                pinnedUpdatedCount += await MergeImportedUrlsAsync(importedData.Urls, addedItems, cancellationToken);
                pinnedUpdatedCount += await MergeImportedImagesAsync(
                    importedData.Images,
                    addedItems,
                    importedImageHashes,
                    cancellationToken);

                await _repository.SaveItemsAsync(
                    addedItems.Files,
                    addedItems.Texts,
                    addedItems.Images,
                    addedItems.Urls,
                    cancellationToken);

                foreach (var file in addedItems.Files)
                {
                    Files.Add(file);
                }

                foreach (var text in addedItems.Texts)
                {
                    Texts.Add(text);
                }

                foreach (var url in addedItems.Urls)
                {
                    Urls.Add(url);
                }

                foreach (var image in addedItems.Images)
                {
                    Images.Add(image);
                }

                foreach (var imageHash in importedImageHashes)
                {
                    _knownImageHashes.Add(imageHash);
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

        RebuildFavorites();
        RefreshClipboardViews();
        CommandManager.InvalidateRequerySuggested();

        return new ImportMergeResult(addedItems.TotalCount, pinnedUpdatedCount);
    }

    private async Task<int> MergeImportedFilesAsync(
        IReadOnlyList<FileInfoModel> importedFiles,
        ClipboardItemsBatch addedItems,
        CancellationToken cancellationToken)
    {
        var paths = Files
            .Select(file => file.FilePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pinnedUpdatedCount = 0;

        foreach (var importedFile in importedFiles)
        {
            var filePath = NormalizeImportedFilePath(importedFile.FilePath);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                continue;
            }

            var existingFile = Files.FirstOrDefault(
                file => string.Equals(file.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (existingFile is not null)
            {
                pinnedUpdatedCount += await PinExistingIfNeededAsync(
                    existingFile,
                    importedFile.IsPinned,
                    cancellationToken);
                continue;
            }

            if (!paths.Add(filePath))
            {
                continue;
            }

            importedFile.Id = 0;
            importedFile.FilePath = filePath;
            importedFile.Name = string.IsNullOrWhiteSpace(importedFile.Name)
                ? Path.GetFileName(filePath)
                : importedFile.Name;
            addedItems.Files.Add(importedFile);
        }

        return pinnedUpdatedCount;
    }

    private async Task<int> MergeImportedTextsAsync(
        IReadOnlyList<TextModel> importedTexts,
        ClipboardItemsBatch addedItems,
        CancellationToken cancellationToken)
    {
        var texts = Texts
            .Select(text => text.Text)
            .ToHashSet(StringComparer.Ordinal);
        var pinnedUpdatedCount = 0;

        foreach (var importedText in importedTexts)
        {
            if (string.IsNullOrWhiteSpace(importedText.Text))
            {
                continue;
            }

            var existingText = Texts.FirstOrDefault(text => text.Text == importedText.Text);
            if (existingText is not null)
            {
                pinnedUpdatedCount += await PinExistingIfNeededAsync(
                    existingText,
                    importedText.IsPinned,
                    cancellationToken);
                continue;
            }

            if (!texts.Add(importedText.Text))
            {
                continue;
            }

            importedText.Id = 0;
            addedItems.Texts.Add(importedText);
        }

        return pinnedUpdatedCount;
    }

    private async Task<int> MergeImportedUrlsAsync(
        IReadOnlyList<UrlModel> importedUrls,
        ClipboardItemsBatch addedItems,
        CancellationToken cancellationToken)
    {
        var urls = Urls
            .Select(url => url.Url)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pinnedUpdatedCount = 0;

        foreach (var importedUrl in importedUrls)
        {
            if (string.IsNullOrWhiteSpace(importedUrl.Url))
            {
                continue;
            }

            var existingUrl = Urls.FirstOrDefault(
                url => string.Equals(url.Url, importedUrl.Url, StringComparison.OrdinalIgnoreCase));
            if (existingUrl is not null)
            {
                pinnedUpdatedCount += await PinExistingIfNeededAsync(
                    existingUrl,
                    importedUrl.IsPinned,
                    cancellationToken);
                continue;
            }

            if (!urls.Add(importedUrl.Url))
            {
                continue;
            }

            importedUrl.Id = 0;
            importedUrl.Title = string.IsNullOrWhiteSpace(importedUrl.Title)
                ? importedUrl.Url
                : importedUrl.Title;
            addedItems.Urls.Add(importedUrl);
        }

        return pinnedUpdatedCount;
    }

    private async Task<int> MergeImportedImagesAsync(
        IReadOnlyList<ImageModel> importedImages,
        ClipboardItemsBatch addedItems,
        ICollection<string> importedImageHashes,
        CancellationToken cancellationToken)
    {
        var existingImages = Images.ToArray();
        var imageByHash = await Task.Run(
            () =>
            {
                var hashes = new Dictionary<string, ImageModel>(StringComparer.Ordinal);
                foreach (var image in existingImages)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var imageHash = TryCreateImageHash(image);
                    if (imageHash is not null)
                    {
                        hashes.TryAdd(imageHash, image);
                    }
                }

                return hashes;
            },
            cancellationToken);
        var importedImagesWithHashes = await Task.Run(
            () =>
            {
                return importedImages
                    .Select(image =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return (Image: image, Hash: TryCreateImageHash(image));
                    })
                    .ToArray();
            },
            cancellationToken);

        var pinnedUpdatedCount = 0;

        foreach (var (importedImage, imageHash) in importedImagesWithHashes)
        {
            if (importedImage.ImageSource is null || importedImage.ImageData.Length == 0)
            {
                continue;
            }

            if (imageHash is null)
            {
                continue;
            }

            if (imageByHash.TryGetValue(imageHash, out var existingImage))
            {
                pinnedUpdatedCount += await PinExistingIfNeededAsync(
                    existingImage,
                    importedImage.IsPinned,
                    cancellationToken);
                continue;
            }

            if (importedImageHashes.Contains(imageHash))
            {
                continue;
            }

            importedImage.Id = 0;
            importedImageHashes.Add(imageHash);
            addedItems.Images.Add(importedImage);
        }

        return pinnedUpdatedCount;
    }

    private async Task<int> PinExistingIfNeededAsync(
        IPinnedClipboardItem existingItem,
        bool shouldPin,
        CancellationToken cancellationToken)
    {
        if (!shouldPin || existingItem.IsPinned)
        {
            return 0;
        }

        existingItem.IsPinned = true;
        await _repository.UpdatePinAsync(existingItem, cancellationToken);
        return 1;
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

    private static bool CanTogglePin(object? parameter)
    {
        return parameter is IPinnedClipboardItem;
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

    private static bool CanOpenFavorite(object? parameter)
    {
        return parameter is FavoriteClipboardItem { Source: FileInfoModel file } && File.Exists(file.FilePath)
            || parameter is FavoriteClipboardItem { Source: UrlModel url }
            && Uri.TryCreate(url.Url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            || parameter is FavoriteClipboardItem { Source: ImageModel { ImageSource: not null } };
    }

    private static bool CanOpenImagePreview(object? parameter)
    {
        return parameter is ImageModel { ImageSource: not null }
            || parameter is FavoriteClipboardItem { Source: ImageModel { ImageSource: not null } };
    }

    private bool CanZoomInImage()
    {
        return IsImagePreviewOpen && PreviewZoom < MaxImagePreviewZoom;
    }

    private bool CanZoomOutImage()
    {
        return IsImagePreviewOpen && PreviewZoom > MinImagePreviewZoom;
    }

    private bool HasClipboardItems()
    {
        return HasItems;
    }

    private void ClipboardCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildFavorites();
        RefreshClipboardViews();
        CommandManager.InvalidateRequerySuggested();
    }

    private void RaiseClipboardStateChanged()
    {
        OnPropertyChanged(nameof(FileCount));
        OnPropertyChanged(nameof(TextCount));
        OnPropertyChanged(nameof(UrlCount));
        OnPropertyChanged(nameof(ImageCount));
        OnPropertyChanged(nameof(FavoriteCount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(FilteredTotalCount));
        OnPropertyChanged(nameof(HistoryTotalCount));
        OnPropertyChanged(nameof(SearchResultText));
        OnPropertyChanged(nameof(HasFavorites));
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(HasTexts));
        OnPropertyChanged(nameof(HasUrls));
        OnPropertyChanged(nameof(HasImages));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsFavoritesEmpty));
        OnPropertyChanged(nameof(IsFilesEmpty));
        OnPropertyChanged(nameof(IsTextsEmpty));
        OnPropertyChanged(nameof(IsUrlsEmpty));
        OnPropertyChanged(nameof(IsImagesEmpty));
        OnPropertyChanged(nameof(IsHistoryEmpty));
    }

    private ICollectionView CreateClipboardView<T>(
        ObservableRangeCollection<T> collection,
        Predicate<T> filter)
        where T : ClipboardItemModel
    {
        var view = CollectionViewSource.GetDefaultView(collection);
        view.Filter = item => item is T typedItem && filter(typedItem);
        view.SortDescriptions.Add(new SortDescription(nameof(ClipboardItemModel.IsPinned), ListSortDirection.Descending));
        return view;
    }

    private ICollectionView CreateFavoritesView()
    {
        var view = CollectionViewSource.GetDefaultView(Favorites);
        view.Filter = item => item is FavoriteClipboardItem favorite && MatchesFavorite(favorite);
        return view;
    }

    private void RefreshClipboardViews()
    {
        FavoritesView.Refresh();
        FilesView.Refresh();
        TextsView.Refresh();
        UrlsView.Refresh();
        ImagesView.Refresh();
        RefreshFilteredCounts();
        RaiseClipboardStateChanged();
    }

    private void RefreshFilteredCounts()
    {
        _filteredFavoriteCount = FavoritesView.Cast<object>().Count();
        _filteredFileCount = FilesView.Cast<object>().Count();
        _filteredTextCount = TextsView.Cast<object>().Count();
        _filteredUrlCount = UrlsView.Cast<object>().Count();
        _filteredImageCount = ImagesView.Cast<object>().Count();
    }

    private void RebuildFavorites()
    {
        var favorites = Files
            .Where(x => x.IsPinned)
            .Select(x => new FavoriteClipboardItem(x))
            .Concat(Texts
                .Where(x => x.IsPinned)
                .Select(x => new FavoriteClipboardItem(x)))
            .Concat(Urls
                .Where(x => x.IsPinned)
                .Select(x => new FavoriteClipboardItem(x)))
            .Concat(Images
                .Where(x => x.IsPinned)
                .Select(x => new FavoriteClipboardItem(x)))
            .ToArray();

        Favorites.ReplaceRange(favorites);
    }

    private bool MatchesFavorite(FavoriteClipboardItem favorite)
    {
        return favorite.Source switch
        {
            FileInfoModel file => MatchesFile(file),
            TextModel text => MatchesText(text),
            UrlModel url => MatchesUrl(url),
            ImageModel image => MatchesImage(image),
            _ => false
        };
    }

    private bool MatchesFile(FileInfoModel file)
    {
        return MatchesSearch(file.Name, file.FilePath);
    }

    private bool MatchesText(TextModel text)
    {
        return MatchesSearch(text.Text);
    }

    private bool MatchesUrl(UrlModel url)
    {
        return MatchesSearch(url.Title, url.Url, url.Description);
    }

    private bool MatchesImage(ImageModel image)
    {
        return MatchesSearch(image.Name);
    }

    private bool MatchesSearch(params string?[] values)
    {
        var query = SearchText.Trim();
        return query.Length == 0
            || values.Any(value => value?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true);
    }

    private static string NormalizeImportedFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetFullPath(filePath.Trim());
        }
        catch
        {
            return filePath.Trim();
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

    private async Task<IReadOnlyCollection<string>> PrepareLoadedImagesAsync(
        IReadOnlyCollection<ImageModel> images,
        CancellationToken cancellationToken)
    {
        if (images.Count == 0)
        {
            return [];
        }

        return await Task.Run(
            () =>
            {
                var hashes = new List<string>(images.Count);

                foreach (var image in images)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    image.ImageSource = BitmapSourceExtensions.ByteArrayToBitmapSource(image.ImageData);
                    if (image.ImageSource is not null)
                    {
                        hashes.Add(_clipboardService.CreateImageSignature(image.ImageSource).Value);
                    }
                }

                return (IReadOnlyCollection<string>)hashes;
            },
            cancellationToken);
    }

    private void TryLaunch(Action action)
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

    private void ShowError(string title, Exception exception)
    {
        _notificationService.ShowError(title, exception.Message);
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

internal readonly record struct ImportMergeResult(int AddedCount, int PinnedUpdatedCount);

public sealed class FavoriteClipboardItem
{
    private const int TitleLimit = 64;
    private const int DescriptionLimit = 180;

    public FavoriteClipboardItem(ClipboardItemModel source)
    {
        Source = source;

        switch (source)
        {
            case FileInfoModel file:
                TypeLabel = "Файл";
                Title = Shorten(file.Name, TitleLimit);
                Subtitle = Shorten(file.FilePath, DescriptionLimit);
                Description = file.FilePath;
                ShowsIconPreview = true;
                break;
            case TextModel text:
                TypeLabel = "Текст";
                Title = Shorten(GetFirstLine(text.Text), TitleLimit);
                Subtitle = "Текстовая запись";
                Description = Shorten(text.Text, DescriptionLimit);
                ShowsTextPreview = true;
                break;
            case UrlModel url:
                TypeLabel = "Ссылка";
                Title = Shorten(string.IsNullOrWhiteSpace(url.Title) ? url.Url : url.Title, TitleLimit);
                Subtitle = Shorten(url.Url, DescriptionLimit);
                Description = Shorten(url.Description, DescriptionLimit);
                PreviewSource = url.ImageUrl;
                ShowsImagePreview = !string.IsNullOrWhiteSpace(url.ImageUrl);
                ShowsIconPreview = !ShowsImagePreview;
                HasOpenAction = true;
                break;
            case ImageModel image:
                TypeLabel = "Изображение";
                Title = Shorten(image.Name, TitleLimit);
                Subtitle = "Изображение";
                Description = image.Name;
                PreviewSource = image.ImageSource;
                ShowsImagePreview = image.ImageSource is not null;
                ShowsIconPreview = !ShowsImagePreview;
                break;
            default:
                TypeLabel = "Запись";
                Title = "Избранная запись";
                Subtitle = string.Empty;
                Description = string.Empty;
                ShowsIconPreview = true;
                break;
        }

        HasOpenAction = HasOpenAction || source is FileInfoModel or ImageModel;
    }

    public ClipboardItemModel Source { get; }
    public string TypeLabel { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string Description { get; }
    public object? PreviewSource { get; }
    public bool ShowsImagePreview { get; }
    public bool ShowsStaticImagePreview => ShowsImagePreview && !HasImagePreviewAction;
    public bool ShowsTextPreview { get; }
    public bool ShowsIconPreview { get; }
    public bool HasOpenAction { get; }
    public bool HasImagePreviewAction => Source is ImageModel { ImageSource: not null };

    private static string GetFirstLine(string value)
    {
        return value
            .ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? "Текстовая запись";
    }

    private static string Shorten(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmedValue = value.Trim();
        return trimmedValue.Length <= maxLength
            ? trimmedValue
            : string.Concat(trimmedValue.AsSpan(0, maxLength - 1), "…");
    }
}
