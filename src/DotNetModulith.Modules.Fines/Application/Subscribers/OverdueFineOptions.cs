using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Modules.Fines.Application.Subscribers;

public sealed class OverdueFineOptions
{
    public const string SectionName = "OverdueFine";

    [Range(0, 100)]
    public decimal FinePerDay { get; set; } = 1.00m;
}
