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
public sealed class UsersApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UsersApiTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ThenGetMe_ShouldReturnCurrentUser()
    {
        await _factory.InitializeUsersModuleAsync();

        var token = await LoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var response = await _client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        body["data"]!["userName"]!.GetValue<string>().Should().Be("admin");
        body["data"]!["permissions"]!.AsArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task Logout_ShouldBlacklistCurrentToken()
    {
        await _factory.InitializeUsersModuleAsync();

        var token = await LoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var logoutResponse = await _client.PostAsync("/api/auth/logout", null, TestContext.Current.CancellationToken);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var meResponse = await _client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);
        var meBody = await meResponse.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        meBody!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Unauthorized);
    }

    [Fact]
    public async Task ForceLogout_ShouldInvalidateExistingToken()
    {
        await _factory.InitializeUsersModuleAsync();

        var token = await LoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var forceLogoutResponse = await _client.PostAsJsonAsync(
            $"/api/users/{token.UserId}/force-logout",
            new
            {
                Reason = "security test"
            },
            TestContext.Current.CancellationToken);

        forceLogoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var meResponse = await _client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);
        var meBody = await meResponse.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        meBody!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_ShouldInvalidateCurrentToken_AndAllowLoginWithNewPassword()
    {
        await _factory.InitializeUsersModuleAsync();

        var admin = await LoginAsAdminAsync();
        var createdUser = await CreateUserAsync(admin.AccessToken);

        var userLogin = await LoginAsync(createdUser.UserName, createdUser.Password);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userLogin.AccessToken);

        var changePasswordResponse = await _client.PostAsJsonAsync(
            "/api/auth/change-password",
            new
            {
                CurrentPassword = createdUser.Password,
                NewPassword = "User@654321"
            },
            TestContext.Current.CancellationToken);

        changePasswordResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var meResponse = await _client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);
        var meBody = await meResponse.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        meBody!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Unauthorized);

        var oldPasswordLogin = await LoginResponseAsync(createdUser.UserName, createdUser.Password);
        var oldPasswordBody = await oldPasswordLogin.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        oldPasswordBody!["code"]!.GetValue<int>().Should().Be(ApiCodes.Auth.InvalidCredentials);

        var newPasswordLogin = await LoginAsync(createdUser.UserName, "User@654321");
        newPasswordLogin.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetUserAndUpdateUser_ShouldReturnUpdatedProfile()
    {
        await _factory.InitializeUsersModuleAsync();

        var admin = await LoginAsAdminAsync();
        var createdUser = await CreateUserAsync(admin.AccessToken);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var getResponse = await _client.GetAsync($"/api/users/{createdUser.UserId}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        getBody!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        getBody["data"]!["userName"]!.GetValue<string>().Should().Be(createdUser.UserName);

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/users/{createdUser.UserId}",
            new
            {
                DisplayName = "Updated User",
                Email = $"{Guid.NewGuid():N}@modulith.local"
            },
            TestContext.Current.CancellationToken);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateBody = await updateResponse.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        updateBody!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        updateBody["data"]!["displayName"]!.GetValue<string>().Should().Be("Updated User");
    }

    [Fact]
    public async Task UpdateCurrentAvatar_ShouldReturnAvatarUrlInMe()
    {
        await _factory.InitializeUsersModuleAsync();

        var admin = await LoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var upload = await CreateUploadSessionAsync("avatar.png", "image/png", "user-avatar");
        await UploadBytesAsync(upload.UploadUrl, "image/png", [137, 80, 78, 71]);

        var updateResponse = await _client.PutAsJsonAsync(
            "/api/auth/avatar",
            new
            {
                UploadId = upload.UploadId
            },
            TestContext.Current.CancellationToken);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateBody = await updateResponse.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        updateBody!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        updateBody["data"]!["avatarUrl"]!.GetValue<string>().Should().Contain("/users/avatars/");

        var meResponse = await _client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var meBody = await meResponse.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        meBody!["data"]!["avatarUrl"]!.GetValue<string>().Should().Contain("/users/avatars/");

        var accessUrlResponse = await _client.GetAsync("/api/auth/avatar-access-url", TestContext.Current.CancellationToken);
        accessUrlResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var accessUrlBody = await accessUrlResponse.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        accessUrlBody!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        accessUrlBody["data"]!["avatarAccessUrl"]!.GetValue<string>().Should().Contain("X-Amz-Signature");
    }

    private async Task<(string AccessToken, Guid UserId)> LoginAsAdminAsync()
        => await LoginAsync("admin", "Admin@123456");

    private async Task<(string AccessToken, Guid UserId)> LoginAsync(string userName, string password)
    {
        var response = await LoginResponseAsync(userName, password);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);

        var accessToken = body["data"]!["accessToken"]!.GetValue<string>();
        var userId = body["data"]!["user"]!["id"]!.GetValue<Guid>();
        return (accessToken, userId);
    }

    private async Task<(Guid UserId, string UserName, string Password)> CreateUserAsync(string adminAccessToken)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userName = $"user_{suffix}";
        const string password = "User@123456";

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAccessToken);

        var response = await _client.PostAsJsonAsync(
            "/api/users",
            new
            {
                UserName = userName,
                DisplayName = $"User {suffix}",
                Email = $"{suffix}@modulith.local",
                Password = password,
                RoleIds = Array.Empty<Guid>()
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);

        var userId = body["data"]!["id"]!.GetValue<Guid>();
        return (userId, userName, password);
    }

    private async Task<HttpResponseMessage> LoginResponseAsync(string userName, string password)
    {
        _client.DefaultRequestHeaders.Authorization = null;

        return await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                UserName = userName,
                Password = password,
                CaptchaId = "test",
                CaptchaCode = "test"
            },
            TestContext.Current.CancellationToken);
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
