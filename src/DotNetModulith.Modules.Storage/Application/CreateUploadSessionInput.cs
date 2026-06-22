namespace DotNetModulith.Modules.Storage.Application;

public sealed record CreateUploadSessionInput(
    string FileName,
    string ContentType,
    string Purpose);
