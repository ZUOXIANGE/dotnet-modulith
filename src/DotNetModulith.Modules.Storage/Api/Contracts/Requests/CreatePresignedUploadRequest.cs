using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Modules.Storage.Api.Contracts.Requests;

public sealed class CreatePresignedUploadRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Purpose { get; set; } = string.Empty;
}
