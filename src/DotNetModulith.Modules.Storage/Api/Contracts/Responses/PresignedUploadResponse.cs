namespace DotNetModulith.Modules.Storage.Api.Contracts.Responses;

public sealed record PresignedUploadResponse(
    Guid UploadId,
    string ObjectKey,
    string UploadUrl,
    DateTimeOffset ExpiresAtUtc);
