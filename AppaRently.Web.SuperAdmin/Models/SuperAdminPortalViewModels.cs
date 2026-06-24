using System.ComponentModel.DataAnnotations;
using AppaRently.App.DTOs.Apartments;
using AppaRently.App.DTOs.Reservations;
using AppaRently.App.DTOs.Users;
using AppaRently.Infrastructure.Data;

namespace AppaRently.Web.SuperAdmin.Models;

public class SuperAdminUserDashboardQueryViewModel
{
    public string? Role { get; set; }

    public bool DeletedOnly { get; set; }
}

public class SuperAdminApartmentDashboardQueryViewModel
{
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [DataType(DataType.Date)]
    public DateTime? To { get; set; }

    public string? City { get; set; }

    public string? Department { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public bool DeletedOnly { get; set; }
}

public class SuperAdminApartmentMetricsViewModel
{
    public decimal Revenue { get; set; }

    public decimal PotentialRevenue { get; set; }

    public decimal AverageReservationValue { get; set; }

    public decimal AverageStayDays { get; set; }

    public decimal ProfitabilityRate { get; set; }

    public decimal OccupancyRate { get; set; }

    public int BookedDays { get; set; }

    public int UniqueTenantCount { get; set; }

    public int AvailableDays { get; set; }

    public int ReservationCount { get; set; }
}

public sealed class SuperAdminCalendarDayViewModel
{
    public DateTime Date { get; set; }

    public bool IsBooked { get; set; }

    public bool IsToday { get; set; }
}

public sealed class SuperAdminUserDashboardItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}

public sealed class SuperAdminApartmentDashboardViewModel : SuperAdminApartmentDashboardQueryViewModel
{
    public IReadOnlyList<ApartmentResponse> Apartments { get; set; } = Array.Empty<ApartmentResponse>();

    public SuperAdminApartmentMetricsViewModel Metrics { get; set; } = new();
}

public sealed class SuperAdminUserDashboardViewModel : SuperAdminUserDashboardQueryViewModel
{
    public IReadOnlyList<SuperAdminUserDashboardItemViewModel> Users { get; set; } = Array.Empty<SuperAdminUserDashboardItemViewModel>();
}

public sealed class SuperAdminUserReservationItemViewModel
{
    public Guid ReservationId { get; set; }

    public string ApartmentTitle { get; set; } = string.Empty;

    public string ApartmentAddress { get; set; } = string.Empty;

    public DateTime CheckIn { get; set; }

    public DateTime CheckOut { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal PricePaid { get; set; }
}

public sealed class SuperAdminOwnedApartmentItemViewModel
{
    public Guid ApartmentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public decimal DailyPrice { get; set; }

    public decimal GeneratedAmount { get; set; }

    public int ReservationCount { get; set; }
}

public sealed class SuperAdminUserDetailViewModel
{
    public UserResponse User { get; set; } = new();

    public string Role { get; set; } = string.Empty;

    public decimal TotalMoneyUsed { get; set; }

    public decimal TotalMoneyGenerated { get; set; }

    public IReadOnlyList<SuperAdminUserReservationItemViewModel> Reservations { get; set; } = Array.Empty<SuperAdminUserReservationItemViewModel>();

    public IReadOnlyList<SuperAdminOwnedApartmentItemViewModel> Apartments { get; set; } = Array.Empty<SuperAdminOwnedApartmentItemViewModel>();

    public bool IsClient => string.Equals(Role, AppaRentlyRoles.Client, StringComparison.Ordinal);

    public bool IsOwner => string.Equals(Role, AppaRentlyRoles.Owner, StringComparison.Ordinal);
}

public sealed class SuperAdminApartmentDetailViewModel : SuperAdminApartmentMetricsViewModel
{
    public ApartmentResponse Apartment { get; set; } = new();

    public int FavoriteCount { get; set; }

    public string OwnerFullName { get; set; } = string.Empty;

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public IReadOnlyList<SuperAdminCalendarDayViewModel> CalendarDays { get; set; } = Array.Empty<SuperAdminCalendarDayViewModel>();
}

public sealed class SuperAdminProfileViewModel
{
    public UserResponse SuperAdmin { get; set; } = new();

    public bool CanDeleteAccount { get; set; }
}
