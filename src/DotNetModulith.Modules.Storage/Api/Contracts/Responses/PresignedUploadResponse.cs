namespace DotNetModulith.Modules.Storage.Api.Contracts.Responses;

public sealed record PresignedUploadResponse(string ObjectKey, string UploadUrl, DateTimeOffset ExpiresAtUtc);
