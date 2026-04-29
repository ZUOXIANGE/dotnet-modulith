using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Modules.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required]
    public string Endpoint { get; set; } = "http://localhost:9000";

    [Required]
    public string AccessKey { get; set; } = "rustfsadmin";

    [Required]
    public string SecretKey { get; set; } = "rustfsadmin";

    [Required]
    public string BucketName { get; set; } = "modulith-files";

    public bool ForcePathStyle { get; set; } = true;

    public bool UseSsl { get; set; } = false;

    [Range(60, 86400)]
    public int PresignedUrlExpireSeconds { get; set; } = 900;
}
