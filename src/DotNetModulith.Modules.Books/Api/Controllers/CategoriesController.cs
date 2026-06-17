using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Books.Api.Contracts.Requests;
using DotNetModulith.Modules.Books.Api.Contracts.Responses;
using DotNetModulith.Modules.Books.Api.Mappings;
using DotNetModulith.Modules.Books.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Books.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [Authorize(Policy = BooksPermissions.CategoriesView)]
    [HttpGet]
    public async Task<ApiResponse<IReadOnlyList<CategoryDetailsResponse>>> GetCategories(CancellationToken ct)
    {
        var categories = await _categoryService.GetCategoriesAsync(ct);
        return ApiResponse.Success<IReadOnlyList<CategoryDetailsResponse>>(categories.Select(x => x.ToResponse()).ToArray());
    }

    [Authorize(Policy = BooksPermissions.CategoriesView)]
    [HttpGet("{categoryId:guid}")]
    public async Task<ApiResponse<CategoryDetailsResponse>> GetCategory(Guid categoryId, CancellationToken ct)
    {
        var category = await _categoryService.GetCategoryByIdAsync(categoryId, ct);
        if (category is null)
            return ApiResponse.Failure<CategoryDetailsResponse>("category not found", ApiCodes.Common.NotFound);

        return ApiResponse.Success(category.ToResponse());
    }

    [Authorize(Policy = BooksPermissions.CategoriesCreate)]
    [HttpPost]
    public async Task<ApiResponse<CategoryDetailsResponse>> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var input = new CreateCategoryInput(request.Name, request.Description, request.ParentId, request.SortOrder);
        var category = await _categoryService.CreateCategoryAsync(input, ct);
        return ApiResponse.Success(category.ToResponse());
    }

    [Authorize(Policy = BooksPermissions.CategoriesEdit)]
    [HttpPut("{categoryId:guid}")]
    public async Task<ApiResponse<CategoryDetailsResponse>> UpdateCategory(Guid categoryId, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
    {
        var input = new UpdateCategoryInput(request.Name, request.Description, request.ParentId, request.SortOrder);
        var category = await _categoryService.UpdateCategoryAsync(categoryId, input, ct);
        return ApiResponse.Success(category.ToResponse());
    }

    [Authorize(Policy = BooksPermissions.CategoriesDelete)]
    [HttpDelete("{categoryId:guid}")]
    public async Task<ApiResponse<object?>> DeleteCategory(Guid categoryId, CancellationToken ct)
    {
        await _categoryService.DeleteCategoryAsync(categoryId, ct);
        return ApiResponse.Success();
    }
}
