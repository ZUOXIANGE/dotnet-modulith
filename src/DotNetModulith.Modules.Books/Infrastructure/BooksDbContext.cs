using DotNetModulith.Modules.Books.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Books.Infrastructure;

public sealed class BooksDbContext : DbContext
{
    public DbSet<BookEntity> Books => Set<BookEntity>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();

    public BooksDbContext(DbContextOptions<BooksDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("books");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BooksDbContext).Assembly);
    }
}
