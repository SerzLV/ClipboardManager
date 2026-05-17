using ClipboardManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClipboardManager.Data;

public sealed class ClipboardDbContext : DbContext
{
    public const string DatabaseFileName = "clipboardDatabase.sqlite";

    public DbSet<FileInfoModel> Files => Set<FileInfoModel>();
    public DbSet<TextModel> Texts => Set<TextModel>();
    public DbSet<UrlModel> Urls => Set<UrlModel>();
    public DbSet<ImageModel> Images => Set<ImageModel>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={DatabaseFileName}");
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
        });

        modelBuilder.Entity<ImageModel>(entity =>
        {
            entity.ToTable("Images");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.ImageData).IsRequired();
            entity.Ignore(x => x.ImageSource);
        });

        modelBuilder.Entity<TextModel>(entity =>
        {
            entity.ToTable("Texts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Text).IsRequired();
        });

        modelBuilder.Entity<UrlModel>(entity =>
        {
            entity.ToTable("Urls");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Url).IsRequired();
            entity.Property(x => x.Title).IsRequired();
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.ImageUrl).IsRequired();
        });
    }
}
