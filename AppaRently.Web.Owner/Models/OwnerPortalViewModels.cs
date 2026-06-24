using System.ComponentModel.DataAnnotations;
using AppaRently.App.DTOs.Apartments;
using AppaRently.App.DTOs.Users;

namespace AppaRently.Web.Owner.Models;

public class OwnerReportPeriodViewModel
{
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [DataType(DataType.Date)]
    public DateTime? To { get; set; }
}

public class OwnerDashboardQueryViewModel : OwnerReportPeriodViewModel
{
    public string? City { get; set; }

    public string? Department { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public bool DeletedOnly { get; set; }
}

public sealed class OwnerMetricsViewModel
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

public sealed class OwnerDashboardViewModel : OwnerDashboardQueryViewModel
{
    public IReadOnlyList<ApartmentResponse> Apartments { get; set; } = Array.Empty<ApartmentResponse>();

    public OwnerMetricsViewModel Metrics { get; set; } = new();
}

public sealed class OwnerCalendarDayViewModel
{
    public DateTime Date { get; set; }

    public bool IsBooked { get; set; }

    public bool IsToday { get; set; }
}

public sealed class OwnerReservationRecordViewModel
{
    public Guid ReservationId { get; set; }

    public string ApartmentTitle { get; set; } = string.Empty;

    public string ApartmentAddress { get; set; } = string.Empty;

    public string ApartmentCity { get; set; } = string.Empty;

    public string ApartmentDepartment { get; set; } = string.Empty;

    public string TenantFullName { get; set; } = string.Empty;

    public string TenantEmail { get; set; } = string.Empty;

    public DateTime CheckIn { get; set; }

    public DateTime CheckOut { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal PricePaid { get; set; }
}

public sealed class OwnerApartmentDetailViewModel : OwnerReportPeriodViewModel
{
    public ApartmentResponse Apartment { get; set; } = new();

    public int FavoriteCount { get; set; }

    public IReadOnlyList<OwnerCalendarDayViewModel> CalendarDays { get; set; } = Array.Empty<OwnerCalendarDayViewModel>();

    public OwnerMetricsViewModel Metrics { get; set; } = new();

    public IReadOnlyList<OwnerReservationRecordViewModel> Reservations { get; set; } = Array.Empty<OwnerReservationRecordViewModel>();
}

public sealed class OwnerProfileViewModel
{
    public UserResponse Owner { get; set; } = new();

    public IReadOnlyList<OwnerReservationRecordViewModel> Reservations { get; set; } = Array.Empty<OwnerReservationRecordViewModel>();
}
