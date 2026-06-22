namespace DotNetModulith.Modules.Storage.Application;

public sealed record StorageObjectInfo(
    string ObjectKey,
    string ContentType,
    long Size);
