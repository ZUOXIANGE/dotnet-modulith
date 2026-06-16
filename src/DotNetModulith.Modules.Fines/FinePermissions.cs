namespace DotNetModulith.Modules.Fines;

public static class FinePermissions
{
    public const string FinesView = "fines.view";
    public const string FinesManage = "fines.manage";

    public static readonly IReadOnlyList<string> All = [FinesView, FinesManage];
}
