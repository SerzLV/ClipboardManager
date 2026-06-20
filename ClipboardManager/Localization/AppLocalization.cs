using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ClipboardManager.Localization;

public enum AppLanguage
{
    English,
    Russian
}

public sealed class LocalizationService : INotifyPropertyChanged
{
    private const string ResourceBaseName = "ClipboardManager.Resources.Strings";
    private static readonly ResourceManager Strings = new(ResourceBaseName, Assembly.GetExecutingAssembly());

    private AppLanguage _language;
    private CultureInfo _culture;

    public LocalizationService(AppLanguage language)
    {
        _language = language;
        _culture = GetCulture(language);
        ApplyCulture(_culture);
    }

    public AppLanguage Language => _language;

    public string WindowTitle => GetString();
    public string AppTitle => GetString();
    public string LanguageToolTip => GetString();
    public string SettingsToolTip => GetString();
    public string SettingsTitle => GetString();
    public string SettingsShellTitle => GetString();
    public string SettingsHistoryTitle => GetString();
    public string SettingsSecretsTitle => GetString();
    public string SettingsShortcutsTitle => GetString();
    public string MinimizeToTraySetting => GetString();
    public string StartWithWindowsSetting => GetString();
    public string GlobalHotKeySetting => GetString();
    public string HistoryBatchSizeSetting => GetString();
    public string HistoryBatchSizeDescription => GetString();
    public string ClearHistorySetting => GetString();
    public string ClearHistoryDescription => GetString();
    public string ClearHistoryButton => GetString();
    public string LinkRefreshIntervalSetting => GetString();
    public string LinkRefreshIntervalDescription => GetString();
    public string ClearLinkPreviewCacheSetting => GetString();
    public string ClearLinkPreviewCacheDescription => GetString();
    public string ClearLinkPreviewCacheButton => GetString();
    public string ClearCopiedSecretsSetting => GetString();
    public string ClearCopiedSecretsDescription => GetString();
    public string LinkRefreshNeverOption => GetString();
    public string LinkRefresh7DaysOption => GetString();
    public string LinkRefresh30DaysOption => GetString();
    public string TrayOpenMenuItem => GetString();
    public string TrayExitMenuItem => GetString();
    public string GlobalHotKeyUnavailableTitle => GetString();
    public string StartupSettingFailedTitle => GetString();
    public string SearchPlaceholder => GetString();
    public string FavoritesTab => GetString();
    public string FilesTab => GetString();
    public string TextTab => GetString();
    public string LinksTab => GetString();
    public string ImagesTab => GetString();
    public string SecretsTab => GetString();
    public string CopyToolTip => GetString();
    public string OpenToolTip => GetString();
    public string DeleteToolTip => GetString();
    public string SaveImageToolTip => GetString();
    public string SaveAsSecretToolTip => GetString();
    public string ShowSecretToolTip => GetString();
    public string HideSecretToolTip => GetString();
    public string ImportToolTip => GetString();
    public string ExportToolTip => GetString();
    public string ClearToolTip => GetString();
    public string ClearSearchToolTip => GetString();
    public string ZoomOutToolTip => GetString();
    public string ResetZoomToolTip => GetString();
    public string ZoomInToolTip => GetString();
    public string CloseToolTip => GetString();
    public string AddToFavoritesToolTip => GetString();
    public string RemoveFromFavoritesToolTip => GetString();
    public string MonitoringActiveStatus => GetString();
    public string LoadingHistoryStatus => GetString();
    public string LoadingMoreHistoryStatus => GetString();
    public string LoadingLinksStatus => GetString();
    public string NoNewEntriesText => GetString();
    public string HistoryEmptyStatus => GetString();
    public string ExportingHistoryStatus => GetString();
    public string ImportingHistoryStatus => GetString();
    public string HistoryClearedStatus => GetString();
    public string ItemDeletedStatus => GetString();
    public string CopiedStatus => GetString();
    public string LinkOpenedStatus => GetString();
    public string FileOpenedStatus => GetString();
    public string ImageOpenedStatus => GetString();
    public string ImageSavedStatus => GetString();
    public string SecretSavedStatus => GetString();
    public string SecretRevealedStatus => GetString();
    public string SecretHiddenStatus => GetString();
    public string SecretAccessDeniedStatus => GetString();
    public string AddedToFavoritesStatus => GetString();
    public string RemovedFromFavoritesStatus => GetString();
    public string FileTypeLabel => GetString();
    public string TextTypeLabel => GetString();
    public string LinkTypeLabel => GetString();
    public string ImageTypeLabel => GetString();
    public string SecretTypeLabel => GetString();
    public string ItemTypeLabel => GetString();
    public string TextRecordLabel => GetString();
    public string SecretRecordLabel => GetString();
    public string FavoriteRecordLabel => GetString();
    public string SecretNameDialogTitle => GetString();
    public string SecretNameDialogDescription => GetString();
    public string SecretNameLabel => GetString();
    public string SaveSecretButton => GetString();
    public string CancelButton => GetString();
    public string SecretNameRequiredMessage => GetString();
    public string WindowsVerificationUnavailableMessage => GetString();
    public string WindowsPasswordPromptTitle => GetString();
    public string ExportDialogTitle => GetString();
    public string ImportDialogTitle => GetString();
    public string SaveImageDialogTitle => GetString();
    public string BackupFilter => GetString();
    public string ImageSaveFilter => GetString();
    public string UnsupportedImportFormatMessage => GetString();
    public string ClipboardProcessingFailedTitle => GetString();
    public string DeleteFailedTitle => GetString();
    public string CopyFailedTitle => GetString();
    public string OpenFailedTitle => GetString();
    public string SaveImageFailedTitle => GetString();
    public string SecretVerificationFailedTitle => GetString();
    public string PinUpdateFailedTitle => GetString();
    public string AutoSaveFailedTitle => GetString();
    public string OperationFailedTitle => GetString();

    public void UseLanguage(AppLanguage language)
    {
        if (_language == language)
        {
            return;
        }

        _language = language;
        _culture = GetCulture(language);
        ApplyCulture(_culture);
        OnPropertyChanged(string.Empty);
    }

    public string LastActivityText(DateTime activityAt)
    {
        return Format("LastActivityTextFormat", activityAt);
    }

    public string SearchResultText(int filteredCount, int totalCount)
    {
        return Format("SearchResultTextFormat", filteredCount, totalCount);
    }

    public string FavoritesEmptyTitle(bool isSearchActive)
    {
        return GetString(isSearchActive ? "FavoritesEmptySearchTitle" : "FavoritesEmptyTitle");
    }

    public string FavoritesEmptyDescription(bool isSearchActive)
    {
        return GetString(isSearchActive ? "FavoritesEmptySearchDescription" : "FavoritesEmptyDescription");
    }

    public string FilesEmptyTitle(bool isSearchActive)
    {
        return GetString(isSearchActive ? "FilesEmptySearchTitle" : "FilesEmptyTitle");
    }

    public string FilesEmptyDescription(bool isSearchActive)
    {
        return GetString(isSearchActive ? "FilesEmptySearchDescription" : "FilesEmptyDescription");
    }

    public string TextsEmptyTitle(bool isSearchActive)
    {
        return GetString(isSearchActive ? "TextsEmptySearchTitle" : "TextsEmptyTitle");
    }

    public string TextsEmptyDescription(bool isSearchActive)
    {
        return GetString(isSearchActive ? "TextsEmptySearchDescription" : "TextsEmptyDescription");
    }

    public string UrlsEmptyTitle(bool isSearchActive)
    {
        return GetString(isSearchActive ? "UrlsEmptySearchTitle" : "UrlsEmptyTitle");
    }

    public string UrlsEmptyDescription(bool isSearchActive)
    {
        return GetString(isSearchActive ? "UrlsEmptySearchDescription" : "UrlsEmptyDescription");
    }

    public string ImagesEmptyTitle(bool isSearchActive)
    {
        return GetString(isSearchActive ? "ImagesEmptySearchTitle" : "ImagesEmptyTitle");
    }

    public string ImagesEmptyDescription(bool isSearchActive)
    {
        return GetString(isSearchActive ? "ImagesEmptySearchDescription" : "ImagesEmptyDescription");
    }

    public string SecretsEmptyTitle(bool isSearchActive)
    {
        return GetString(isSearchActive ? "SecretsEmptySearchTitle" : "SecretsEmptyTitle");
    }

    public string SecretsEmptyDescription(bool isSearchActive)
    {
        return GetString(isSearchActive ? "SecretsEmptySearchDescription" : "SecretsEmptyDescription");
    }

    public string HistoryLoadedStatus(int count)
    {
        return Format("HistoryLoadedStatusFormat", FormatItemCount(count));
    }

    public string AddedNewItemsStatus(int count)
    {
        return Format("AddedNewItemsStatusFormat", count);
    }

    public string HistoryExportedStatus(int count)
    {
        return Format("HistoryExportedStatusFormat", FormatItemCount(count));
    }

    public string ImportedWithPinnedStatus(int addedCount, int pinnedUpdatedCount)
    {
        return Format("ImportedWithPinnedStatusFormat", addedCount, pinnedUpdatedCount);
    }

    public string ImportedItemsStatus(int count)
    {
        return Format("ImportedItemsStatusFormat", count);
    }

    public string AutoSavedItemsStatus(int count)
    {
        return Format("AutoSavedItemsStatusFormat", count);
    }

    public string LinkPreviewCacheClearedStatus(int count)
    {
        return Format("LinkPreviewCacheClearedStatusFormat", count);
    }

    public string LinkRefreshIntervalOptionText(int days)
    {
        return days switch
        {
            AppSettings.LinkRefreshNeverDays => LinkRefreshNeverOption,
            AppSettings.LinkRefreshWeeklyDays => LinkRefresh7DaysOption,
            AppSettings.LinkRefreshMonthlyDays => LinkRefresh30DaysOption,
            _ => LinkRefresh30DaysOption
        };
    }

    public string SecretCopiedStatus(int clipboardSeconds, int trustSeconds)
    {
        return Format("SecretCopiedStatusFormat", clipboardSeconds, trustSeconds);
    }

    public string SecretCopiedWithoutAutoClearStatus(int trustSeconds)
    {
        return Format("SecretCopiedWithoutAutoClearStatusFormat", trustSeconds);
    }

    public string SecretRevealVerificationMessage(string secretName)
    {
        return Format("SecretRevealVerificationMessageFormat", secretName);
    }

    public string SecretCopyVerificationMessage(string secretName)
    {
        return Format("SecretCopyVerificationMessageFormat", secretName);
    }

    public string GlobalHotKeyUnavailableMessage(string hotKey)
    {
        return Format("GlobalHotKeyUnavailableMessageFormat", hotKey);
    }

    public string StartupSettingFailedMessage(bool enabled)
    {
        return Format("StartupSettingFailedMessageFormat", enabled ? GetString("EnabledState") : GetString("DisabledState"));
    }

    private string FormatItemCount(int count)
    {
        return string.Format(_culture, "{0} {1}", count, GetString(GetItemCountKey(count)));
    }

    private string GetItemCountKey(int count)
    {
        if (_language == AppLanguage.English)
        {
            return count == 1 ? "ItemCountOne" : "ItemCountMany";
        }

        var value = Math.Abs(count);
        var mod100 = value % 100;
        if (mod100 is >= 11 and <= 14)
        {
            return "ItemCountMany";
        }

        return (value % 10) switch
        {
            1 => "ItemCountOne",
            >= 2 and <= 4 => "ItemCountFew",
            _ => "ItemCountMany"
        };
    }

    private string Format(string key, params object[] args)
    {
        return string.Format(_culture, GetString(key), args);
    }

    private string GetString([CallerMemberName] string? key = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Strings.GetString(key, _culture) ?? $"!{key}!";
    }

    private static CultureInfo GetCulture(AppLanguage language)
    {
        return CultureInfo.GetCultureInfo(language == AppLanguage.English ? "en-US" : "ru-RU");
    }

    private static void ApplyCulture(CultureInfo culture)
    {
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public static class AppLanguageParser
{
    public static string ToCode(AppLanguage language)
    {
        return language == AppLanguage.English ? "en" : "ru";
    }

    public static bool TryParse(string? code, out AppLanguage language)
    {
        switch (code?.Trim().ToLowerInvariant())
        {
            case "en":
            case "eng":
            case "english":
                language = AppLanguage.English;
                return true;
            case "ru":
            case "rus":
            case "russian":
            case "рус":
                language = AppLanguage.Russian;
                return true;
            default:
                language = AppLanguage.Russian;
                return false;
        }
    }
}

public static class LanguagePreferenceStore
{
    public static AppLanguage Load()
    {
        return AppSettingsStore.Load().Language;
    }

    public static void Save(AppLanguage language)
    {
        var settings = AppSettingsStore.Load();
        settings.Language = language;
        AppSettingsStore.Save(settings);
    }
}

public sealed class AppSettings
{
    public const int DefaultHistoryBatchSize = 100;
    public const int MinHistoryBatchSize = 25;
    public const int MaxHistoryBatchSize = 1000;
    public const int LinkRefreshNeverDays = 0;
    public const int LinkRefreshWeeklyDays = 7;
    public const int LinkRefreshMonthlyDays = 30;
    public const int DefaultLinkRefreshIntervalDays = LinkRefreshMonthlyDays;

    public AppLanguage Language { get; set; } = AppLanguage.Russian;
    public bool MinimizeToTray { get; set; }
    public bool StartWithWindows { get; set; }
    public bool GlobalHotKeyEnabled { get; set; }
    public bool ClearCopiedSecretsFromClipboard { get; set; } = true;
    public int HistoryBatchSize { get; set; } = DefaultHistoryBatchSize;
    public int LinkRefreshIntervalDays { get; set; } = DefaultLinkRefreshIntervalDays;

    public static int NormalizeHistoryBatchSize(int value)
    {
        return Math.Clamp(value, MinHistoryBatchSize, MaxHistoryBatchSize);
    }

    public static int NormalizeLinkRefreshIntervalDays(int value)
    {
        return value switch
        {
            LinkRefreshNeverDays or LinkRefreshWeeklyDays or LinkRefreshMonthlyDays => value,
            _ => DefaultLinkRefreshIntervalDays
        };
    }
}

public static class AppSettingsStore
{
    private const string ApplicationDataDirectoryName = "ClipboardManager";
    private const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static AppSettings Load()
    {
        try
        {
            var filePath = GetSettingsFilePath();
            if (!File.Exists(filePath))
            {
                return new AppSettings();
            }

            var document = JsonSerializer.Deserialize<AppSettingsDocument>(File.ReadAllText(filePath));
            AppLanguageParser.TryParse(document?.Language, out var language);

            return new AppSettings
            {
                Language = language,
                MinimizeToTray = document?.MinimizeToTray ?? false,
                StartWithWindows = document?.StartWithWindows ?? false,
                GlobalHotKeyEnabled = document?.GlobalHotKeyEnabled ?? false,
                ClearCopiedSecretsFromClipboard = document?.ClearCopiedSecretsFromClipboard ?? true,
                HistoryBatchSize = AppSettings.NormalizeHistoryBatchSize(
                    document?.HistoryBatchSize ?? AppSettings.DefaultHistoryBatchSize),
                LinkRefreshIntervalDays = AppSettings.NormalizeLinkRefreshIntervalDays(
                    document?.LinkRefreshIntervalDays ?? AppSettings.DefaultLinkRefreshIntervalDays)
            };
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        var directoryPath = GetSettingsDirectoryPath();
        Directory.CreateDirectory(directoryPath);

        var document = new AppSettingsDocument
        {
            Language = AppLanguageParser.ToCode(settings.Language),
            MinimizeToTray = settings.MinimizeToTray,
            StartWithWindows = settings.StartWithWindows,
            GlobalHotKeyEnabled = settings.GlobalHotKeyEnabled,
            ClearCopiedSecretsFromClipboard = settings.ClearCopiedSecretsFromClipboard,
            HistoryBatchSize = AppSettings.NormalizeHistoryBatchSize(settings.HistoryBatchSize),
            LinkRefreshIntervalDays = AppSettings.NormalizeLinkRefreshIntervalDays(settings.LinkRefreshIntervalDays)
        };
        WriteAllTextAtomic(GetSettingsFilePath(), JsonSerializer.Serialize(document, SerializerOptions));
    }

    private static string GetSettingsDirectoryPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ApplicationDataDirectoryName);
    }

    private static string GetSettingsFilePath()
    {
        return Path.Combine(GetSettingsDirectoryPath(), SettingsFileName);
    }

    private static void WriteAllTextAtomic(string filePath, string contents)
    {
        var temporaryFilePath = $"{filePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.WriteAllText(temporaryFilePath, contents);
            File.Move(temporaryFilePath, filePath, true);
        }
        finally
        {
            TryDeleteFile(temporaryFilePath);
        }
    }

    private static void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
        }
    }

    private sealed class AppSettingsDocument
    {
        public string? Language { get; set; }
        public bool? MinimizeToTray { get; set; }
        public bool? StartWithWindows { get; set; }
        public bool? GlobalHotKeyEnabled { get; set; }
        public bool? ClearCopiedSecretsFromClipboard { get; set; }
        public int? HistoryBatchSize { get; set; }
        public int? LinkRefreshIntervalDays { get; set; }
    }
}
