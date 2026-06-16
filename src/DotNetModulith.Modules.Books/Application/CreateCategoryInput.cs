namespace DotNetModulith.Modules.Books.Application;

public sealed record CreateCategoryInput(string Name, string Description, Guid? ParentId, int SortOrder);
