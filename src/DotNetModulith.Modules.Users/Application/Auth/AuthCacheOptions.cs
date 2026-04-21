using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Modules.Users.Application;

public sealed class AuthCacheOptions
{
    public const string SectionName = "Authentication:Cache";

    [Range(1, 1440)]
    public int UserSnapshotTtlMinutes { get; init; } = 30;
}
