using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace DotNetModulith.IntegrationTests;

[Collection("Api collection")]
public sealed class BooksApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BooksApiTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateBook_WithCoverUploadId_ShouldPersistCoverImageUrl()
    {
        await _factory.InitializeUsersModuleAsync();

        var token = await LoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var categoryId = await CreateCategoryAsync("集成测试分类");
        var upload = await CreateUploadSessionAsync("cover.jpg", "image/jpeg", "book-cover");
        await UploadBytesAsync(upload.UploadUrl, "image/jpeg", [1, 2, 3, 4, 5]);

        var response = await _client.PostAsJsonAsync(
            "/api/books",
            new
            {
                Isbn = Guid.NewGuid().ToString("N")[..13],
                Title = "集成测试图书",
                Author = "测试作者",
                Publisher = "测试出版社",
                PublishDate = new DateOnly(2024, 1, 1),
                Description = "用于验证图书封面上传会话",
                CategoryId = categoryId,
                TotalCopies = 3,
                CoverUploadId = upload.UploadId
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        body["data"]!["title"]!.GetValue<string>().Should().Be("集成测试图书");
        body["data"]!["coverImageUrl"]!.GetValue<string>().Should().Contain("/books/covers/");
    }

    private async Task<Guid> CreateCategoryAsync(string name)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/categories",
            new
            {
                Name = $"{name}-{Guid.NewGuid():N}",
                Description = "集成测试分类",
                ParentId = (Guid?)null,
                SortOrder = 1
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        return body["data"]!["id"]!.GetValue<Guid>();
    }

    private async Task<(string AccessToken, Guid UserId)> LoginAsAdminAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                UserName = "admin",
                Password = "Admin@123456",
                CaptchaId = "test",
                CaptchaCode = "test"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        return (
            body["data"]!["accessToken"]!.GetValue<string>(),
            body["data"]!["user"]!["id"]!.GetValue<Guid>());
    }

    private async Task<(Guid UploadId, string UploadUrl)> CreateUploadSessionAsync(string fileName, string contentType, string purpose)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/storage/upload/presign",
            new
            {
                FileName = fileName,
                ContentType = contentType,
                Purpose = purpose
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        return (
            body["data"]!["uploadId"]!.GetValue<Guid>(),
            body["data"]!["uploadUrl"]!.GetValue<string>());
    }

    private static async Task UploadBytesAsync(string uploadUrl, string contentType, byte[] bytes)
    {
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
        {
            Content = new ByteArrayContent(bytes)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
