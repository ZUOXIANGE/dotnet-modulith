using Microsoft.AspNetCore.Authorization;

namespace DotNetModulith.Modules.Users.Application;

/// <summary>
/// 权限要求
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}
