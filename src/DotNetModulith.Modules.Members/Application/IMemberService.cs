namespace DotNetModulith.Modules.Members.Application;

public interface IMemberService
{
    Task<IReadOnlyList<MemberListItem>> GetMembersAsync(string? keyword, string? status, int page, int pageSize, CancellationToken ct);
    Task<int> GetMembersCountAsync(string? keyword, string? status, CancellationToken ct);
    Task<MemberDetails?> GetMemberByIdAsync(Guid memberId, CancellationToken ct);
    Task<MemberDetails> CreateMemberAsync(CreateMemberInput input, CancellationToken ct);
    Task<MemberDetails> UpdateMemberAsync(Guid memberId, UpdateMemberInput input, CancellationToken ct);
    Task DeleteMemberAsync(Guid memberId, CancellationToken ct);
    Task SuspendMemberAsync(Guid memberId, CancellationToken ct);
    Task ActivateMemberAsync(Guid memberId, CancellationToken ct);
    Task IncrementBorrowCountAsync(Guid memberId, CancellationToken ct);
    Task DecrementBorrowCountAsync(Guid memberId, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, string>> GetMemberNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct);
}
