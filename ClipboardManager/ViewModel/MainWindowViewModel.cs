using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ClipboardManager.Data;
using ClipboardManager.Helper;
using ClipboardManager.Interfaces;
using ClipboardManager.Localization;
using ClipboardManager.Models;
using ClipboardManager.Services;

namespace ClipboardManager.ViewModels;

public sealed partial class MainWindowViewModel : BaseViewModel
{
    private const double MinImagePreviewZoom = 0.25;
    private const double MaxImagePreviewZoom = 4.0;
    private const double ImagePreviewZoomStep = 0.25;
    private const int SecretClipboardClearSeconds = 45;
    private const int SecretCopyTrustSeconds = 30;
    private const int MaxStaleLinkRefreshPerRun = 10;
    private const int MaxUrlPreviewImageConcurrency = 4;
    private const int ImageThumbnailDecodePixelWidth = 540;
    private const int UrlPreviewImageDecodePixelWidth = 144;
    private const string FilesSectionKey = "Files";
    private const string TextsSectionKey = "Texts";
    private const string UrlsSectionKey = "Urls";
    private const string ImagesSectionKey = "Images";
    private const string SecretsSectionKey = "Secrets";
    private static readonly TimeSpan SecretRevealDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SecretCopyTrustDuration = TimeSpan.FromSeconds(SecretCopyTrustSeconds);
    private static readonly TimeSpan SecretClipboardSuppressionDuration = TimeSpan.FromSeconds(60);

    private readonly IClipboardFileCaptureService _fileCaptureService;
    private readonly IClipboardTextCaptureService _textCaptureService;
    private readonly IClipboardItemLookupService _itemLookupService;
    private readonly ILinkPreviewImageService _linkPreviewImageService;
    private readonly ILinkMetadataRefreshService _linkMetadataRefreshService;
    private readonly IShellLauncher _shellLauncher;
    private readonly IClipboardService _clipboardService;
    private readonly IImageStorageService _imageStorageService;
    private readonly IClipboardItemPersistenceService _itemPersistenceService;
    private readonly IClipboardHistoryService _historyService;
    private readonly IUserNotificationService _notificationService;
    private readonly IClipboardTransferService _transferService;
    private readonly IClipboardTransferDialogService _transferDialogService;
    private readonly ISecretProtectionService _secretProtectionService;
    private readonly IUserConsentService _userConsentService;
    private readonly ISecretDialogService _secretDialogService;
    private readonly LocalizationService _localization;
    private readonly AppSettings _settings;
    private readonly ClipboardChangeSuppressor _clipboardChangeSuppressor = new();
    private readonly SemaphoreSlim _clipboardLock = new(1, 1);
    private readonly SemaphoreSlim _persistenceLock = new(1, 1);
    private readonly SemaphoreSlim _urlPreviewImageLoadLock = new(MaxUrlPreviewImageConcurrency, MaxUrlPreviewImageConcurrency);
    private readonly SemaphoreSlim _linkMetadataRefreshLock = new(1, 1);
    private readonly HashSet<string> _knownFilePaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _knownTexts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _knownUrls = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _knownUrlTextValues = new(StringComparer.Ordinal);
    private readonly HashSet<string> _knownImageHashes = [];
    private readonly Dictionary<SecretModel, CancellationTokenSource> _secretRevealTimers = [];
    private readonly Dictionary<SecretModel, DateTimeOffset> _trustedSecretCopyExpirations = [];

    private ClipboardContentSignature? _lastHandledClipboardSignature;
    private ClipboardContentSignature? _pendingSecretClipboardSignature;
    private CancellationTokenSource? _secretClipboardClearCancellation;
    private bool _isBusy;
    private bool _isLoadingMoreHistory;
    private int _activeLinkLoadCount;
    private int _bulkCollectionUpdateDepth;
    private int _selectedSectionIndex;
    private int _filePageOffset;
    private int _textPageOffset;
    private int _urlPageOffset;
    private int _imagePageOffset;
    private int _secretPageOffset;
    private int _totalFileCount;
    private int _totalTextCount;
    private int _totalUrlCount;
    private int _totalImageCount;
    private int _totalSecretCount;
    private Func<LocalizationService, string> _statusTextFactory = text => text.MonitoringActiveStatus;
    private string _searchText = string.Empty;
    private ImageModel? _previewImage;
    private object? _previewImageSource;
    private string _previewImageName = string.Empty;
    private double _previewZoom = 1.0;
    private bool _isImagePreviewOpen;
    private bool _isSettingsOpen;
    private DateTime? _lastActivityAt;
    private int _filteredFileCount;
    private int _filteredTextCount;
    private int _filteredUrlCount;
    private int _filteredImageCount;
    private int _filteredSecretCount;
    private int _filteredFavoriteCount;
    private IReadOnlyList<LinkRefreshIntervalOption> _linkRefreshIntervalOptions = [];

    public MainWindowViewModel(
        IClipboardFileCaptureService fileCaptureService,
        IClipboardTextCaptureService textCaptureService,
        IClipboardItemLookupService itemLookupService,
        ILinkPreviewImageService linkPreviewImageService,
        ILinkMetadataRefreshService linkMetadataRefreshService,
        IShellLauncher shellLauncher,
        IClipboardService clipboardService,
        IImageStorageService imageStorageService,
        IClipboardItemPersistenceService itemPersistenceService,
        IClipboardHistoryService historyService,
        IUserNotificationService notificationService,
        IClipboardTransferService transferService,
        IClipboardTransferDialogService transferDialogService,
        ISecretProtectionService secretProtectionService,
        IUserConsentService userConsentService,
        ISecretDialogService secretDialogService,
        LocalizationService localization,
        AppSettings settings)
    {
        _fileCaptureService = fileCaptureService;
        _textCaptureService = textCaptureService;
        _itemLookupService = itemLookupService;
        _linkPreviewImageService = linkPreviewImageService;
        _linkMetadataRefreshService = linkMetadataRefreshService;
        _shellLauncher = shellLauncher;
        _clipboardService = clipboardService;
        _imageStorageService = imageStorageService;
        _itemPersistenceService = itemPersistenceService;
        _historyService = historyService;
        _notificationService = notificationService;
        _transferService = transferService;
        _transferDialogService = transferDialogService;
        _secretProtectionService = secretProtectionService;
        _userConsentService = userConsentService;
        _secretDialogService = secretDialogService;
        _localization = localization;
        _settings = settings;
        _linkRefreshIntervalOptions = CreateLinkRefreshIntervalOptions();

        ClearCommand = CreateAsyncCommand(_ => ClearAsync(), _ => HasClipboardItems());
        ImportCommand = CreateAsyncCommand(_ => ImportAsync());
        ExportCommand = CreateAsyncCommand(_ => ExportAsync(), _ => HasClipboardItems());
        LoadMoreCommand = CreateAsyncCommand(LoadMoreAsync, CanLoadMore);
        CopyCommand = CreateAsyncCommand(CopyAsync, CanCopy);
        DeleteCommand = CreateAsyncCommand(DeleteAsync, CanDelete);
        TogglePinCommand = CreateAsyncCommand(TogglePinAsync, CanTogglePin);
        SaveTextAsSecretCommand = CreateAsyncCommand(SaveTextAsSecretAsync, CanSaveTextAsSecret);
        RevealSecretCommand = CreateAsyncCommand(RevealSecretAsync, CanRevealSecret);
        HideSecretCommand = new RelayCommand(HideSecret, CanHideSecret);
        OpenLinkCommand = new RelayCommand(OpenLink, CanOpenLink);
        OpenFileCommand = new RelayCommand(OpenFile, CanOpenFile);
        OpenFavoriteCommand = CreateAsyncCommand(OpenFavoriteAsync, CanOpenFavorite);
        OpenImagePreviewCommand = CreateAsyncCommand(OpenImagePreviewAsync, CanOpenImagePreview);
        SaveImageCommand = CreateAsyncCommand(SaveImageAsync, CanSaveImage);
        CloseImagePreviewCommand = new RelayCommand(_ => CloseImagePreview(), _ => IsImagePreviewOpen);
        ZoomInImageCommand = new RelayCommand(_ => ZoomImage(ImagePreviewZoomStep), _ => CanZoomInImage());
        ZoomOutImageCommand = new RelayCommand(_ => ZoomImage(-ImagePreviewZoomStep), _ => CanZoomOutImage());
        ResetImageZoomCommand = new RelayCommand(_ => PreviewZoom = 1.0, _ => IsImagePreviewOpen && Math.Abs(PreviewZoom - 1.0) > 0.001);
        SelectSectionCommand = new RelayCommand(SelectSection);
        ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty, _ => IsSearchActive);
        ChangeLanguageCommand = new RelayCommand(ChangeLanguage);
        ToggleSettingsCommand = new RelayCommand(_ => IsSettingsOpen = !IsSettingsOpen);
        ClearLinkPreviewCacheCommand = CreateAsyncCommand(_ => ClearLinkPreviewCacheAsync());

        FilesView = CreateClipboardView(Files, MatchesFile);
        TextsView = CreateClipboardView(Texts, MatchesText);
        UrlsView = CreateClipboardView(Urls, MatchesUrl);
        ImagesView = CreateClipboardView(Images, MatchesImage);
        SecretsView = CreateClipboardView(Secrets, MatchesSecret);
        FavoritesView = CreateFavoritesView();

        Files.CollectionChanged += ClipboardCollectionChanged;
        Texts.CollectionChanged += ClipboardCollectionChanged;
        Urls.CollectionChanged += ClipboardCollectionChanged;
        Images.CollectionChanged += ClipboardCollectionChanged;
        Secrets.CollectionChanged += ClipboardCollectionChanged;

        RefreshFilteredCounts();
    }

    public ObservableRangeCollection<FileInfoModel> Files { get; } = [];
    public ObservableRangeCollection<TextModel> Texts { get; } = [];
    public ObservableRangeCollection<UrlModel> Urls { get; } = [];
    public ObservableRangeCollection<ImageModel> Images { get; } = [];
    public ObservableRangeCollection<SecretModel> Secrets { get; } = [];
    public ObservableRangeCollection<FavoriteClipboardItem> Favorites { get; } = [];

    public ICollectionView FilesView { get; }
    public ICollectionView TextsView { get; }
    public ICollectionView UrlsView { get; }
    public ICollectionView ImagesView { get; }
    public ICollectionView SecretsView { get; }
    public ICollectionView FavoritesView { get; }

    public event EventHandler<AppSettings>? SettingsChanged;

    public LocalizationService L => _localization;

    public string SelectedLanguageCode
    {
        get => AppLanguageParser.ToCode(_localization.Language);
        set
        {
            if (!AppLanguageParser.TryParse(value, out var language) || language == _localization.Language)
            {
                return;
            }

            _localization.UseLanguage(language);
            _settings.Language = language;
            SaveSettings();

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEnglishLanguageSelected));
            OnPropertyChanged(nameof(IsRussianLanguageSelected));
            RefreshLocalizedState();
        }
    }

    public bool IsEnglishLanguageSelected => _localization.Language == AppLanguage.English;
    public bool IsRussianLanguageSelected => _localization.Language == AppLanguage.Russian;

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set
        {
            if (_isSettingsOpen == value)
            {
                return;
            }

            _isSettingsOpen = value;
            OnPropertyChanged();
        }
    }

    public bool MinimizeToTray
    {
        get => _settings.MinimizeToTray;
        set
        {
            if (_settings.MinimizeToTray == value)
            {
                return;
            }

            _settings.MinimizeToTray = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public bool GlobalHotKeyEnabled
    {
        get => _settings.GlobalHotKeyEnabled;
        set
        {
            if (_settings.GlobalHotKeyEnabled == value)
            {
                return;
            }

            _settings.GlobalHotKeyEnabled = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public bool StartWithWindows
    {
        get => _settings.StartWithWindows;
        set
        {
            if (_settings.StartWithWindows == value)
            {
                return;
            }

            _settings.StartWithWindows = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public IReadOnlyList<int> HistoryBatchSizeOptions { get; } = [25, 50, 100, 200, 500, 1000];

    public int HistoryBatchSize
    {
        get => AppSettings.NormalizeHistoryBatchSize(_settings.HistoryBatchSize);
        set
        {
            var normalizedValue = AppSettings.NormalizeHistoryBatchSize(value);
            if (_settings.HistoryBatchSize == normalizedValue)
            {
                return;
            }

            _settings.HistoryBatchSize = normalizedValue;
            OnPropertyChanged();
            SaveSettings();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public IReadOnlyList<LinkRefreshIntervalOption> LinkRefreshIntervalOptions => _linkRefreshIntervalOptions;

    public int LinkRefreshIntervalDays
    {
        get => AppSettings.NormalizeLinkRefreshIntervalDays(_settings.LinkRefreshIntervalDays);
        set
        {
            var normalizedValue = AppSettings.NormalizeLinkRefreshIntervalDays(value);
            if (_settings.LinkRefreshIntervalDays == normalizedValue)
            {
                return;
            }

            _settings.LinkRefreshIntervalDays = normalizedValue;
            OnPropertyChanged();
            SaveSettings();
            QueueStaleLinkRefresh();
        }
    }

    public string GlobalHotKeyText => "Ctrl+Alt+V";

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
            OnPropertyChanged(nameof(IsSecretsSectionSelected));
            OnPropertyChanged(nameof(IsFavoritesSectionSelected));
        }
    }

    public bool IsFavoritesSectionSelected => SelectedSectionIndex == 0;
    public bool IsSecretsSectionSelected => SelectedSectionIndex == 1;
    public bool IsFilesSectionSelected => SelectedSectionIndex == 2;
    public bool IsTextsSectionSelected => SelectedSectionIndex == 3;
    public bool IsUrlsSectionSelected => SelectedSectionIndex == 4;
    public bool IsImagesSectionSelected => SelectedSectionIndex == 5;

    public string StatusText => _statusTextFactory(_localization);

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
        ? _localization.NoNewEntriesText
        : _localization.LastActivityText(LastActivityAt.Value);

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
            OnPropertyChanged(nameof(SecretsEmptyTitle));
            OnPropertyChanged(nameof(SecretsEmptyDescription));

            RefreshClipboardViews();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool IsSearchActive => !string.IsNullOrWhiteSpace(SearchText);
    public bool IsSearchPlaceholderVisible => !IsSearchActive;

    public string SearchResultText => IsSearchActive
        ? _localization.SearchResultText(FilteredTotalCount, TotalCount)
        : string.Empty;

    public int FileCount => IsSearchActive ? _filteredFileCount : _totalFileCount;
    public int TextCount => IsSearchActive ? _filteredTextCount : _totalTextCount;
    public int UrlCount => IsSearchActive ? _filteredUrlCount : _totalUrlCount;
    public int ImageCount => IsSearchActive ? _filteredImageCount : _totalImageCount;
    public int SecretCount => IsSearchActive ? _filteredSecretCount : _totalSecretCount;
    public int FavoriteCount => IsSearchActive ? _filteredFavoriteCount : Favorites.Count;
    public int TotalCount => HistoryTotalCount + _totalSecretCount;
    public int FilteredTotalCount => _filteredFileCount + _filteredTextCount + _filteredUrlCount + _filteredImageCount + _filteredSecretCount;
    public int HistoryTotalCount => _totalFileCount + _totalTextCount + _totalUrlCount + _totalImageCount;

    public bool HasFavorites => FavoriteCount > 0;
    public bool HasFiles => FileCount > 0;
    public bool HasTexts => TextCount > 0;
    public bool HasUrls => UrlCount > 0;
    public bool HasImages => ImageCount > 0;
    public bool HasSecrets => SecretCount > 0;
    public bool HasItems => HistoryTotalCount > 0;

    public bool IsFavoritesEmpty => !HasFavorites;
    public bool IsFilesEmpty => !HasFiles;
    public bool IsTextsEmpty => !HasTexts;
    public bool IsUrlsEmpty => !HasUrls;
    public bool IsImagesEmpty => !HasImages;
    public bool IsSecretsEmpty => !HasSecrets;
    public bool IsHistoryEmpty => !HasItems;

    public string FavoritesEmptyTitle => _localization.FavoritesEmptyTitle(IsSearchActive);
    public string FavoritesEmptyDescription => _localization.FavoritesEmptyDescription(IsSearchActive);
    public string FilesEmptyTitle => _localization.FilesEmptyTitle(IsSearchActive);
    public string FilesEmptyDescription => _localization.FilesEmptyDescription(IsSearchActive);
    public string TextsEmptyTitle => _localization.TextsEmptyTitle(IsSearchActive);
    public string TextsEmptyDescription => _localization.TextsEmptyDescription(IsSearchActive);
    public string UrlsEmptyTitle => _localization.UrlsEmptyTitle(IsSearchActive);
    public string UrlsEmptyDescription => _localization.UrlsEmptyDescription(IsSearchActive);
    public string ImagesEmptyTitle => _localization.ImagesEmptyTitle(IsSearchActive);
    public string ImagesEmptyDescription => _localization.ImagesEmptyDescription(IsSearchActive);
    public string SecretsEmptyTitle => _localization.SecretsEmptyTitle(IsSearchActive);
    public string SecretsEmptyDescription => _localization.SecretsEmptyDescription(IsSearchActive);

    public ICommand ClearCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand LoadMoreCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand TogglePinCommand { get; }
    public ICommand SaveTextAsSecretCommand { get; }
    public ICommand RevealSecretCommand { get; }
    public ICommand HideSecretCommand { get; }
    public ICommand OpenLinkCommand { get; }
    public ICommand OpenFileCommand { get; }
    public ICommand OpenFavoriteCommand { get; }
    public ICommand OpenImagePreviewCommand { get; }
    public ICommand SaveImageCommand { get; }
    public ICommand CloseImagePreviewCommand { get; }
    public ICommand ZoomInImageCommand { get; }
    public ICommand ZoomOutImageCommand { get; }
    public ICommand ResetImageZoomCommand { get; }
    public ICommand SelectSectionCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand ChangeLanguageCommand { get; }
    public ICommand ToggleSettingsCommand { get; }
    public ICommand ClearLinkPreviewCacheCommand { get; }

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
            RaiseActivityStateChanged();
        }
    }

    public bool IsLoadingMoreHistory
    {
        get => _isLoadingMoreHistory;
        private set
        {
            if (_isLoadingMoreHistory == value)
            {
                return;
            }

            _isLoadingMoreHistory = value;
            OnPropertyChanged();
            RaiseActivityStateChanged();
        }
    }

    public bool IsLoadingLinks => _activeLinkLoadCount > 0;
    public bool IsActivityIndicatorVisible => IsBusy || IsLoadingMoreHistory || IsLoadingLinks;
    public string ActivityText => IsBusy
        ? _localization.LoadingHistoryStatus
        : IsLoadingMoreHistory
            ? _localization.LoadingMoreHistoryStatus
            : IsLoadingLinks
                ? _localization.LoadingLinksStatus
                : string.Empty;

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

    private bool HasClipboardItems()
    {
        return HasItems;
    }

    private void ClipboardCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_bulkCollectionUpdateDepth > 0)
        {
            return;
        }

        RebuildFavorites();
        RefreshClipboardViews();
        CommandManager.InvalidateRequerySuggested();
    }

    private void BeginBulkCollectionUpdate()
    {
        _bulkCollectionUpdateDepth++;
    }

    private void EndBulkCollectionUpdate()
    {
        if (_bulkCollectionUpdateDepth > 0)
        {
            _bulkCollectionUpdateDepth--;
        }

        if (_bulkCollectionUpdateDepth == 0)
        {
            RebuildFavorites();
            RefreshClipboardViews();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void RaiseClipboardStateChanged()
    {
        OnPropertyChanged(nameof(FileCount));
        OnPropertyChanged(nameof(TextCount));
        OnPropertyChanged(nameof(UrlCount));
        OnPropertyChanged(nameof(ImageCount));
        OnPropertyChanged(nameof(SecretCount));
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
        OnPropertyChanged(nameof(HasSecrets));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsFavoritesEmpty));
        OnPropertyChanged(nameof(IsFilesEmpty));
        OnPropertyChanged(nameof(IsTextsEmpty));
        OnPropertyChanged(nameof(IsUrlsEmpty));
        OnPropertyChanged(nameof(IsImagesEmpty));
        OnPropertyChanged(nameof(IsSecretsEmpty));
        OnPropertyChanged(nameof(IsHistoryEmpty));
        RaiseLocalizedTextChanged();
    }

    private void RaiseLocalizedTextChanged()
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(ActivityText));
        OnPropertyChanged(nameof(LastActivityText));
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
        OnPropertyChanged(nameof(SecretsEmptyTitle));
        OnPropertyChanged(nameof(SecretsEmptyDescription));
    }

    private void RefreshLocalizedState()
    {
        RefreshLinkRefreshIntervalOptionLabels();
        OnPropertyChanged(nameof(LinkRefreshIntervalDays));
        RebuildFavorites();
        RefreshClipboardViews();
        RaiseLocalizedTextChanged();
    }

    private IReadOnlyList<LinkRefreshIntervalOption> CreateLinkRefreshIntervalOptions()
    {
        return
        [
            new(AppSettings.LinkRefreshNeverDays, _localization.LinkRefreshIntervalOptionText(AppSettings.LinkRefreshNeverDays)),
            new(AppSettings.LinkRefreshWeeklyDays, _localization.LinkRefreshIntervalOptionText(AppSettings.LinkRefreshWeeklyDays)),
            new(AppSettings.LinkRefreshMonthlyDays, _localization.LinkRefreshIntervalOptionText(AppSettings.LinkRefreshMonthlyDays))
        ];
    }

    private void RefreshLinkRefreshIntervalOptionLabels()
    {
        foreach (var option in _linkRefreshIntervalOptions)
        {
            option.DisplayName = _localization.LinkRefreshIntervalOptionText(option.Days);
        }
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
        SecretsView.Refresh();
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
        _filteredSecretCount = SecretsView.Cast<object>().Count();
    }

    private void SetTotalCounts(ClipboardCounts counts)
    {
        SetHistoryCounts(counts.Files, counts.Texts, counts.Images, counts.Urls);
        _totalSecretCount = counts.Secrets;
    }

    private void SetHistoryCounts(int files, int texts, int images, int urls)
    {
        _totalFileCount = Math.Max(0, files);
        _totalTextCount = Math.Max(0, texts);
        _totalImageCount = Math.Max(0, images);
        _totalUrlCount = Math.Max(0, urls);
    }

    private void AddToTotalCounts(ClipboardItemsBatch addedItems)
    {
        _totalFileCount += addedItems.Files.Count;
        _totalTextCount += addedItems.Texts.Count;
        _totalImageCount += addedItems.Images.Count;
        _totalUrlCount += addedItems.Urls.Count;
        _filePageOffset = Math.Min(_filePageOffset + addedItems.Files.Count, _totalFileCount);
        _textPageOffset = Math.Min(_textPageOffset + addedItems.Texts.Count, _totalTextCount);
        _imagePageOffset = Math.Min(_imagePageOffset + addedItems.Images.Count, _totalImageCount);
        _urlPageOffset = Math.Min(_urlPageOffset + addedItems.Urls.Count, _totalUrlCount);
        RaiseClipboardStateChanged();
    }

    private void RebuildLookupIndexes()
    {
        ClearLookupIndexes();

        foreach (var file in Files)
        {
            AddFileToLookup(file);
        }

        foreach (var text in Texts)
        {
            if (!string.IsNullOrWhiteSpace(text.Text))
            {
                _knownTexts.Add(text.Text);
            }
        }

        foreach (var url in Urls)
        {
            AddUrlToLookup(url);
        }
    }

    private void ClearLookupIndexes()
    {
        _knownFilePaths.Clear();
        _knownTexts.Clear();
        _knownUrls.Clear();
        _knownUrlTextValues.Clear();
    }

    private void AddFileToLookup(FileInfoModel file)
    {
        var filePath = NormalizeFilePathForLookup(file.FilePath);
        if (filePath.Length > 0)
        {
            _knownFilePaths.Add(filePath);
        }
    }

    private void RemoveFileFromLookup(FileInfoModel file)
    {
        var filePath = NormalizeFilePathForLookup(file.FilePath);
        if (filePath.Length > 0)
        {
            _knownFilePaths.Remove(filePath);
        }
    }

    private void RebuildFavorites()
    {
        var favorites = Files
            .Where(x => x.IsPinned)
            .Select(x => new FavoriteClipboardItem(x, _localization))
            .Concat(Texts
                .Where(x => x.IsPinned)
                .Select(x => new FavoriteClipboardItem(x, _localization)))
            .Concat(Urls
                .Where(x => x.IsPinned)
                .Select(x => new FavoriteClipboardItem(x, _localization)))
            .Concat(Images
                .Where(x => x.IsPinned)
                .Select(x => new FavoriteClipboardItem(x, _localization)))
            .Concat(Secrets
                .Where(x => x.IsPinned)
                .Select(x => new FavoriteClipboardItem(x, _localization)))
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
            SecretModel secret => MatchesSecret(secret),
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

    private bool MatchesImage(ImageModel image)
    {
        return MatchesSearch(image.Name);
    }

    private bool MatchesSecret(SecretModel secret)
    {
        return MatchesSearch(secret.Name);
    }

    private bool MatchesSearch(params string?[] values)
    {
        var query = SearchText.Trim();
        return query.Length == 0
            || values.Any(value => value?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true);
    }

    private static string NormalizeFilePathForLookup(string? filePath)
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

    private void SetStatus(Func<LocalizationService, string> statusTextFactory)
    {
        _statusTextFactory = statusTextFactory;
        OnPropertyChanged(nameof(StatusText));
    }

    private void RaiseActivityStateChanged()
    {
        OnPropertyChanged(nameof(IsLoadingLinks));
        OnPropertyChanged(nameof(IsActivityIndicatorVisible));
        OnPropertyChanged(nameof(ActivityText));
    }

    private void SaveSettings()
    {
        try
        {
            AppSettingsStore.Save(_settings);
            SettingsChanged?.Invoke(this, _settings);
        }
        catch (Exception ex)
        {
            ShowError(_localization.OperationFailedTitle, ex);
        }
    }

    private AsyncRelayCommand CreateAsyncCommand(
        Func<object?, Task> execute,
        Predicate<object?>? canExecute = null)
    {
        return new AsyncRelayCommand(
            execute,
            canExecute,
            ex => ShowError(_localization.OperationFailedTitle, ex));
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
            ShowError(_localization.OperationFailedTitle, ex);
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

                    image.ImageSource = null;
                    image.ThumbnailSource = BitmapSourceExtensions.ByteArrayToBitmapSource(
                        image.ImageData,
                        ImageThumbnailDecodePixelWidth);

                    var imageSource = BitmapSourceExtensions.ByteArrayToBitmapSource(image.ImageData);
                    if (imageSource is not null)
                    {
                        hashes.Add(_clipboardService.CreateImageSignature(imageSource).Value);
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
            ShowError(_localization.OpenFailedTitle, ex);
        }
    }

    private void ShowError(string title, Exception exception)
    {
        _notificationService.ShowError(title, exception.Message);
    }
}
