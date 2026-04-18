using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// OpenObserve 导出配置选项
/// </summary>
public sealed class OpenObserveOptions
{
    public const string SectionName = "OpenObserve";

    public bool Enabled { get; set; }

    [Required]
    public string Endpoint { get; set; } = "http://localhost:5080";

    [Required]
    public string Organization { get; set; } = "default";

    [Required]
    public string StreamName { get; set; } = "dotnet-modulith";

    [Required]
    public string UserEmail { get; set; } = string.Empty;

    [Required]
    public string UserPassword { get; set; } = string.Empty;
}
