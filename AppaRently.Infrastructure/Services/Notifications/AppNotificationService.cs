using AppaRently.App.DTOs.Notifications;
using AppaRently.App.Interfaces;
using AppaRently.Domain.Models;
using AppaRently.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppaRently.Infrastructure.Services.Notifications;

public sealed class AppNotificationService : IAppNotificationService
{
    private readonly AppaRentlyDbContext _dbContext;

    public AppNotificationService(AppaRentlyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NotificationResponse> CreateAsync(
        string userId,
        string title,
        string body,
        string type,
        string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new AppNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            Body = body.Trim(),
            Type = type.Trim(),
            ActionUrl = NormalizeOptionalString(actionUrl),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(notification);
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetInboxAsync(
        string userId,
        bool unreadOnly = false,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Notifications
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.DeletedAt == null);

        if (unreadOnly)
        {
            query = query.Where(x => x.ReadAt == null);
        }

        var notifications = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);

        return notifications.Select(MapToDto).ToList();
    }

    public Task<int> CountUnreadAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Notifications
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(x => x.UserId == userId && x.DeletedAt == null && x.ReadAt == null, cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, string userId, CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.Notifications
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, cancellationToken);

        if (notification is null || notification.DeletedAt is not null)
        {
            return false;
        }

        notification.ReadAt ??= DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _dbContext.Notifications
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId && x.DeletedAt == null && x.ReadAt == null)
            .ToListAsync(cancellationToken);

        if (notifications.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var notification in notifications)
        {
            notification.ReadAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return notifications.Count;
    }

    private static NotificationResponse MapToDto(AppNotification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Body = notification.Body,
            Type = notification.Type,
            ActionUrl = notification.ActionUrl,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt,
            DeletedAt = notification.DeletedAt
        };
    }

    private static string? NormalizeOptionalString(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
