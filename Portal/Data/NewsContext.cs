using Microsoft.EntityFrameworkCore;
using Portal.Models;
using Portal.Models.Birthday;
using Portal.Models.Election;
using Portal.Models.Menu;

namespace Portal.Data
{
    public class NewsContext: DbContext
    {
        public NewsContext(DbContextOptions<NewsContext> options) : base(options)
        {
        }

        public DbSet<Article> Articles { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Birthday> Birthdays { get; set; }
        public DbSet<ElectionArticle> ElectionArticles { get; set; }
        public DbSet<ElectionImage> ElectionImages { get; set; }
        public DbSet<Menu> Menus { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Article>().ToTable("Articles");
            modelBuilder.Entity<Image>().ToTable("Images");
            modelBuilder.Entity<ElectionArticle>().ToTable("ElectionArticles");
            modelBuilder.Entity<ElectionImage>().ToTable("ElectionImages");
            modelBuilder.Entity<Birthday>().ToTable("Birthdays");
            modelBuilder.Entity<Menu>().ToTable("Menus");
        }
    }
}
