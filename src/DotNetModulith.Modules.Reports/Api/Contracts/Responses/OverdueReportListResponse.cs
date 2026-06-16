namespace DotNetModulith.Modules.Reports.Api.Contracts.Responses;

public sealed record OverdueReportListResponse(
    OverdueReportItemResponse[] Items,
    int Total,
    int Page,
    int PageSize);
