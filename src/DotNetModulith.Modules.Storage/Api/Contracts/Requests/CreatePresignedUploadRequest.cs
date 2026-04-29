using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Modules.Storage.Api.Contracts.Requests;

public sealed class CreatePresignedUploadRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string FileName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ObjectKey { get; set; }
}
