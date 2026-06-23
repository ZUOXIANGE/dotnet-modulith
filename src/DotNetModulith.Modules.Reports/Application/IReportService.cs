namespace DotNetModulith.Modules.Reports.Application;

public interface IReportService
{
    Task<BorrowingStatistics> GetBorrowingStatisticsAsync(CancellationToken ct);
    Task<PopularBookItem[]> GetPopularBooksAsync(int topN, CancellationToken ct);
    Task<OverdueReportItem[]> GetOverdueReportAsync(int page, int pageSize, CancellationToken ct);
    Task<int> GetOverdueReportCountAsync(CancellationToken ct);
    Task<DailyBorrowingItem[]> GetDailyBorrowingTrendAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
