using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Modules.Users.Application;

/// <summary>
/// JWT 配置
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    [Required]
    public string Issuer { get; init; } = "dotnet-modulith";

    [Required]
    public string Audience { get; init; } = "dotnet-modulith-api";

    [Required]
    [MinLength(32)]
    public string SecretKey { get; init; } = "DotNetModulith-Replace-This-Key-2026";

    [Range(5, 1440)]
    public int AccessTokenLifetimeMinutes { get; init; } = 120;
}
