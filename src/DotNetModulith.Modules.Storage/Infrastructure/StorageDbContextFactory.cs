using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotNetModulith.Modules.Storage.Infrastructure;

public sealed class StorageDbContextFactory : IDesignTimeDbContextFactory<StorageDbContext>
{
    public StorageDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<StorageDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=modulithdb;Username=postgres;Password=postgres");
        return new StorageDbContext(optionsBuilder.Options);
    }
}
