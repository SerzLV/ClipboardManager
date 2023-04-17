using ClipboardManager.Helper;
using Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

public class MainWindowViewModel : BaseViewModel
{
    private readonly List<FileInfoModel> _copiedFiles = new();
    private readonly List<TextModel> _copiedTexts = new();
    private readonly List<UrlModel> _copiedUrls = new();
    private readonly List<ImageModel> _copiedImages = new();
    private string imageName = string.Empty;
    private int trigger = 0;

    public MainWindowViewModel()
    {
        ClearCommand = new RelayCommand(ExecuteClearCommand);
        CopyCommand = new RelayCommand(ExecuteCopyCommand);
        DeleteCommand = new RelayCommand(ExecuteDeleteCommand);
        OpenLinkCommand = new RelayCommand(ExecuteOpenCommand);
    }

    public ObservableCollection<FileInfoModel> Files { get; } = new();
    public ObservableCollection<TextModel> Texts { get; } = new();
    public ObservableCollection<UrlModel> Urls { get; } = new();
    public ObservableCollection<ImageModel> Images { get; } = new();

    public ICommand ClearCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand OpenLinkCommand { get; }

    public void ClipboardContextChange(object sender, EventArgs e)
    {
        ProcessFileDropList();
        ProcessText();
        ProcessImage();
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

            if (_copiedFiles.Any(x => x.Name == fileName)) continue;

            var fileInfo = new FileInfoModel 
            { 
                FilePath = filePath, 
                Name = fileName,
                CopyCommand = this.CopyCommand,
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

        if (_copiedTexts.Any(x => x.Text == text) || _copiedUrls.Any(x => x.Url == text || x.Title == text)) return;

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
            if (_copiedUrls.Any(x => x.Url == url)) continue;

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
        if (_copiedImages.Any(x => x.Name == imageName))
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

    private void ExecuteDeleteCommand(object parameter) 
    {
        if (parameter is FileInfoModel fileInfo)
        {
            this.Files.Remove(fileInfo);
        }
        if (parameter is TextModel textInfo)
        {
            this.Texts.Remove(textInfo);
        }
        if (parameter is ImageModel imageInfo)
        {
            this.Images.Remove(imageInfo);
        }
        if (parameter is UrlModel urlInfo)
        {
            Urls.Add(urlInfo);
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
}
