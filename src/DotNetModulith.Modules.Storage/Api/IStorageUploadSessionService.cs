using DotNetModulith.Modules.Storage.Application;

namespace DotNetModulith.Modules.Storage.Api;

public interface IStorageUploadSessionService
{
    Task<UploadSessionDescriptor> CreateUploadSessionAsync(
        Guid ownerUserId,
        CreateUploadSessionInput input,
        CancellationToken ct);

    Task<ConsumedStorageObject> ConsumeUploadSessionAsync(
        Guid ownerUserId,
        Guid uploadId,
        string purpose,
        CancellationToken ct);
}
