using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Reports.Api.Contracts.Responses;
using DotNetModulith.Modules.Reports.Api.Mappings;
using DotNetModulith.Modules.Reports.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Reports.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [Authorize(Policy = ReportPermissions.ReportsView)]
    [HttpGet("statistics")]
    public async Task<ApiResponse<BorrowingStatisticsResponse>> GetStatistics(CancellationToken ct)
    {
        var stats = await _reportService.GetBorrowingStatisticsAsync(ct);
        return ApiResponse.Success(stats.ToResponse());
    }

    [Authorize(Policy = ReportPermissions.ReportsView)]
    [HttpGet("popular-books")]
    public async Task<ApiResponse<PopularBookItemResponse[]>> GetPopularBooks([FromQuery] int topN = 10, CancellationToken ct = default)
    {
        var items = await _reportService.GetPopularBooksAsync(topN, ct);
        return ApiResponse.Success(items.Select(x => x.ToResponse()).ToArray());
    }

    [Authorize(Policy = ReportPermissions.ReportsView)]
    [HttpGet("overdue")]
    public async Task<ApiResponse<OverdueReportListResponse>> GetOverdueReport(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var items = await _reportService.GetOverdueReportAsync(page, pageSize, ct);
        var total = await _reportService.GetOverdueReportCountAsync(ct);

        return ApiResponse.Success(new OverdueReportListResponse(
            items.Select(x => x.ToResponse()).ToArray(),
            total,
            page,
            pageSize));
    }

    [Authorize(Policy = ReportPermissions.ReportsView)]
    [HttpGet("daily-trend")]
    public async Task<ApiResponse<DailyBorrowingItemResponse[]>> GetDailyTrend(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct = default)
    {
        var items = await _reportService.GetDailyBorrowingTrendAsync(from, to, ct);
        return ApiResponse.Success(items.Select(x => x.ToResponse()).ToArray());
    }
}
