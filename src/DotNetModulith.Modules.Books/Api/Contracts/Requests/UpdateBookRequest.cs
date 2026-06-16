using System.ComponentModel.DataAnnotations;
using DotNetModulith.Abstractions.Validation.Attributes;

namespace DotNetModulith.Modules.Books.Api.Contracts.Requests;

public sealed record UpdateBookRequest
{
    [NotWhiteSpace]
    [StringLength(20)]
    public required string Isbn { get; init; }

    [NotWhiteSpace]
    [StringLength(200)]
    public required string Title { get; init; }

    [NotWhiteSpace]
    [StringLength(200)]
    public required string Author { get; init; }

    [NotWhiteSpace]
    [StringLength(200)]
    public required string Publisher { get; init; }

    [Required]
    public required DateOnly PublishDate { get; init; }

    [StringLength(2000)]
    public string Description { get; init; } = string.Empty;

    [Required]
    public required Guid CategoryId { get; init; }

    [Range(1, 9999)]
    public int TotalCopies { get; init; } = 1;

    [StringLength(500)]
    public string CoverImageUrl { get; init; } = string.Empty;
}
