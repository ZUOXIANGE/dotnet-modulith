using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Modules.Borrowing.Application.Jobs;

public sealed class OverdueDetectionOptions
{
    public const string SectionName = "OverdueDetection";

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;

    public decimal FinePerDay { get; set; } = 1.00m;
}
