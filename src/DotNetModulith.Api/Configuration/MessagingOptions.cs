using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Api.Configuration;

public sealed class CapMessagingOptions
{
    public const string SectionName = "Cap";

    [Range(0, 100)]
    public int FailedRetryCount { get; set; } = 5;

    [Range(1, 3600)]
    public int FailedRetryInterval { get; set; } = 60;

    [Required]
    public string DefaultGroupName { get; set; } = "modulith";

    [Required]
    public string Version { get; set; } = "v1";
}

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    [Required]
    public string HostName { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 5672;

    [Required]
    public string UserName { get; set; } = "guest";

    [Required]
    public string Password { get; set; } = "guest";

    [Required]
    public string VirtualHost { get; set; } = "/";
}
