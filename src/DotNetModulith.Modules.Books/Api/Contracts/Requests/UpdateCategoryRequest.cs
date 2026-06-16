using System.ComponentModel.DataAnnotations;
using DotNetModulith.Abstractions.Validation.Attributes;

namespace DotNetModulith.Modules.Books.Api.Contracts.Requests;

public sealed record UpdateCategoryRequest
{
    [NotWhiteSpace]
    [StringLength(100)]
    public required string Name { get; init; }

    [StringLength(500)]
    public string Description { get; init; } = string.Empty;

    public Guid? ParentId { get; init; }

    public int SortOrder { get; init; }
}
