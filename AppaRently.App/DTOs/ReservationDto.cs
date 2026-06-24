using System.ComponentModel.DataAnnotations;

namespace AppaRently.App.DTOs.Reservations;

public sealed record CreateReservationRequest
{
    public Guid ApartmentId { get; init; }

    public DateTime CheckIn { get; init; }

    public DateTime CheckOut { get; init; }
}

public sealed record UpdateReservationRequest
{
    public DateTime CheckIn { get; init; }

    public DateTime CheckOut { get; init; }
}

public sealed record ReservationSearchRequest
{
    [MaxLength(450)]
    public string? UserId { get; init; }

    public Guid? ApartmentId { get; init; }

    public DateTime? CheckInFrom { get; init; }

    public DateTime? CheckInTo { get; init; }
}

public sealed record ReservationResponse
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string UserFullName { get; init; } = string.Empty;
    public Guid ApartmentId { get; init; }
    public string ApartmentTitle { get; init; } = string.Empty;
    public string ApartmentAddress { get; init; } = string.Empty;
    public DateTime CheckIn { get; init; }
    public DateTime CheckOut { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}
