using DotNetModulith.Modules.Reports.Api.Contracts.Responses;
using DotNetModulith.Modules.Reports.Application;
using Riok.Mapperly.Abstractions;

namespace DotNetModulith.Modules.Reports.Api.Mappings;

[Mapper]
public static partial class ReportResponseMapper
{
    public static partial BorrowingStatisticsResponse ToResponse(this BorrowingStatistics source);

    public static partial PopularBookItemResponse ToResponse(this PopularBookItem source);

    public static partial OverdueReportItemResponse ToResponse(this OverdueReportItem source);

    public static partial DailyBorrowingItemResponse ToResponse(this DailyBorrowingItem source);
}
