namespace DotNetModulith.Modules.Books.Application;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDetails>> GetCategoriesAsync(CancellationToken ct);
    Task<CategoryDetails?> GetCategoryByIdAsync(Guid id, CancellationToken ct);
    Task<CategoryDetails> CreateCategoryAsync(CreateCategoryInput input, CancellationToken ct);
    Task<CategoryDetails> UpdateCategoryAsync(Guid id, UpdateCategoryInput input, CancellationToken ct);
    Task DeleteCategoryAsync(Guid id, CancellationToken ct);
}
