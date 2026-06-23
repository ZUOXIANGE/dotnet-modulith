namespace DotNetModulith.Modules.Books.Application;

public sealed record CategoryDetails(
    Guid Id,
    string Name,
    string Description,
    Guid? ParentId,
    string? ParentName,
    int SortOrder,
    DateTimeOffset CreatedAt);
