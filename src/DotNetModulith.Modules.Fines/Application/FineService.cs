using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Fines.Domain;
using DotNetModulith.Modules.Fines.Infrastructure;
using DotNetModulith.Modules.Members.Application;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Fines.Application;

internal sealed class FineService : IFineService
{
    private readonly FinesDbContext _dbContext;
    private readonly IMemberService _memberService;
    private readonly ICapPublisher _capPublisher;

    public FineService(FinesDbContext dbContext, IMemberService memberService, ICapPublisher capPublisher)
    {
        _dbContext = dbContext;
        _memberService = memberService;
        _capPublisher = capPublisher;
    }

    public async Task<FineListItem[]> GetFinesAsync(string? keyword, string? status, int page, int pageSize, CancellationToken ct)
    {
        var query = _dbContext.Fines.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<FineStatus>(status, true, out var fineStatus))
            query = query.Where(x => x.Status == fineStatus);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FineListItem(
                x.Id,
                x.MemberId,
                string.Empty,
                x.BorrowingRecordId,
                x.Amount,
                x.Reason.ToString(),
                x.Status.ToString(),
                x.CreatedAt,
                x.PaidAt))
            .ToArrayAsync(ct);

        if (items.Length > 0)
        {
            var memberIds = items.Select(x => x.MemberId).Distinct();
            var memberNames = await _memberService.GetMemberNamesByIdsAsync(memberIds, ct);
            for (var i = 0; i < items.Length; i++)
            {
                var name = memberNames.GetValueOrDefault(items[i].MemberId, string.Empty);
                items[i] = items[i] with { MemberName = name };
            }
        }

        return items;
    }

    public async Task<int> GetFinesCountAsync(string? keyword, string? status, CancellationToken ct)
    {
        var query = _dbContext.Fines.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<FineStatus>(status, true, out var fineStatus))
            query = query.Where(x => x.Status == fineStatus);

        return await query.CountAsync(ct);
    }

    public async Task<FineDetails?> GetFineByIdAsync(Guid id, CancellationToken ct)
    {
        var fine = await _dbContext.Fines
            .Where(x => x.Id == id)
            .Select(x => new FineDetails(
                x.Id,
                x.MemberId,
                string.Empty,
                x.BorrowingRecordId,
                x.Amount,
                x.Reason.ToString(),
                x.Status.ToString(),
                x.CreatedAt,
                x.PaidAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(ct);

        if (fine is not null)
        {
            var memberNames = await _memberService.GetMemberNamesByIdsAsync(new[] { fine.MemberId }, ct);
            fine = fine with { MemberName = memberNames.GetValueOrDefault(fine.MemberId, string.Empty) };
        }

        return fine;
    }

    public async Task<FineListItem[]> GetFinesByMemberAsync(Guid memberId, CancellationToken ct)
    {
        var items = await _dbContext.Fines
            .Where(x => x.MemberId == memberId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new FineListItem(
                x.Id,
                x.MemberId,
                string.Empty,
                x.BorrowingRecordId,
                x.Amount,
                x.Reason.ToString(),
                x.Status.ToString(),
                x.CreatedAt,
                x.PaidAt))
            .ToArrayAsync(ct);

        if (items.Length > 0)
        {
            var memberNames = await _memberService.GetMemberNamesByIdsAsync(new[] { memberId }, ct);
            var name = memberNames.GetValueOrDefault(memberId, string.Empty);
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = items[i] with { MemberName = name };
            }
        }

        return items;
    }

    public async Task<FineDetails> CreateFineAsync(CreateFineInput input, CancellationToken ct)
    {
        var member = await _memberService.GetMemberByIdAsync(input.MemberId, ct);
        if (member is null)
            throw new BusinessException("member not found", ApiCodes.Common.NotFound);

        if (!Enum.TryParse<FineReason>(input.Reason, true, out var reason))
            throw new BusinessException("invalid fine reason", ApiCodes.Common.ValidationFailed);

        if (input.Amount <= 0)
            throw new BusinessException("fine amount must be greater than zero", ApiCodes.Common.ValidationFailed);

        var now = DateTimeOffset.UtcNow;
        var entity = FineEntity.Create(input.MemberId, input.BorrowingRecordId, input.Amount, reason, now);

        using var transaction = _dbContext.Database.BeginTransaction(_capPublisher, autoCommit: false);

        _dbContext.Fines.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        await _capPublisher.PublishAsync(
            nameof(FineCreatedIntegrationEvent),
            new FineCreatedIntegrationEvent(
                entity.Id,
                entity.MemberId,
                member.Name,
                entity.BorrowingRecordId ?? default,
                entity.Amount,
                entity.Reason.ToString()),
            cancellationToken: ct);

        transaction.Commit();

        return new FineDetails(
            entity.Id,
            entity.MemberId,
            member.Name,
            entity.BorrowingRecordId,
            entity.Amount,
            entity.Reason.ToString(),
            entity.Status.ToString(),
            entity.CreatedAt,
            entity.PaidAt,
            entity.UpdatedAt);
    }

    public async Task PayFineAsync(Guid fineId, CancellationToken ct)
    {
        var entity = await _dbContext.Fines.FindAsync(new object[] { fineId }, ct);
        if (entity is null)
            throw new BusinessException("fine not found", ApiCodes.Common.NotFound);

        entity.Pay(DateTimeOffset.UtcNow);

        using var transaction = _dbContext.Database.BeginTransaction(_capPublisher, autoCommit: false);

        await _dbContext.SaveChangesAsync(ct);

        var member = await _memberService.GetMemberByIdAsync(entity.MemberId, ct);

        await _capPublisher.PublishAsync(
            nameof(FinePaidIntegrationEvent),
            new FinePaidIntegrationEvent(
                entity.Id,
                entity.MemberId,
                member?.Name ?? string.Empty,
                entity.Amount),
            cancellationToken: ct);

        transaction.Commit();
    }

    public async Task WaiveFineAsync(Guid fineId, CancellationToken ct)
    {
        var entity = await _dbContext.Fines.FindAsync(new object[] { fineId }, ct);
        if (entity is null)
            throw new BusinessException("fine not found", ApiCodes.Common.NotFound);

        entity.Waive(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(ct);
    }
}
