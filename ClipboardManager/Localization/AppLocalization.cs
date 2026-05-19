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
    public string SearchPlaceholder => GetString();
    public string FavoritesTab => GetString();
    public string FilesTab => GetString();
    public string TextTab => GetString();
    public string LinksTab => GetString();
    public string ImagesTab => GetString();
    public string CopyToolTip => GetString();
    public string OpenToolTip => GetString();
    public string DeleteToolTip => GetString();
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
    public string AddedToFavoritesStatus => GetString();
    public string RemovedFromFavoritesStatus => GetString();
    public string FileTypeLabel => GetString();
    public string TextTypeLabel => GetString();
    public string LinkTypeLabel => GetString();
    public string ImageTypeLabel => GetString();
    public string ItemTypeLabel => GetString();
    public string TextRecordLabel => GetString();
    public string FavoriteRecordLabel => GetString();
    public string ExportDialogTitle => GetString();
    public string ImportDialogTitle => GetString();
    public string BackupFilter => GetString();
    public string UnsupportedImportFormatMessage => GetString();
    public string ClipboardProcessingFailedTitle => GetString();
    public string DeleteFailedTitle => GetString();
    public string CopyFailedTitle => GetString();
    public string OpenFailedTitle => GetString();
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
    private const string ApplicationDataDirectoryName = "ClipboardManager";
    private const string SettingsFileName = "settings.json";

    public static AppLanguage Load()
    {
        try
        {
            var filePath = GetSettingsFilePath();
            if (!File.Exists(filePath))
            {
                return AppLanguage.Russian;
            }

            var document = JsonSerializer.Deserialize<LanguageSettingsDocument>(File.ReadAllText(filePath));
            return AppLanguageParser.TryParse(document?.Language, out var language)
                ? language
                : AppLanguage.Russian;
        }
        catch
        {
            return AppLanguage.Russian;
        }
    }

    public static void Save(AppLanguage language)
    {
        var directoryPath = GetSettingsDirectoryPath();
        Directory.CreateDirectory(directoryPath);

        var document = new LanguageSettingsDocument
        {
            Language = AppLanguageParser.ToCode(language)
        };
        File.WriteAllText(GetSettingsFilePath(), JsonSerializer.Serialize(document));
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

    private sealed class LanguageSettingsDocument
    {
        public string? Language { get; set; }
    }
}
