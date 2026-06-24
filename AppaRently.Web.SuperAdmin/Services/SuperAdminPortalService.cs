using AppaRently.App.DTOs.Apartments;
using AppaRently.App.DTOs.Reservations;
using AppaRently.App.DTOs.Users;
using AppaRently.App.Interfaces;
using AppaRently.Domain.Models;
using AppaRently.Infrastructure.Data;
using AppaRently.Web.SuperAdmin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AppaRently.Web.SuperAdmin.Services;

public sealed class SuperAdminPortalService : ISuperAdminPortalService
{
    private const int CalendarWindowDays = 30;
    private const int DefaultReportDays = 30;

    private readonly AppaRentlyDbContext _dbContext;
    private readonly IApartmentService _apartmentService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SuperAdminPortalService(
        AppaRentlyDbContext dbContext,
        IApartmentService apartmentService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _apartmentService = apartmentService;
        _userManager = userManager;
    }

    public async Task<SuperAdminUserDashboardViewModel> BuildUserDashboardAsync(
        SuperAdminUserDashboardQueryViewModel query,
        CancellationToken cancellationToken = default)
    {
        var users = await LoadUsersAsync(query.DeletedOnly, cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            users = users
                .Where(x => string.Equals(x.Role, query.Role.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return new SuperAdminUserDashboardViewModel
        {
            Role = query.Role,
            DeletedOnly = query.DeletedOnly,
            Users = users.OrderBy(x => x.Role).ThenBy(x => x.FullName).ToList()
        };
    }

    public async Task<SuperAdminApartmentDashboardViewModel> BuildApartmentDashboardAsync(
        SuperAdminApartmentDashboardQueryViewModel query,
        CancellationToken cancellationToken = default)
    {
        var reportRange = NormalizeRange(query.From, query.To);
        var apartments = query.DeletedOnly
            ? await _apartmentService.GetDeletedAsync(new ApartmentSearchRequest
            {
                City = query.City,
                Department = query.Department,
                MinPrice = query.MinPrice,
                MaxPrice = query.MaxPrice
            }, cancellationToken)
            : await _apartmentService.GetAllAsync(new ApartmentSearchRequest
            {
                City = query.City,
                Department = query.Department,
                MinPrice = query.MinPrice,
                MaxPrice = query.MaxPrice
            }, cancellationToken);

        var apartmentList = apartments.Data?.ToList() ?? new List<ApartmentResponse>();
        var reservationApartmentIds = apartmentList.Select(x => x.Id).ToList();
        var reservations = await LoadReservationsForApartmentsAsync(
            reservationApartmentIds,
            reportRange.start,
            reportRange.endExclusive,
            cancellationToken);
        var metrics = BuildApartmentMetrics(
            apartmentList,
            reservations,
            reportRange.start,
            reportRange.endExclusive);

        return new SuperAdminApartmentDashboardViewModel
        {
            City = query.City,
            Department = query.Department,
            MinPrice = query.MinPrice,
            MaxPrice = query.MaxPrice,
            DeletedOnly = query.DeletedOnly,
            From = reportRange.start,
            To = reportRange.endExclusive.AddDays(-1),
            Apartments = apartmentList,
            Metrics = metrics
        };
    }

    public async Task<SuperAdminUserDetailViewModel?> BuildUserDetailAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var role = await GetRoleAsync(user, cancellationToken);
        if (string.IsNullOrWhiteSpace(role) || string.Equals(role, AppaRentlyRoles.SuperAdmin, StringComparison.Ordinal))
        {
            return null;
        }

        var reservations = await LoadReservationsForUserAsync(userId, cancellationToken);

        if (string.Equals(role, AppaRentlyRoles.Client, StringComparison.Ordinal))
        {
            return new SuperAdminUserDetailViewModel
            {
                User = MapToUserResponse(user, role),
                Role = role,
                TotalMoneyUsed = reservations.Sum(x => x.PricePaid),
                Reservations = reservations
            };
        }

        var ownedApartments = await LoadOwnedApartmentsAsync(userId, cancellationToken);
        return new SuperAdminUserDetailViewModel
        {
            User = MapToUserResponse(user, role),
            Role = role,
            TotalMoneyGenerated = ownedApartments.Sum(x => x.GeneratedAmount),
            Apartments = ownedApartments
        };
    }

    public async Task<SuperAdminApartmentDetailViewModel?> BuildApartmentDetailAsync(
        Guid apartmentId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var apartmentResponse = await _apartmentService.GetByIdAsync(apartmentId, cancellationToken);
        if (!apartmentResponse.Success || apartmentResponse.Data is null)
        {
            return null;
        }

        var reportRange = NormalizeRange(from, to);
        var filteredReservations = await _dbContext.Reservations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Apartment)
            .Where(x => x.ApartmentId == apartmentId && x.DeletedAt == null)
            .OrderByDescending(x => x.CheckIn)
            .ToListAsync(cancellationToken);

        var favoriteCount = await _dbContext.Favorites
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(x => x.ApartmentId == apartmentId && x.DeletedAt == null, cancellationToken);

        var bookedRanges = filteredReservations
            .Where(x => x.DeletedAt is null)
            .Select(x => (Start: x.CheckIn.Date, End: x.CheckOut.Date))
            .ToList();

        var metrics = BuildApartmentMetrics(
            new[] { apartmentResponse.Data },
            filteredReservations,
            reportRange.start,
            reportRange.endExclusive);

        return new SuperAdminApartmentDetailViewModel
        {
            Apartment = apartmentResponse.Data,
            FavoriteCount = favoriteCount,
            OwnerFullName = apartmentResponse.Data.OwnerFullName,
            From = reportRange.start,
            To = reportRange.endExclusive.AddDays(-1),
            CalendarDays = BuildCalendar(bookedRanges),
            Revenue = metrics.Revenue,
            PotentialRevenue = metrics.PotentialRevenue,
            AverageReservationValue = metrics.AverageReservationValue,
            AverageStayDays = metrics.AverageStayDays,
            ProfitabilityRate = metrics.ProfitabilityRate,
            OccupancyRate = metrics.OccupancyRate,
            BookedDays = metrics.BookedDays,
            UniqueTenantCount = metrics.UniqueTenantCount,
            AvailableDays = metrics.AvailableDays,
            ReservationCount = metrics.ReservationCount
        };
    }

    public async Task<SuperAdminProfileViewModel?> BuildProfileAsync(
        string superAdminId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == superAdminId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var role = await GetRoleAsync(user, cancellationToken);
        if (!string.Equals(role, AppaRentlyRoles.SuperAdmin, StringComparison.Ordinal))
        {
            return null;
        }

        var canDelete = await HasAnotherSuperAdminAsync(superAdminId, cancellationToken);

        return new SuperAdminProfileViewModel
        {
            SuperAdmin = MapToUserResponse(user, role),
            CanDeleteAccount = canDelete
        };
    }

    public async Task<bool> HasAnotherSuperAdminAsync(
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Id != currentUserId && x.DeletedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            var role = await GetRoleAsync(user, cancellationToken);
            if (string.Equals(role, AppaRentlyRoles.SuperAdmin, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<List<SuperAdminUserDashboardItemViewModel>> LoadUsersAsync(
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var items = new List<SuperAdminUserDashboardItemViewModel>();
        foreach (var user in users)
        {
            if (!includeDeleted && user.DeletedAt is not null)
            {
                continue;
            }

            var role = await GetRoleAsync(user, cancellationToken);
            if (string.IsNullOrWhiteSpace(role) || string.Equals(role, AppaRentlyRoles.SuperAdmin, StringComparison.Ordinal))
            {
                continue;
            }

            items.Add(new SuperAdminUserDashboardItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = role,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                DeletedAt = user.DeletedAt
            });
        }

        return items;
    }

    private async Task<string> GetRoleAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault() ?? string.Empty;
    }

    private async Task<List<SuperAdminUserReservationItemViewModel>> LoadReservationsForUserAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var reservations = await _dbContext.Reservations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.Apartment)
            .Where(x => x.UserId == userId && x.DeletedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return reservations
            .Select(x => new SuperAdminUserReservationItemViewModel
            {
                ReservationId = x.Id,
                ApartmentTitle = x.Apartment?.Title ?? string.Empty,
                ApartmentAddress = x.Apartment?.Address ?? string.Empty,
                CheckIn = x.CheckIn,
                CheckOut = x.CheckOut,
                CreatedAt = x.CreatedAt,
                PricePaid = CalculatePricePaid(x.CheckIn, x.CheckOut, x.Apartment?.Price ?? 0m)
            })
            .ToList();
    }

    private async Task<List<SuperAdminOwnedApartmentItemViewModel>> LoadOwnedApartmentsAsync(
        string ownerId,
        CancellationToken cancellationToken)
    {
        var apartments = await _dbContext.Apartments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.OwnerId == ownerId)
            .OrderBy(x => x.City)
            .ThenBy(x => x.Department)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        var reservations = await _dbContext.Reservations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var items = new List<SuperAdminOwnedApartmentItemViewModel>();
        foreach (var apartment in apartments)
        {
            var apartmentReservations = reservations.Where(x => x.ApartmentId == apartment.Id).ToList();
            var generated = apartmentReservations.Sum(x => CalculatePricePaid(x.CheckIn, x.CheckOut, apartment.Price));
            items.Add(new SuperAdminOwnedApartmentItemViewModel
            {
                ApartmentId = apartment.Id,
                Title = apartment.Title,
                Address = apartment.Address,
                City = apartment.City,
                Department = apartment.Department,
                DailyPrice = apartment.Price,
                GeneratedAmount = generated,
                ReservationCount = apartmentReservations.Count
            });
        }

        return items;
    }

    private async Task<List<Reservation>> LoadReservationsForApartmentsAsync(
        IReadOnlyCollection<Guid> apartmentIds,
        DateTime start,
        DateTime endExclusive,
        CancellationToken cancellationToken)
    {
        if (apartmentIds.Count == 0)
        {
            return new List<Reservation>();
        }

        return await _dbContext.Reservations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Apartment)
            .Where(x =>
                x.DeletedAt == null &&
                apartmentIds.Contains(x.ApartmentId) &&
                x.CheckIn < endExclusive &&
                x.CheckOut > start)
            .OrderByDescending(x => x.CheckIn)
            .ToListAsync(cancellationToken);
    }

    private static decimal CalculatePricePaid(DateTime checkIn, DateTime checkOut, decimal dailyPrice)
    {
        var stayDays = Math.Max(1, (checkOut.Date - checkIn.Date).Days);
        return stayDays * dailyPrice;
    }

    private static SuperAdminApartmentMetricsViewModel BuildApartmentMetrics(
        IReadOnlyCollection<ApartmentResponse> apartments,
        IReadOnlyCollection<Reservation> reservations,
        DateTime start,
        DateTime endExclusive)
    {
        var periodDays = Math.Max(1, (endExclusive.Date - start.Date).Days);
        var apartmentCount = apartments.Count;
        var availableDays = apartmentCount * periodDays;
        var bookedDays = reservations.Sum(reservation =>
        {
            var bookedStart = reservation.CheckIn.Date > start.Date ? reservation.CheckIn.Date : start.Date;
            var bookedEnd = reservation.CheckOut.Date < endExclusive.Date ? reservation.CheckOut.Date : endExclusive.Date;
            return Math.Max(0, (bookedEnd - bookedStart).Days);
        });

        var revenue = reservations.Sum(reservation =>
        {
            var bookedStart = reservation.CheckIn.Date > start.Date ? reservation.CheckIn.Date : start.Date;
            var bookedEnd = reservation.CheckOut.Date < endExclusive.Date ? reservation.CheckOut.Date : endExclusive.Date;
            var days = Math.Max(0, (bookedEnd - bookedStart).Days);
            return days * (reservation.Apartment?.Price ?? 0m);
        });

        var potentialRevenue = apartments.Sum(apartment => apartment.Price * periodDays);
        var reservationCount = reservations.Count;
        var averageReservationValue = reservationCount == 0 ? 0m : revenue / reservationCount;
        var averageStayDays = reservationCount == 0 ? 0m : (decimal)bookedDays / reservationCount;
        var uniqueTenantCount = reservations
            .Select(x => x.UserId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return new SuperAdminApartmentMetricsViewModel
        {
            Revenue = revenue,
            PotentialRevenue = potentialRevenue,
            AverageReservationValue = averageReservationValue,
            AverageStayDays = averageStayDays,
            AvailableDays = availableDays,
            BookedDays = bookedDays,
            OccupancyRate = availableDays == 0 ? 0 : (decimal)bookedDays / availableDays * 100m,
            ProfitabilityRate = potentialRevenue == 0 ? 0 : revenue / potentialRevenue * 100m,
            ReservationCount = reservationCount,
            UniqueTenantCount = uniqueTenantCount
        };
    }

    private static IReadOnlyList<SuperAdminCalendarDayViewModel> BuildCalendar(IReadOnlyCollection<(DateTime Start, DateTime End)> bookedRanges)
    {
        var startDate = DateTime.Today;
        var calendar = new List<SuperAdminCalendarDayViewModel>(CalendarWindowDays);

        for (var index = 0; index < CalendarWindowDays; index++)
        {
            var date = startDate.AddDays(index);
            var isBooked = bookedRanges.Any(range => date >= range.Start && date < range.End);

            calendar.Add(new SuperAdminCalendarDayViewModel
            {
                Date = date,
                IsBooked = isBooked,
                IsToday = date.Date == DateTime.Today
            });
        }

        return calendar;
    }

    private static (DateTime start, DateTime endExclusive) NormalizeRange(DateTime? from, DateTime? to)
    {
        var endInclusive = (to ?? DateTime.UtcNow.Date).Date;
        var start = (from ?? endInclusive.AddDays(-(DefaultReportDays - 1))).Date;

        if (endInclusive < start)
        {
            (start, endInclusive) = (endInclusive, start);
        }

        return (start, endInclusive.AddDays(1));
    }

    private static UserResponse MapToUserResponse(ApplicationUser user, string role) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email ?? string.Empty,
        Role = role,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        DeletedAt = user.DeletedAt
    };
}
