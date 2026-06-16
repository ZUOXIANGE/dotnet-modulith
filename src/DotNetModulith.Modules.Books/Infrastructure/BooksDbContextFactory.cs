using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotNetModulith.Modules.Books.Infrastructure;

public sealed class BooksDbContextFactory : IDesignTimeDbContextFactory<BooksDbContext>
{
    public BooksDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BooksDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=modulithdb;Username=postgres;Password=postgres");
        return new BooksDbContext(optionsBuilder.Options);
    }
}
