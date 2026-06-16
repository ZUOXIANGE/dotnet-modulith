using DotNetModulith.Modules.Members.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotNetModulith.Modules.Members.Infrastructure;

public sealed class MembersDbContextFactory : IDesignTimeDbContextFactory<MembersDbContext>
{
    public MembersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MembersDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=modulith;Username=postgres;Password=postgres",
            npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(MembersDbContext).Assembly.FullName));

        return new MembersDbContext(optionsBuilder.Options);
    }
}
