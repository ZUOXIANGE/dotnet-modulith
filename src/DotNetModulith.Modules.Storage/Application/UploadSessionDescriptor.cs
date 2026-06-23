namespace DotNetModulith.Modules.Storage.Application;

public sealed record UploadSessionDescriptor(
    Guid UploadId,
    string ObjectKey,
    string UploadUrl,
    DateTimeOffset ExpiresAtUtc);
