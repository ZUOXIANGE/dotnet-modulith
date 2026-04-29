using System.Text;
using DotNetModulith.IntegrationTests.Fixtures;
using DotNetModulith.Modules.Storage;
using DotNetModulith.Modules.Storage.Application;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetModulith.IntegrationTests.Storage;

public sealed class StorageUploadTests : IClassFixture<S3CompatibleStorageFixture>, IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IObjectStorageService _storageService;

    public StorageUploadTests(S3CompatibleStorageFixture storageFixture)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Endpoint"] = storageFixture.Endpoint,
                ["Storage:AccessKey"] = S3CompatibleStorageFixture.AccessKey,
                ["Storage:SecretKey"] = S3CompatibleStorageFixture.SecretKey,
                ["Storage:BucketName"] = S3CompatibleStorageFixture.BucketName,
                ["Storage:ForcePathStyle"] = "true",
                ["Storage:UseSsl"] = "false",
                ["Storage:PresignedUrlExpireSeconds"] = "600"
            })
            .Build();

        var services = new ServiceCollection();
        var module = new StorageModule();
        module.AddModuleServices(services, configuration);

        _serviceProvider = services.BuildServiceProvider();
        _storageService = _serviceProvider.GetRequiredService<IObjectStorageService>();
    }

    [Fact]
    public async Task UploadDirect_ShouldPersistObjectToStorage()
    {
        var bytes = Encoding.UTF8.GetBytes("direct upload content");
        await using var stream = new MemoryStream(bytes);
        IFormFile file = new FormFile(stream, 0, bytes.Length, "file", "direct.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var uploadResult = await _storageService.UploadDirectAsync(file, null, TestContext.Current.CancellationToken);
        var readBytes = await _storageService.GetObjectBytesAsync(uploadResult.ObjectKey, TestContext.Current.CancellationToken);

        readBytes.Should().Equal(bytes);
    }

    [Fact]
    public async Task CreatePresignedUpload_ShouldAllowClientDirectPut()
    {
        var presignResult = await _storageService.CreatePresignedUploadAsync("signed.txt", null, TestContext.Current.CancellationToken);

        var uploadBytes = Encoding.UTF8.GetBytes("signed upload content");
        using var putClient = new HttpClient();
        using var putRequest = new HttpRequestMessage(HttpMethod.Put, presignResult.UploadUrl)
        {
            Content = new ByteArrayContent(uploadBytes)
        };
        var putResponse = await putClient.SendAsync(putRequest, TestContext.Current.CancellationToken);
        putResponse.IsSuccessStatusCode.Should().BeTrue();

        var readBytes = await _storageService.GetObjectBytesAsync(presignResult.ObjectKey, TestContext.Current.CancellationToken);
        readBytes.Should().Equal(uploadBytes);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}
