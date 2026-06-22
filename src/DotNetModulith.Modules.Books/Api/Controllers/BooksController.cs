using System.Security.Claims;
using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Books.Api.Contracts.Requests;
using DotNetModulith.Modules.Books.Api.Contracts.Responses;
using DotNetModulith.Modules.Books.Api.Mappings;
using DotNetModulith.Modules.Books.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Books.Api.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [Authorize(Policy = BooksPermissions.BooksView)]
    [HttpGet]
    public async Task<ApiResponse<BookListResponse>> GetBooks(
        [FromQuery] string? keyword,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var books = await _bookService.GetBooksAsync(keyword, categoryId, page, pageSize, ct);
        var total = await _bookService.GetBooksCountAsync(keyword, categoryId, ct);

        return ApiResponse.Success(new BookListResponse(
            books.Select(x => x.ToResponse()).ToArray(),
            total,
            page,
            pageSize));
    }

    [Authorize(Policy = BooksPermissions.BooksView)]
    [HttpGet("{bookId:guid}")]
    public async Task<ApiResponse<BookDetailsResponse>> GetBook(Guid bookId, CancellationToken ct)
    {
        var book = await _bookService.GetBookByIdAsync(bookId, ct);
        if (book is null)
            return ApiResponse.Failure<BookDetailsResponse>("book not found", ApiCodes.Common.NotFound);

        return ApiResponse.Success(book.ToResponse());
    }

    [Authorize(Policy = BooksPermissions.BooksCreate)]
    [HttpPost]
    public async Task<ApiResponse<BookDetailsResponse>> CreateBook([FromBody] CreateBookRequest request, CancellationToken ct)
    {
        var input = new CreateBookInput(
            GetCurrentUserId(),
            request.Isbn,
            request.Title,
            request.Author,
            request.Publisher,
            request.PublishDate,
            request.Description,
            request.CategoryId,
            request.TotalCopies,
            request.CoverUploadId);

        var book = await _bookService.CreateBookAsync(input, ct);
        return ApiResponse.Success(book.ToResponse());
    }

    [Authorize(Policy = BooksPermissions.BooksEdit)]
    [HttpPut("{bookId:guid}")]
    public async Task<ApiResponse<BookDetailsResponse>> UpdateBook(Guid bookId, [FromBody] UpdateBookRequest request, CancellationToken ct)
    {
        var input = new UpdateBookInput(
            GetCurrentUserId(),
            request.Isbn,
            request.Title,
            request.Author,
            request.Publisher,
            request.PublishDate,
            request.Description,
            request.CategoryId,
            request.TotalCopies,
            request.CoverUploadId,
            request.ClearCoverImage);

        var book = await _bookService.UpdateBookAsync(bookId, input, ct);
        return ApiResponse.Success(book.ToResponse());
    }

    [Authorize(Policy = BooksPermissions.BooksDelete)]
    [HttpDelete("{bookId:guid}")]
    public async Task<ApiResponse<object?>> DeleteBook(Guid bookId, CancellationToken ct)
    {
        await _bookService.DeleteBookAsync(bookId, ct);
        return ApiResponse.Success();
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new BusinessException("invalid token", ApiCodes.Auth.InvalidToken, StatusCodes.Status401Unauthorized);
    }
}
