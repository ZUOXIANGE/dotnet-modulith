using Microsoft.AspNetCore.Http;

namespace DotNetModulith.Modules.Storage.Application;

public interface IObjectStorageService
{
    Task<StorageObjectResult> UploadDirectAsync(IFormFile file, string? objectKey, CancellationToken ct);

    Task<PresignedUploadResult> CreatePresignedUploadAsync(string fileName, string? objectKey, CancellationToken ct);

    Task<byte[]> GetObjectBytesAsync(string objectKey, CancellationToken ct);
}
