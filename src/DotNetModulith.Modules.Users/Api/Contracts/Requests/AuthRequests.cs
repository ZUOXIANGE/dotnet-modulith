using System.ComponentModel.DataAnnotations;
using DotNetModulith.Abstractions.Validation.Attributes;

namespace DotNetModulith.Modules.Users.Api.Contracts.Requests;

/// <summary>
/// 登录请求
/// </summary>
public sealed record LoginRequest
{
    [NotWhiteSpace]
    [StringLength(100)]
    public required string UserName { get; init; }

    [NotWhiteSpace]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }
}
