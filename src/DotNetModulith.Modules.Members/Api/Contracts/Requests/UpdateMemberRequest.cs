using System.ComponentModel.DataAnnotations;
using DotNetModulith.Abstractions.Validation.Attributes;

namespace DotNetModulith.Modules.Members.Api.Contracts.Requests;

public sealed record UpdateMemberRequest
{
    [NotWhiteSpace]
    [StringLength(100)]
    public required string Name { get; init; }

    [NotWhiteSpace]
    [StringLength(20)]
    public required string Phone { get; init; }

    [NotWhiteSpace]
    [EmailAddress]
    [StringLength(200)]
    public required string Email { get; init; }

    [StringLength(500)]
    public string Address { get; init; } = string.Empty;

    [NotWhiteSpace]
    public required string MembershipType { get; init; }

    public DateOnly? ExpiryDate { get; init; }
}
