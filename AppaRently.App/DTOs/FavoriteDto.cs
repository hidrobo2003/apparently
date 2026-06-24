using System.ComponentModel.DataAnnotations;

namespace AppaRently.App.DTOs.Favorites;

public sealed record CreateFavoriteRequest
{
    public Guid ApartmentId { get; init; }
}

public sealed record FavoriteSearchRequest
{
    [MaxLength(450)]
    public string? UserId { get; init; }

    public Guid? ApartmentId { get; init; }
}

public sealed record FavoriteResponse
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string UserFullName { get; init; } = string.Empty;
    public Guid ApartmentId { get; init; }
    public string ApartmentTitle { get; init; } = string.Empty;
    public string ApartmentAddress { get; init; } = string.Empty;
    public DateTime? DeletedAt { get; init; }
}
