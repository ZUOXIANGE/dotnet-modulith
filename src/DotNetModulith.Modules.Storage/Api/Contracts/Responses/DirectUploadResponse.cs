namespace DotNetModulith.Modules.Storage.Api.Contracts.Responses;

public sealed record DirectUploadResponse(string ObjectKey, string ObjectUrl, long Size);
