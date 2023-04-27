using ClipboardManager.DB;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ClipboardManager.ViewModel
{
    public interface IDatabaseMethodsViewModel
    {
        Task SaveFile(FileInfoModel fileInfoModel);
        Task SaveImage(ImageModel imageModel);
        Task SaveText(TextModel textModel);
        Task SaveUrl(UrlModel urlModel);
        Task<ObservableCollection<FileInfoModel>> GetAllFile();
        Task<ObservableCollection<ImageModel>> GetAllImage();
        Task<ObservableCollection<TextModel>> GetAllText();
        Task<ObservableCollection<UrlModel>> GetAllUrl();
        Task DeleteFile(FileInfoModel fileInfoModel);
        Task DeleteImage(ImageModel imageModel);
        Task DeleteText(TextModel textModel);
        Task DeleteUrl(UrlModel urlModel);
    }

    public class DatabaseMethodsViewModel : IDatabaseMethodsViewModel
    {
        private readonly ClipboardDbContext _context = new ClipboardDbContext();

        public DatabaseMethodsViewModel()
        {
            _context.Database.EnsureCreated();
        }

        public async Task SaveFile(FileInfoModel fileInfoModel)
        {
            await _context.Files.AddAsync(fileInfoModel);
            await _context.SaveChangesAsync();
        }

        public async Task SaveImage(ImageModel imageModel)
        {
            await _context.Images.AddAsync(imageModel);
            await _context.SaveChangesAsync();
        }

        public async Task SaveText(TextModel textModel)
        {
            await _context.Texts.AddAsync(textModel);
            await _context.SaveChangesAsync();
        }

        public async Task SaveUrl(UrlModel urlModel)
        {
            await _context.Urls.AddAsync(urlModel);
            await _context.SaveChangesAsync();
        }

        public async Task<ObservableCollection<FileInfoModel>> GetAllFile()
        {
            var files = await _context.Files.ToListAsync();
            return new ObservableCollection<FileInfoModel>(files);
        }

        public async Task<ObservableCollection<ImageModel>> GetAllImage()
        {
            var images = await _context.Images.ToListAsync();
            return new ObservableCollection<ImageModel>(images);
        }

        public async Task<ObservableCollection<TextModel>> GetAllText()
        {
            var texts = await _context.Texts.ToListAsync();
            return new ObservableCollection<TextModel>(texts);
        }

        public async Task<ObservableCollection<UrlModel>> GetAllUrl()
        {
            var urls = await _context.Urls.ToListAsync();
            return new ObservableCollection<UrlModel>(urls);
        }

        public async Task DeleteFile(FileInfoModel fileInfoModel)
        {
            _context.Files.Remove(fileInfoModel);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteImage(ImageModel imageModel)
        {
            _context.Images.Remove(imageModel);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteText(TextModel textModel)
        {
            _context.Texts.Remove(textModel);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteUrl(UrlModel urlModel)
        {
            _context.Urls.Remove(urlModel);
            await _context.SaveChangesAsync();
        }
    }
}
