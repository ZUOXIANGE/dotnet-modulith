using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Members.Domain;
using DotNetModulith.Modules.Members.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Members.Application;

internal sealed class MemberService : IMemberService
{
    private readonly MembersDbContext _dbContext;

    public MemberService(MembersDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<MemberListItem>> GetMembersAsync(string? keyword, string? status, int page, int pageSize, CancellationToken ct)
    {
        var query = _dbContext.Members.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(x => x.Name.Contains(kw) || x.Phone.Contains(kw) || x.Email.Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MemberStatus>(status, true, out var memberStatus))
            query = query.Where(x => x.Status == memberStatus);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MemberListItem(
                x.Id,
                x.Name,
                x.Phone,
                x.Email,
                x.MembershipType.ToString(),
                x.Status.ToString(),
                x.MaxBorrowCount,
                x.CurrentBorrowCount,
                x.JoinDate,
                x.ExpiryDate,
                x.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<int> GetMembersCountAsync(string? keyword, string? status, CancellationToken ct)
    {
        var query = _dbContext.Members.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(x => x.Name.Contains(kw) || x.Phone.Contains(kw) || x.Email.Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MemberStatus>(status, true, out var memberStatus))
            query = query.Where(x => x.Status == memberStatus);

        return await query.CountAsync(ct);
    }

    public async Task<MemberDetails?> GetMemberByIdAsync(Guid memberId, CancellationToken ct)
    {
        return await _dbContext.Members
            .Where(x => x.Id == memberId)
            .Select(x => new MemberDetails(
                x.Id,
                x.Name,
                x.Phone,
                x.Email,
                x.Address,
                x.MembershipType.ToString(),
                x.Status.ToString(),
                x.MaxBorrowCount,
                x.CurrentBorrowCount,
                x.JoinDate,
                x.ExpiryDate,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<MemberDetails> CreateMemberAsync(CreateMemberInput input, CancellationToken ct)
    {
        var phoneExists = await _dbContext.Members.AnyAsync(x => x.Phone == input.Phone, ct);
        if (phoneExists)
            throw new BusinessException("phone already exists", ApiCodes.Common.ValidationFailed);

        if (!Enum.TryParse<MembershipType>(input.MembershipType, true, out var membershipType))
            throw new BusinessException("invalid membership type", ApiCodes.Common.ValidationFailed);

        var now = DateTimeOffset.UtcNow;
        var entity = MemberEntity.Create(
            input.Name,
            input.Phone,
            input.Email,
            input.Address,
            membershipType,
            input.JoinDate,
            input.ExpiryDate,
            now);

        _dbContext.Members.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        return new MemberDetails(
            entity.Id,
            entity.Name,
            entity.Phone,
            entity.Email,
            entity.Address,
            entity.MembershipType.ToString(),
            entity.Status.ToString(),
            entity.MaxBorrowCount,
            entity.CurrentBorrowCount,
            entity.JoinDate,
            entity.ExpiryDate,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    public async Task<MemberDetails> UpdateMemberAsync(Guid memberId, UpdateMemberInput input, CancellationToken ct)
    {
        var entity = await _dbContext.Members.FindAsync(new object[] { memberId }, ct);
        if (entity is null)
            throw new BusinessException("member not found", ApiCodes.Common.NotFound);

        var phoneExists = await _dbContext.Members.AnyAsync(x => x.Phone == input.Phone && x.Id != memberId, ct);
        if (phoneExists)
            throw new BusinessException("phone already exists", ApiCodes.Common.ValidationFailed);

        if (!Enum.TryParse<MembershipType>(input.MembershipType, true, out var membershipType))
            throw new BusinessException("invalid membership type", ApiCodes.Common.ValidationFailed);

        entity.Update(input.Name, input.Phone, input.Email, input.Address, membershipType, input.ExpiryDate, DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(ct);

        return new MemberDetails(
            entity.Id,
            entity.Name,
            entity.Phone,
            entity.Email,
            entity.Address,
            entity.MembershipType.ToString(),
            entity.Status.ToString(),
            entity.MaxBorrowCount,
            entity.CurrentBorrowCount,
            entity.JoinDate,
            entity.ExpiryDate,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    public async Task DeleteMemberAsync(Guid memberId, CancellationToken ct)
    {
        var entity = await _dbContext.Members.FindAsync(new object[] { memberId }, ct);
        if (entity is null)
            throw new BusinessException("member not found", ApiCodes.Common.NotFound);

        entity.Cancel(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task SuspendMemberAsync(Guid memberId, CancellationToken ct)
    {
        var entity = await _dbContext.Members.FindAsync(new object[] { memberId }, ct);
        if (entity is null)
            throw new BusinessException("member not found", ApiCodes.Common.NotFound);

        entity.Suspend(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task ActivateMemberAsync(Guid memberId, CancellationToken ct)
    {
        var entity = await _dbContext.Members.FindAsync(new object[] { memberId }, ct);
        if (entity is null)
            throw new BusinessException("member not found", ApiCodes.Common.NotFound);

        entity.Activate(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task IncrementBorrowCountAsync(Guid memberId, CancellationToken ct)
    {
        var entity = await _dbContext.Members.FindAsync(new object[] { memberId }, ct);
        if (entity is null)
            throw new BusinessException("member not found", ApiCodes.Common.NotFound);

        entity.IncrementBorrowCount(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task DecrementBorrowCountAsync(Guid memberId, CancellationToken ct)
    {
        var entity = await _dbContext.Members.FindAsync(new object[] { memberId }, ct);
        if (entity is null)
            throw new BusinessException("member not found", ApiCodes.Common.NotFound);

        entity.DecrementBorrowCount(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(ct);
    }
}
