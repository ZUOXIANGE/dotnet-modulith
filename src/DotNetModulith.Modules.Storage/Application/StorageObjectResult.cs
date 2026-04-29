namespace DotNetModulith.Modules.Storage.Application;

public sealed record StorageObjectResult(string ObjectKey, string ObjectUrl, long Size);

public sealed record PresignedUploadResult(string ObjectKey, string UploadUrl, DateTimeOffset ExpiresAtUtc);
