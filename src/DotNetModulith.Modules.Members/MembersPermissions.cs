namespace DotNetModulith.Modules.Members;

public static class MembersPermissions
{
    public const string MembersView = "members.view";
    public const string MembersCreate = "members.create";
    public const string MembersEdit = "members.edit";
    public const string MembersDelete = "members.delete";
    public const string MemberGroupsView = "members.groups.view";
    public const string MemberGroupsCreate = "members.groups.create";
    public const string MemberGroupsEdit = "members.groups.edit";
    public const string MemberGroupsDelete = "members.groups.delete";

    public static readonly IReadOnlyList<string> All =
    [
        MembersView, MembersCreate, MembersEdit, MembersDelete,
        MemberGroupsView, MemberGroupsCreate, MemberGroupsEdit, MemberGroupsDelete
    ];
}
