using ClipboardManager.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace ClipboardManager.Data;

public sealed class ClipboardDbContext : DbContext
{
    public const string DatabaseFileName = "clipboardDatabase.sqlite";
    private const string ApplicationDataDirectoryName = "ClipboardManager";

    private static readonly Lazy<string> DatabasePath = new(GetDatabasePath);

    public DbSet<FileInfoModel> Files => Set<FileInfoModel>();
    public DbSet<TextModel> Texts => Set<TextModel>();
    public DbSet<UrlModel> Urls => Set<UrlModel>();
    public DbSet<ImageModel> Images => Set<ImageModel>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={DatabasePath.Value}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileInfoModel>(entity =>
        {
            entity.ToTable("Files");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.FilePath).IsRequired();
            entity.Property(x => x.IsPinned).HasDefaultValue(false);
            entity.HasIndex(x => x.FilePath);
        });

        modelBuilder.Entity<ImageModel>(entity =>
        {
            entity.ToTable("Images");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.ImageData).IsRequired();
            entity.Property(x => x.IsPinned).HasDefaultValue(false);
            entity.Ignore(x => x.ImageSource);
        });

        modelBuilder.Entity<TextModel>(entity =>
        {
            entity.ToTable("Texts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Text).IsRequired();
            entity.Property(x => x.IsPinned).HasDefaultValue(false);
        });

        modelBuilder.Entity<UrlModel>(entity =>
        {
            entity.ToTable("Urls");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Url).IsRequired();
            entity.Property(x => x.Title).IsRequired();
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.ImageUrl).IsRequired();
            entity.Property(x => x.IsPinned).HasDefaultValue(false);
            entity.HasIndex(x => x.Url);
        });
    }

    private static string GetDatabasePath()
    {
        var databaseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ApplicationDataDirectoryName);

        Directory.CreateDirectory(databaseDirectory);

        var databasePath = Path.Combine(databaseDirectory, DatabaseFileName);
        var legacyDatabasePath = Path.Combine(AppContext.BaseDirectory, DatabaseFileName);

        if (!File.Exists(databasePath) && File.Exists(legacyDatabasePath))
        {
            File.Copy(legacyDatabasePath, databasePath);
        }

        return databasePath;
    }
}
