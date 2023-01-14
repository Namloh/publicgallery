using FileUpload.Models;
using galerie.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace galerie.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }
        
        public DbSet<StoredFile> Files { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<Gallery> Galleries { get; set; }
        public DbSet<ThumbnailBlob> Thumbnails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Thumbnail>().HasKey(t => new { t.FileId, t.Type });
            


        }
    }
}