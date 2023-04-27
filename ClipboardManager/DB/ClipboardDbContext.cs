using Microsoft.EntityFrameworkCore;
using Models;

namespace ClipboardManager.DB
{
    public class ClipboardDbContext : DbContext
    {
        public DbSet<FileInfoModel> Files { get; set; }
        public DbSet<TextModel> Texts { get; set; }
        public DbSet<UrlModel> Urls { get; set; }
        public DbSet<ImageModel> Images { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=clipboardDatabase.sqlite");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileInfoModel>()
                .HasKey(x => x.Id);
            modelBuilder.Entity<FileInfoModel>()
                .Property(x => x.Name)
                .IsRequired();
            modelBuilder.Entity<FileInfoModel>()
                .Property(x => x.FilePath)
                .IsRequired();
            modelBuilder.Entity<FileInfoModel>()
                .Ignore(x => x.CopyCommand);
            modelBuilder.Entity<FileInfoModel>()
                .Ignore(x => x.OpenFileCommand);
            modelBuilder.Entity<FileInfoModel>()
                .Ignore(x => x.DeleteCommand);
            modelBuilder.Entity<FileInfoModel>()
                .ToTable("Files");

            modelBuilder.Entity<ImageModel>()
                .HasKey(x => x.Id);
            modelBuilder.Entity<ImageModel>()
                .Property(x => x.Name)
                .IsRequired();
            modelBuilder.Entity<ImageModel>()
                .Property(x => x.ImageData)
                .IsRequired();
            modelBuilder.Entity<ImageModel>()
                .Ignore(x => x.ImageSource);
            modelBuilder.Entity<ImageModel>()
                .Ignore(x => x.CopyCommand);
            modelBuilder.Entity<ImageModel>()
                .Ignore(x => x.DeleteCommand);
            modelBuilder.Entity<ImageModel>()
                .ToTable("Images");

            modelBuilder.Entity<TextModel>()
                .HasKey(x => x.Id);
            modelBuilder.Entity<TextModel>()
                .Property(x => x.Text)
                .IsRequired();
            modelBuilder.Entity<TextModel>()
                .Ignore(x => x.CopyCommand);
            modelBuilder.Entity<TextModel>()
                .Ignore(x => x.DeleteCommand);
            modelBuilder.Entity<TextModel>()
                .ToTable("Texts");

            modelBuilder.Entity<UrlModel>()
                .HasKey(x => x.Id);
            modelBuilder.Entity<UrlModel>()
                .Property(x => x.Url)
                .IsRequired();
            modelBuilder.Entity<UrlModel>()
                .Property(x => x.Title)
                .IsRequired();
            modelBuilder.Entity<UrlModel>()
                .Property(x => x.Description)
                .IsRequired();
            modelBuilder.Entity<UrlModel>()
                .Property(x => x.ImageUrl)
                .IsRequired();
            modelBuilder.Entity<UrlModel>()
                .Ignore(x => x.CopyCommand);
            modelBuilder.Entity<UrlModel>()
                .Ignore(x => x.OpenLinkCommand);
            modelBuilder.Entity<UrlModel>()
                .Ignore(x => x.DeleteCommand);
            modelBuilder.Entity<UrlModel>()
                .ToTable("Urls");
        }
    }
}
