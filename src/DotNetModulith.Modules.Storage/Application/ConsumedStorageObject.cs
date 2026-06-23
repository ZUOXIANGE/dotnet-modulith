namespace DotNetModulith.Modules.Storage.Application;

public sealed record ConsumedStorageObject(
    Guid UploadId,
    string ObjectKey,
    string ObjectUrl,
    string ContentType,
    long Size);
