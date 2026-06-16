using DotNetModulith.Modules.Members.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Members.Infrastructure;

public sealed class MembersDbContext : DbContext
{
    public DbSet<MemberEntity> Members => Set<MemberEntity>();

    public MembersDbContext(DbContextOptions<MembersDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("members");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MembersDbContext).Assembly);
    }
}
