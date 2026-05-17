using System.Collections.ObjectModel;
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
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly HashSet<string> _knownImageHashes = [];

    private ClipboardContentSignature? _lastHandledClipboardSignature;
    private bool _isBusy;

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

        SaveCommand = new AsyncRelayCommand(_ => SaveAsync());
        ClearCommand = new AsyncRelayCommand(_ => ClearAsync(), _ => HasClipboardItems());
        CopyCommand = new RelayCommand(Copy, CanCopy);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);
        OpenLinkCommand = new RelayCommand(OpenLink, CanOpenLink);
        OpenFileCommand = new RelayCommand(OpenFile, CanOpenFile);
    }

    public ObservableCollection<FileInfoModel> Files { get; } = [];
    public ObservableCollection<TextModel> Texts { get; } = [];
    public ObservableCollection<UrlModel> Urls { get; } = [];
    public ObservableCollection<ImageModel> Images { get; } = [];

    public ICommand SaveCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand OpenLinkCommand { get; }
    public ICommand OpenFileCommand { get; }

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

            ProcessFileDropList();
            await ProcessTextAsync(cancellationToken);
            ProcessImage(signature);
            CommandManager.InvalidateRequerySuggested();
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

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _saveLock.WaitAsync(cancellationToken);

        try
        {
            await RunBusyAsync(async () =>
            {
                foreach (var image in Images.Where(x => x.Id == 0 && x.ImageSource is not null))
                {
                    image.ImageData = BitmapSourceExtensions.ConvertBitmapSourceToByteArray(image.ImageSource, ".png");
                }

                await _repository.SaveNewItemsAsync(
                    Files,
                    Texts,
                    Images,
                    Urls,
                    cancellationToken);
            });
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private void ProcessFileDropList()
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

            Files.Add(new FileInfoModel
            {
                FilePath = filePath,
                Name = Path.GetFileName(filePath)
            });
        }
    }

    private async Task ProcessTextAsync(CancellationToken cancellationToken)
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

        Texts.Add(new TextModel { Text = text });

        foreach (Match match in UrlRegex.Matches(text))
        {
            var url = match.Value.TrimEnd('.', ',', ';', ')', ']');
            if (Urls.Any(x => string.Equals(x.Url, url, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var metadata = await _linkMetadataService.GetMetadataAsync(url, cancellationToken);
            if (metadata is not null && !Urls.Any(x => string.Equals(x.Url, metadata.Url, StringComparison.OrdinalIgnoreCase)))
            {
                Urls.Add(metadata);
            }
        }
    }

    private void ProcessImage(ClipboardContentSignature currentSignature)
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

        Images.Add(new ImageModel
        {
            ImageSource = image,
            Name = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture)
        });
    }

    private async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await RunBusyAsync(async () =>
        {
            await _repository.ClearAsync(cancellationToken);
            Files.Clear();
            Texts.Clear();
            Images.Clear();
            Urls.Clear();
            _knownImageHashes.Clear();
            _lastHandledClipboardSignature = null;
            _clipboardChangeSuppressor.Clear();
            CommandManager.InvalidateRequerySuggested();
        });
    }

    private async Task DeleteAsync(object? parameter)
    {
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
        }
    }

    private void OpenFile(object? parameter)
    {
        if (parameter is FileInfoModel file)
        {
            TryLaunch(() => _shellLauncher.OpenFile(file.FilePath));
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
        return Files.Count > 0 || Texts.Count > 0 || Images.Count > 0 || Urls.Count > 0;
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
