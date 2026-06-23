using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Notifications.Domain;
using DotNetModulith.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Notifications.Application;

internal sealed class NotificationService : INotificationService
{
    private readonly NotificationsDbContext _dbContext;

    public NotificationService(NotificationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NotificationListItem[]> GetNotificationsAsync(string? recipientId, bool? isRead, int page, int pageSize, CancellationToken ct)
    {
        var query = _dbContext.Notifications.AsQueryable();

        if (!string.IsNullOrWhiteSpace(recipientId))
            query = query.Where(x => x.RecipientId == recipientId);

        if (isRead.HasValue)
            query = query.Where(x => x.IsRead == isRead.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NotificationListItem(
                x.Id,
                x.Title,
                x.Content,
                x.Type.ToString(),
                x.RecipientId,
                x.IsRead,
                x.CreatedAt,
                x.ReadAt))
            .ToArrayAsync(ct);
    }

    public async Task<int> GetNotificationsCountAsync(string? recipientId, bool? isRead, CancellationToken ct)
    {
        var query = _dbContext.Notifications.AsQueryable();

        if (!string.IsNullOrWhiteSpace(recipientId))
            query = query.Where(x => x.RecipientId == recipientId);

        if (isRead.HasValue)
            query = query.Where(x => x.IsRead == isRead.Value);

        return await query.CountAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(string recipientId, CancellationToken ct)
    {
        return await _dbContext.Notifications
            .CountAsync(x => x.RecipientId == recipientId && !x.IsRead, ct);
    }

    public async Task<NotificationDetails?> GetNotificationByIdAsync(Guid id, CancellationToken ct)
    {
        return await _dbContext.Notifications
            .Where(x => x.Id == id)
            .Select(x => new NotificationDetails(
                x.Id,
                x.Title,
                x.Content,
                x.Type.ToString(),
                x.RecipientId,
                x.IsRead,
                x.CreatedAt,
                x.ReadAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<NotificationDetails> CreateNotificationAsync(CreateNotificationInput input, CancellationToken ct)
    {
        if (!Enum.TryParse<NotificationType>(input.Type, true, out var type))
            throw new BusinessException("invalid notification type", ApiCodes.Common.ValidationFailed);

        var now = DateTimeOffset.UtcNow;
        var entity = NotificationEntity.Create(input.Title, input.Content, type, input.RecipientId, now);

        _dbContext.Notifications.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        return new NotificationDetails(
            entity.Id,
            entity.Title,
            entity.Content,
            entity.Type.ToString(),
            entity.RecipientId,
            entity.IsRead,
            entity.CreatedAt,
            entity.ReadAt);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken ct)
    {
        var entity = await _dbContext.Notifications.AsTracking().FirstOrDefaultAsync(x => x.Id == notificationId, ct);
        if (entity is null)
            throw new BusinessException("notification not found", ApiCodes.Common.NotFound);

        entity.MarkAsRead(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(string recipientId, CancellationToken ct)
    {
        var unread = await _dbContext.Notifications
            .AsTracking()
            .Where(x => x.RecipientId == recipientId && !x.IsRead)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var item in unread)
        {
            item.MarkAsRead(now);
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
