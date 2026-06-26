using AppaRently.App.DTOs.Apartments;
using AppaRently.App.DTOs.Reservations;
using AppaRently.App.DTOs.Users;
using AppaRently.App.Interfaces;
using AppaRently.Domain.Models;
using AppaRently.Infrastructure.Data;
using AppaRently.Web.Owner.Models;
using Microsoft.EntityFrameworkCore;

namespace AppaRently.Web.Owner.Services;

public sealed class OwnerPortalService : IOwnerPortalService
{
    private const int CalendarWindowDays = 30;
    private const int DefaultReportDays = 30;

    private readonly AppaRentlyDbContext _dbContext;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IApartmentService _apartmentService;
    private readonly IUserService _userService;

    public OwnerPortalService(
        AppaRentlyDbContext dbContext,
        IEmailNotificationService emailNotificationService,
        IApartmentService apartmentService,
        IUserService userService)
    {
        _dbContext = dbContext;
        _emailNotificationService = emailNotificationService;
        _apartmentService = apartmentService;
        _userService = userService;
    }

    public async Task<OwnerDashboardViewModel> BuildDashboardAsync(
        string ownerId,
        OwnerDashboardQueryViewModel query,
        CancellationToken cancellationToken = default)
    {
        var apartments = await LoadApartmentsAsync(ownerId, query, cancellationToken);
        var reportRange = NormalizeRange(query.From, query.To);
        var reservations = await LoadReservationsAsync(
            apartmentIds: apartments.Select(x => x.Id).ToList(),
            ownerId: ownerId,
            includeDeletedApartments: query.DeletedOnly,
            reportRange.start,
            reportRange.endExclusive,
            cancellationToken);

        return new OwnerDashboardViewModel
        {
            City = query.City,
            Department = query.Department,
            MinPrice = query.MinPrice,
            MaxPrice = query.MaxPrice,
            DeletedOnly = query.DeletedOnly,
            From = reportRange.start,
            To = reportRange.endInclusive,
            Apartments = apartments,
            Metrics = BuildMetrics(apartments, reservations, reportRange.start, reportRange.endExclusive)
        };
    }

    public async Task<OwnerApartmentDetailViewModel?> BuildApartmentDetailAsync(
        string ownerId,
        Guid apartmentId,
        OwnerReportPeriodViewModel query,
        CancellationToken cancellationToken = default)
    {
        var apartmentResponse = await _apartmentService.GetByIdByOwnerIdAsync(apartmentId, ownerId, cancellationToken);
        if (!apartmentResponse.Success || apartmentResponse.Data is null)
        {
            return null;
        }

        var reportRange = NormalizeRange(query.From, query.To);
        var reservations = await LoadReservationsAsync(
            new[] { apartmentId },
            ownerId,
            includeDeletedApartments: false,
            reportRange.start,
            reportRange.endExclusive,
            cancellationToken);

        var allReservations = await LoadReservationsAsync(
            new[] { apartmentId },
            ownerId,
            includeDeletedApartments: false,
            start: null,
            endExclusive: null,
            cancellationToken);

        var favoriteCount = await _dbContext.Favorites
            .AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(x => x.ApartmentId == apartmentId && x.DeletedAt == null, cancellationToken);

        return new OwnerApartmentDetailViewModel
        {
            Apartment = apartmentResponse.Data,
            FavoriteCount = favoriteCount,
            From = reportRange.start,
            To = reportRange.endInclusive,
            Metrics = BuildMetrics(
                new[] { apartmentResponse.Data },
                reservations,
                reportRange.start,
                reportRange.endExclusive),
            CalendarDays = BuildCalendar(allReservations),
            Reservations = reservations.Select(MapToRecord).ToList()
        };
    }

    public async Task<OwnerProfileViewModel?> BuildProfileAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        var userResponse = await _userService.GetByIdAsync(ownerId, cancellationToken);
        if (!userResponse.Success || userResponse.Data is null)
        {
            return null;
        }

        var reservations = await LoadReservationsForOwnerAsync(ownerId, cancellationToken);

        return new OwnerProfileViewModel
        {
            Owner = userResponse.Data,
            Reservations = reservations
                .OrderByDescending(x => x.CreatedAt)
                .Select(MapToRecord)
                .ToList()
        };
    }

    public async Task<byte[]> ExportDashboardAsync(
        string ownerId,
        OwnerDashboardQueryViewModel query,
        CancellationToken cancellationToken = default)
    {
        var apartments = await LoadApartmentsAsync(ownerId, query, cancellationToken);
        var reportRange = NormalizeRange(query.From, query.To);
        var reservations = await LoadReservationsAsync(
            apartments.Select(x => x.Id).ToList(),
            ownerId,
            includeDeletedApartments: query.DeletedOnly,
            reportRange.start,
            reportRange.endExclusive,
            cancellationToken);

        var bytes = BuildWorkbook(reservations.Select(MapToRecord).ToList(), "Owner report");
        await SendReportEmailAsync(
            ownerId,
            "AppaRently owner revenue report",
            "Attached is the revenue report generated from your dashboard.",
            bytes,
            $"owner-report-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx",
            cancellationToken);

        return bytes;
    }

    public async Task<byte[]> ExportApartmentAsync(
        string ownerId,
        Guid apartmentId,
        OwnerReportPeriodViewModel query,
        CancellationToken cancellationToken = default)
    {
        var apartmentResponse = await _apartmentService.GetByIdByOwnerIdAsync(apartmentId, ownerId, cancellationToken);
        if (!apartmentResponse.Success || apartmentResponse.Data is null)
        {
            return BuildWorkbook(Array.Empty<OwnerReservationRecordViewModel>(), "Apartment report");
        }

        var reportRange = NormalizeRange(query.From, query.To);
        var reservations = await LoadReservationsAsync(
            new[] { apartmentId },
            ownerId,
            includeDeletedApartments: false,
            reportRange.start,
            reportRange.endExclusive,
            cancellationToken);

        var bytes = BuildWorkbook(reservations.Select(MapToRecord).ToList(), apartmentResponse.Data.Title);
        await SendReportEmailAsync(
            ownerId,
            $"AppaRently report for {apartmentResponse.Data.Title}",
            "Attached is the apartment revenue report you requested.",
            bytes,
            $"apartment-report-{apartmentId:N}-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx",
            cancellationToken);

        return bytes;
    }

    private async Task<List<ApartmentResponse>> LoadApartmentsAsync(
        string ownerId,
        OwnerDashboardQueryViewModel query,
        CancellationToken cancellationToken)
    {
        var search = new ApartmentSearchRequest
        {
            City = query.City,
            Department = query.Department,
            MinPrice = query.MinPrice,
            MaxPrice = query.MaxPrice,
            OwnerId = ownerId
        };

        var response = query.DeletedOnly
            ? await _apartmentService.GetDeletedByOwnerIdAsync(ownerId, search, cancellationToken)
            : await _apartmentService.GetByOwnerIdAsync(ownerId, search, cancellationToken);

        return response.Data?.ToList() ?? new List<ApartmentResponse>();
    }

    private async Task<List<Reservation>> LoadReservationsForOwnerAsync(
        string ownerId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Reservations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Apartment)
            .Where(x => x.DeletedAt == null && x.Apartment != null && x.Apartment.OwnerId == ownerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Reservation>> LoadReservationsAsync(
        IReadOnlyCollection<Guid> apartmentIds,
        string ownerId,
        bool includeDeletedApartments,
        DateTime? start,
        DateTime? endExclusive,
        CancellationToken cancellationToken)
    {
        if (apartmentIds.Count == 0)
        {
            return new List<Reservation>();
        }

        var query = _dbContext.Reservations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Apartment)
            .Where(x => x.DeletedAt == null && x.Apartment != null && x.Apartment.OwnerId == ownerId);

        if (apartmentIds.Count > 0)
        {
            query = query.Where(x => apartmentIds.Contains(x.ApartmentId));
        }

        if (start.HasValue && endExclusive.HasValue)
        {
            query = query.Where(x => x.CheckIn < endExclusive.Value && x.CheckOut > start.Value);
        }

        if (!includeDeletedApartments)
        {
            query = query.Where(x => x.Apartment!.DeletedAt == null);
        }

        return await query
            .OrderByDescending(x => x.CheckIn)
            .ToListAsync(cancellationToken);
    }

    private static OwnerMetricsViewModel BuildMetrics(
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
            if (reservation.Apartment is null)
            {
                return 0;
            }

            var bookedStart = reservation.CheckIn.Date > start.Date ? reservation.CheckIn.Date : start.Date;
            var bookedEnd = reservation.CheckOut.Date < endExclusive.Date ? reservation.CheckOut.Date : endExclusive.Date;
            return Math.Max(0, (bookedEnd - bookedStart).Days);
        });

        var revenue = reservations.Sum(reservation =>
        {
            if (reservation.Apartment is null)
            {
                return 0m;
            }

            var bookedStart = reservation.CheckIn.Date > start.Date ? reservation.CheckIn.Date : start.Date;
            var bookedEnd = reservation.CheckOut.Date < endExclusive.Date ? reservation.CheckOut.Date : endExclusive.Date;
            var days = Math.Max(0, (bookedEnd - bookedStart).Days);
            return days * reservation.Apartment.Price;
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

        return new OwnerMetricsViewModel
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

    private static IReadOnlyList<OwnerCalendarDayViewModel> BuildCalendar(IReadOnlyCollection<Reservation> reservations)
    {
        var bookedRanges = reservations
            .Select(x => (Start: x.CheckIn.Date, End: x.CheckOut.Date))
            .ToList();

        var startDate = DateTime.Today;
        var calendar = new List<OwnerCalendarDayViewModel>(CalendarWindowDays);

        for (var index = 0; index < CalendarWindowDays; index++)
        {
            var date = startDate.AddDays(index);
            var isBooked = bookedRanges.Any(range => date >= range.Start && date < range.End);

            calendar.Add(new OwnerCalendarDayViewModel
            {
                Date = date,
                IsBooked = isBooked,
                IsToday = date.Date == DateTime.Today
            });
        }

        return calendar;
    }

    private static OwnerReservationRecordViewModel MapToRecord(Reservation reservation)
    {
        var apartmentPrice = reservation.Apartment?.Price ?? 0m;
        var stayDays = Math.Max(1, (reservation.CheckOut.Date - reservation.CheckIn.Date).Days);

        return new OwnerReservationRecordViewModel
        {
            ReservationId = reservation.Id,
            ApartmentTitle = reservation.Apartment?.Title ?? string.Empty,
            ApartmentAddress = reservation.Apartment?.Address ?? string.Empty,
            ApartmentCity = reservation.Apartment?.City ?? string.Empty,
            ApartmentDepartment = reservation.Apartment?.Department ?? string.Empty,
            TenantFullName = reservation.User?.FullName ?? string.Empty,
            TenantEmail = reservation.User?.Email ?? string.Empty,
            CheckIn = reservation.CheckIn,
            CheckOut = reservation.CheckOut,
            CreatedAt = reservation.CreatedAt,
            PricePaid = stayDays * apartmentPrice
        };
    }

    private static byte[] BuildWorkbook(IReadOnlyList<OwnerReservationRecordViewModel> rows, string sheetName)
    {
        var safeSheetName = SanitizeSheetName(sheetName);
        var headers = new[]
        {
            "Reservation ID",
            "Check-in",
            "Check-out",
            "Created at",
            "Price paid",
            "Tenant name",
            "Tenant email",
            "Apartment",
            "Address",
            "City",
            "Department"
        };

        var data = rows
            .Select(row => (IReadOnlyList<object?>)new object?[]
            {
                row.ReservationId,
                row.CheckIn,
                row.CheckOut,
                row.CreatedAt,
                row.PricePaid,
                row.TenantFullName,
                row.TenantEmail,
                row.ApartmentTitle,
                row.ApartmentAddress,
                row.ApartmentCity,
                row.ApartmentDepartment
            })
            .ToList();

        return XlsxReportWriter.Write(safeSheetName, headers, data);
    }

    private static string SanitizeSheetName(string name)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var sanitized = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        sanitized = sanitized.Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "Report";
        }

        return sanitized.Length > 31 ? sanitized[..31] : sanitized;
    }

    private async Task SendReportEmailAsync(
        string ownerId,
        string subject,
        string body,
        byte[] fileBytes,
        string fileName,
        CancellationToken cancellationToken)
    {
        var ownerResponse = await _userService.GetByIdAsync(ownerId, cancellationToken);
        if (!ownerResponse.Success || ownerResponse.Data is null || string.IsNullOrWhiteSpace(ownerResponse.Data.Email))
        {
            return;
        }

        await _emailNotificationService.SendAsync(
            ownerResponse.Data.Email,
            subject,
            body,
            new[]
            {
                new EmailAttachment(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileBytes)
            },
            cancellationToken);
    }

    private static (DateTime start, DateTime endInclusive, DateTime endExclusive) NormalizeRange(DateTime? from, DateTime? to)
    {
        var endInclusive = AsUtcDate(to ?? DateTime.UtcNow.Date);
        var start = AsUtcDate(from ?? endInclusive.AddDays(-(DefaultReportDays - 1)));

        if (endInclusive < start)
        {
            (start, endInclusive) = (endInclusive, start);
        }

        return (start, endInclusive, endInclusive.AddDays(1));
    }

    private static DateTime AsUtcDate(DateTime value)
    {
        return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }
}
