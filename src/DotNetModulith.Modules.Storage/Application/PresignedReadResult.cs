namespace DotNetModulith.Modules.Storage.Application;

public sealed record PresignedReadResult(
    string ObjectKey,
    string AccessUrl,
    DateTimeOffset ExpiresAtUtc);
