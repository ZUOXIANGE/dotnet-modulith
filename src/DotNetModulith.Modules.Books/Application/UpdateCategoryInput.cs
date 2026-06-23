namespace DotNetModulith.Modules.Books.Application;

public sealed record UpdateCategoryInput(string Name, string Description, Guid? ParentId, int SortOrder);
