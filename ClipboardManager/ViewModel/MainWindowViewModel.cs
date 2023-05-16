using ClipboardManager.Helper;
using ClipboardManager.ViewModel;
using Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

public class MainWindowViewModel : BaseViewModel
{
    private DatabaseMethodsViewModel db = new();
    private readonly List<FileInfoModel> _copiedFiles = new();
    private readonly List<TextModel> _copiedTexts = new();
    private readonly List<UrlModel> _copiedUrls = new();
    private readonly List<ImageModel> _copiedImages = new();
    private string imageName = string.Empty;
    private int trigger = 0;

    public MainWindowViewModel()
    {
        SaveCommand = new RelayCommand(ExecuteSaveCommand);
        ClearCommand = new RelayCommand(ExecuteClearCommand);
        CopyCommand = new RelayCommand(ExecuteCopyCommand);
        DeleteCommand = new RelayCommand(ExecuteDeleteCommand);
        OpenLinkCommand = new RelayCommand(ExecuteOpenCommand);
        OpenFileCommand = new RelayCommand(ExecuteOpenFileCommand);
        LoadDataFromDb();
    }

    public ObservableCollection<FileInfoModel> Files { get; set; } = new();
    public ObservableCollection<TextModel> Texts { get; set; } = new();
    public ObservableCollection<UrlModel> Urls { get; set; } = new();
    public ObservableCollection<ImageModel> Images { get; set; } = new();

    public ICommand SaveCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand OpenLinkCommand { get; }
    public ICommand OpenFileCommand { get; }

    public void ClipboardContextChange(object sender, EventArgs e)
    {
        ProcessFileDropList();
        ProcessText();
        ProcessImage();
    }

    private async void LoadDataFromDb()
    {
        var dbImages = await db.GetAllImage();
        foreach (var item in dbImages)
        {
            item.ImageSource = BitmapSourceExtensions.ByteArrayToBitmapSource(item.ImageData);
        }
        this.Images = dbImages;
        this.Texts = await db.GetAllText();
        this.Files = await db.GetAllFile();
        this.Urls = await db.GetAllUrl();
        AddCommands();
    }

    private void AssignCommands<T>(IEnumerable<T> items, ICommand copyCommand, ICommand deleteCommand, Action<T, ICommand> assignCommand)
    {
        PropertyInfo copyProp = typeof(T).GetProperty("CopyCommand");
        PropertyInfo deleteProp = typeof(T).GetProperty("DeleteCommand");

        foreach (var item in items)
        {
            copyProp.SetValue(item, copyCommand);
            deleteProp.SetValue(item, deleteCommand);
            assignCommand(item, copyCommand);
        }
    }

    private void AddCommands()
    {
        AssignCommands(Images, CopyCommand, DeleteCommand, (item, command) => { });
        AssignCommands(Texts, CopyCommand, DeleteCommand, (item, command) => { });
        AssignCommands(Files, CopyCommand, DeleteCommand, (item, command) => { item.OpenFileCommand = OpenFileCommand; });
        AssignCommands(Urls, CopyCommand, DeleteCommand, (item, command) => { item.OpenLinkCommand = OpenLinkCommand; });
    }

    private void ProcessFileDropList()
    {
        if (!Clipboard.ContainsFileDropList()) return;

        var files = Clipboard.GetFileDropList();
        foreach (var file in files)
        {
            if (!File.Exists(file)) continue;

            var fileName = Path.GetFileName(file);
            var filePath = Path.GetFullPath(file);

            if (Files.Any(x => x.Name == fileName)) continue;

            var fileInfo = new FileInfoModel
            {
                FilePath = filePath,
                Name = fileName,
                CopyCommand = this.CopyCommand,
                OpenFileCommand = this.OpenFileCommand,
                DeleteCommand = this.DeleteCommand
            };
            _copiedFiles.Add(fileInfo);
            AddToGroup(GroupTypes.File, fileInfo);
        }
    }

    private async void ProcessText()
    {
        if (!Clipboard.ContainsText()) return;

        var text = Clipboard.GetText();

        if (Texts.Any(x => x.Text == text) || Urls.Any(x => x.Url == text || x.Title == text)) return;

        var textInfo = new TextModel
        {
            Text = text,
            CopyCommand = this.CopyCommand,
            DeleteCommand = this.DeleteCommand
        };
        _copiedTexts.Add(textInfo);
        AddToGroup(GroupTypes.Text, textInfo);

        var urls = HtmlHelper.GetUrlsFromText(text);
        foreach (Match match in urls)
        {
            var url = match.Value;
            if (Urls.Any(x => x.Url == url)) continue;

            var linksInfo = await LinkInformationExtractor.GetLinkInformationAsync(url);

            linksInfo.CopyCommand = this.CopyCommand;
            linksInfo.DeleteCommand = this.DeleteCommand;
            linksInfo.OpenLinkCommand = this.OpenLinkCommand;

            _copiedUrls.Add(linksInfo);
            AddToGroup(GroupTypes.URL, linksInfo);
        }
    }

    private void ProcessImage()
    {
        if (!Clipboard.ContainsImage()) return;
        if (Images.Any(x => x.Name == imageName))
        {
            imageName = string.Empty;
            trigger = 1;
            return;
        }
        if (trigger == 0)
        {
            var image = Clipboard.GetImage();

            var imageInfo = new ImageModel
            {
                ImageSource = image,
                Name = DateTime.UtcNow.ToString("ddMMyyyy_hhmmss"),
                CopyCommand = this.CopyCommand,
                DeleteCommand = this.DeleteCommand
            };
            _copiedImages.Add(imageInfo);
            AddToGroup(GroupTypes.Image, imageInfo);
        }
        trigger = 0;
    }

    private void AddToGroup<T>(GroupTypes group, T item)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            switch (group)
            {
                case GroupTypes.File:
                    if (item is FileInfoModel fileInfo)
                    {
                        Files.Add(fileInfo);
                    }
                    break;
                case GroupTypes.Text:
                    if (item is TextModel textInfo)
                    {
                        Texts.Add(textInfo);
                    }
                    break;
                case GroupTypes.Image:
                    if (item is ImageModel imageInfo)
                    {
                        Images.Add(imageInfo);
                    }
                    break;
                case GroupTypes.URL:
                    if (item is UrlModel urlInfo)
                    {
                        Urls.Add(urlInfo);
                    }
                    break;
            }

            return Task.CompletedTask;
        });
    }

    private void ExecuteClearCommand(object parameter)
    {
        Files.Clear();
        Texts.Clear();
        Images.Clear();
        Urls.Clear();
        _copiedFiles.Clear();
        _copiedTexts.Clear();
        _copiedImages.Clear();
        _copiedUrls.Clear();
        OnPropertyChanged("Files");
        OnPropertyChanged("Texts");
        OnPropertyChanged("Images");
        OnPropertyChanged("Urls");
    }

    private async void ExecuteSaveCommand(object parameter)
    {
        await SaveDataToDb();
    }

    private async Task SaveDataToDb()
    {
        foreach (var fileInfo in Files.Where(x => x.Id == 0))
        {
            await db.SaveFile(fileInfo);
        }
        foreach (var textInfo in Texts.Where(x => x.Id == 0))
        {
            await db.SaveText(textInfo);
        }
        foreach (var imageInfo in Images.Where(x => x.Id == 0))
        {
            imageInfo.ImageData = BitmapSourceExtensions.ConvertBitmapSourceToByteArray(imageInfo.ImageSource, ".png");
            await db.SaveImage(imageInfo);
        }
        foreach (var urlInfo in Urls.Where(x => x.Id == 0))
        {
            await db.SaveUrl(urlInfo);
        }
    }

    private async void ExecuteDeleteCommand(object parameter)
    {
        if (parameter is FileInfoModel fileInfo)
        {
            this.Files.Remove(fileInfo);
            if (fileInfo.Id != 0)
            {
                await this.db.DeleteFile(fileInfo);
            }
        }
        if (parameter is TextModel textInfo)
        {
            this.Texts.Remove(textInfo);
            if (textInfo.Id != 0)
            {
                await this.db.DeleteText(textInfo);
            }
        }
        if (parameter is ImageModel imageInfo)
        {
            this.Images.Remove(imageInfo);
            if (imageInfo.Id != 0)
            {
                await this.db.DeleteImage(imageInfo);
            }
        }
        if (parameter is UrlModel urlInfo)
        {
            this.Urls.Remove(urlInfo);
            if (urlInfo.Id != 0)
            {
                await this.db.DeleteUrl(urlInfo);
            }
        }
    }

    private void ExecuteCopyCommand(object parameter)
    {
        if (parameter is FileInfoModel fileInfo)
        {
            Clipboard.SetText(fileInfo.FilePath);
        }
        if (parameter is TextModel textInfo)
        {
            Clipboard.SetText(textInfo.Text);
        }
        if (parameter is ImageModel imageInfo)
        {
            imageName = imageInfo.Name;
            Clipboard.SetImage(imageInfo.ImageSource);
        }
        if (parameter is UrlModel urlInfo)
        {
            Clipboard.SetText(urlInfo.Url);
        }
    }

    private void ExecuteOpenCommand(object parameter)
    {
        if (parameter is UrlModel urlInfo)
        {
            OpenLink.LinkOpenInDefaultBrowser(urlInfo.Url);
        }
    }

    private void ExecuteOpenFileCommand(object parameter)
    {
        if (parameter is FileInfoModel fileInfo)
        {
            OpenFile.FileOpen(fileInfo.FilePath);
        }
    }
}
