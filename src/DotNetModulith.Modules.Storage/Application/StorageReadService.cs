using DotNetModulith.Modules.Storage.Api;

namespace DotNetModulith.Modules.Storage.Application;

internal sealed class StorageReadService : IStorageReadService
{
    private readonly IObjectStorageService _objectStorageService;

    public StorageReadService(IObjectStorageService objectStorageService)
    {
        _objectStorageService = objectStorageService;
    }

    public async Task<string> GetPresignedReadUrlAsync(string objectUrlOrKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(objectUrlOrKey))
            return string.Empty;

        var result = await _objectStorageService.CreatePresignedReadAsync(objectUrlOrKey, ct);
        return result.AccessUrl;
    }
}
