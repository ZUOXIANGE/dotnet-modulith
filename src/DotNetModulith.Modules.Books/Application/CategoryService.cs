using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Books.Domain;
using DotNetModulith.Modules.Books.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Books.Application;

internal sealed class CategoryService : ICategoryService
{
    private readonly BooksDbContext _dbContext;

    public CategoryService(BooksDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CategoryDetails>> GetCategoriesAsync(CancellationToken ct)
    {
        return await _dbContext.Categories
            .Include(x => x.Parent)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new CategoryDetails(
                x.Id,
                x.Name,
                x.Description,
                x.ParentId,
                x.Parent != null ? x.Parent.Name : null,
                x.SortOrder,
                x.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<CategoryDetails?> GetCategoryByIdAsync(Guid id, CancellationToken ct)
    {
        return await _dbContext.Categories
            .Include(x => x.Parent)
            .Where(x => x.Id == id)
            .Select(x => new CategoryDetails(
                x.Id,
                x.Name,
                x.Description,
                x.ParentId,
                x.Parent != null ? x.Parent.Name : null,
                x.SortOrder,
                x.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CategoryDetails> CreateCategoryAsync(CreateCategoryInput input, CancellationToken ct)
    {
        var exists = await _dbContext.Categories.AnyAsync(x => x.Name == input.Name, ct);
        if (exists)
            throw new BusinessException("category name already exists", ApiCodes.Common.ValidationFailed, 400);

        if (input.ParentId.HasValue)
        {
            var parentExists = await _dbContext.Categories.AnyAsync(x => x.Id == input.ParentId.Value, ct);
            if (!parentExists)
                throw new BusinessException("parent category not found", ApiCodes.Common.NotFound, 404);
        }

        var now = DateTimeOffset.UtcNow;
        var entity = CategoryEntity.Create(input.Name, input.Description, input.ParentId, input.SortOrder, now);

        _dbContext.Categories.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        return new CategoryDetails(entity.Id, entity.Name, entity.Description, entity.ParentId, null, entity.SortOrder, entity.CreatedAt);
    }

    public async Task<CategoryDetails> UpdateCategoryAsync(Guid id, UpdateCategoryInput input, CancellationToken ct)
    {
        var entity = await _dbContext.Categories
            .Include(x => x.Parent)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            throw new BusinessException("category not found", ApiCodes.Common.NotFound, 404);

        var nameConflict = await _dbContext.Categories.AnyAsync(x => x.Name == input.Name && x.Id != id, ct);
        if (nameConflict)
            throw new BusinessException("category name already exists", ApiCodes.Common.ValidationFailed, 400);

        if (input.ParentId.HasValue && input.ParentId.Value == id)
            throw new BusinessException("category cannot be its own parent", ApiCodes.Common.ValidationFailed, 400);

        if (input.ParentId.HasValue)
        {
            var parentExists = await _dbContext.Categories.AnyAsync(x => x.Id == input.ParentId.Value, ct);
            if (!parentExists)
                throw new BusinessException("parent category not found", ApiCodes.Common.NotFound, 404);
        }

        entity.UpdateInfo(input.Name, input.Description, input.ParentId, input.SortOrder, DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(ct);

        return new CategoryDetails(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.ParentId,
            entity.Parent?.Name,
            entity.SortOrder,
            entity.CreatedAt);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken ct)
    {
        var hasChildren = await _dbContext.Categories.AnyAsync(x => x.ParentId == id, ct);
        if (hasChildren)
            throw new BusinessException("cannot delete category with subcategories", ApiCodes.Common.ValidationFailed, 400);

        var hasBooks = await _dbContext.Books.AnyAsync(x => x.CategoryId == id, ct);
        if (hasBooks)
            throw new BusinessException("cannot delete category with books", ApiCodes.Common.ValidationFailed, 400);

        var entity = await _dbContext.Categories.FindAsync([id], ct);
        if (entity is null)
            throw new BusinessException("category not found", ApiCodes.Common.NotFound, 404);

        _dbContext.Categories.Remove(entity);
        await _dbContext.SaveChangesAsync(ct);
    }
}
