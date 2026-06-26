using AppaRently.App.DTOs.Notifications;

namespace AppaRently.App.Interfaces;

public interface IAppNotificationService
{
    Task<NotificationResponse> CreateAsync(
        string userId,
        string title,
        string body,
        string type,
        Guid? apartmentId = null,
        string? actionUrl = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationResponse>> GetInboxAsync(
        string userId,
        bool unreadOnly = false,
        bool favoritesOnly = false,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(
        string userId,
        bool favoritesOnly = false,
        CancellationToken cancellationToken = default);

    Task<bool> MarkAsReadAsync(Guid notificationId, string userId, CancellationToken cancellationToken = default);

    Task<int> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
}
