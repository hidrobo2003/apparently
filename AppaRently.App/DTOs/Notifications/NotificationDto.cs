namespace AppaRently.App.DTOs.Notifications;

public sealed record NotificationResponse
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? ActionUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime? DeletedAt { get; init; }
    public bool IsRead => ReadAt is not null;
}
