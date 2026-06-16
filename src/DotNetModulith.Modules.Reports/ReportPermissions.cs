namespace DotNetModulith.Modules.Reports;

public static class ReportPermissions
{
    public const string ReportsView = "reports.view";

    public static readonly IReadOnlyList<string> All = [ReportsView];
}
