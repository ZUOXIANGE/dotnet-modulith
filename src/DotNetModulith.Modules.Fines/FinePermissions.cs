namespace DotNetModulith.Modules.Fines;

public static class FinePermissions
{
    public const string FinesView = "fines.view";
    public const string FinesCreate = "fines.create";
    public const string FinesPay = "fines.pay";
    public const string FinesWaive = "fines.waive";
    public const string FinesRulesView = "fines.rules.view";
    public const string FinesRulesManage = "fines.rules.manage";

    public static readonly IReadOnlyList<string> All =
    [
        FinesView, FinesCreate, FinesPay, FinesWaive, FinesRulesView, FinesRulesManage
    ];
}
