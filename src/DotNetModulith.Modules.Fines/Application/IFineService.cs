namespace DotNetModulith.Modules.Fines.Application;

public interface IFineService
{
    Task<FineListItem[]> GetFinesAsync(string? keyword, string? status, int page, int pageSize, CancellationToken ct);
    Task<int> GetFinesCountAsync(string? keyword, string? status, CancellationToken ct);
    Task<FineDetails?> GetFineByIdAsync(Guid id, CancellationToken ct);
    Task<FineListItem[]> GetFinesByMemberAsync(Guid memberId, CancellationToken ct);
    Task<FineDetails> CreateFineAsync(CreateFineInput input, CancellationToken ct);
    Task PayFineAsync(Guid fineId, CancellationToken ct);
    Task WaiveFineAsync(Guid fineId, CancellationToken ct);
}
