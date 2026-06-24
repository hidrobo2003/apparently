using AppaRently.App.DTOs.Apartments;
using AppaRently.App.DTOs.Favorites;
using AppaRently.App.DTOs.Reservations;
using AppaRently.App.DTOs.Users;

namespace AppaRently.Web.Client.Models;

public sealed class ApartmentDashboardViewModel
{
    public ApartmentSearchRequest Filters { get; set; } = new();

    public IReadOnlyList<ApartmentResponse> Apartments { get; set; } = Array.Empty<ApartmentResponse>();
}

public sealed class ApartmentCalendarDayViewModel
{
    public DateTime Date { get; set; }

    public bool IsBooked { get; set; }

    public bool IsToday { get; set; }
}

public sealed class ApartmentDetailViewModel
{
    public ApartmentResponse Apartment { get; set; } = new();

    public int FavoriteCount { get; set; }

    public bool IsFavorited { get; set; }

    public bool IsAuthenticated { get; set; }

    public IReadOnlyList<ApartmentCalendarDayViewModel> CalendarDays { get; set; } = Array.Empty<ApartmentCalendarDayViewModel>();
}

public sealed class FavoritePageViewModel
{
    public IReadOnlyList<FavoriteResponse> Favorites { get; set; } = Array.Empty<FavoriteResponse>();
}

public sealed class ProfileViewModel
{
    public UserResponse User { get; set; } = new();

    public IReadOnlyList<ReservationResponse> Reservations { get; set; } = Array.Empty<ReservationResponse>();
}
