using AppaRently.App.DTOs.Reservations;
using AppaRently.App.Interfaces;
using AppaRently.App.ServiceResponse;
using AppaRently.Domain.Models;
using AppaRently.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppaRently.Infrastructure.Services;

public sealed class ReservationService : IReservationService
{
    private readonly AppaRentlyDbContext _dbContext;
    private readonly IAppNotificationService _appNotificationService;
    private readonly IEmailNotificationService _emailNotificationService;

    public ReservationService(
        AppaRentlyDbContext dbContext,
        IAppNotificationService appNotificationService,
        IEmailNotificationService emailNotificationService)
    {
        _dbContext = dbContext;
        _appNotificationService = appNotificationService;
        _emailNotificationService = emailNotificationService;
    }

    public async Task<ServiceResponse<IEnumerable<ReservationResponse>>> GetAllAsync(ReservationSearchRequest? request = null, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<IEnumerable<ReservationResponse>>();
        try
        {
            var query = _dbContext.Reservations
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Apartment)
                .AsQueryable();

            query = ApplyFilters(query, request);

            var reservations = await query
                .OrderByDescending(x => x.CheckIn)
                .ToListAsync(cancellationToken);

            response.Data = reservations.Select(MapToDto).ToList();
            response.Success = true;
            response.Message = "Reservations retrieved successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error retrieving reservations: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<ReservationResponse>> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<ReservationResponse>();
        try
        {
            var reservation = await _dbContext.Reservations
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Apartment)
                .FirstOrDefaultAsync(x => x.Id == reservationId, cancellationToken);

            if (reservation is null)
            {
                response.Success = false;
                response.Message = $"Reservation with Id {reservationId} not found";
                return response;
            }

            response.Data = MapToDto(reservation);
            response.Success = true;
            response.Message = "Reservation retrieved successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error retrieving reservation: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<ReservationResponse>> CreateAsync(string userId, CreateReservationRequest request, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<ReservationResponse>();
        try
        {
            var normalizedDates = ReservationDateRules.NormalizeStay(request.CheckIn, request.CheckOut);
            var validationError = ValidateReservationDates(normalizedDates.CheckIn, normalizedDates.CheckOut);
            if (validationError is not null)
            {
                response.Success = false;
                response.Message = validationError;
                return response;
            }

            var user = await _dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == userId && x.DeletedAt == null, cancellationToken);

            if (user is null)
            {
                response.Success = false;
                response.Message = $"User with Id {userId} not found";
                return response;
            }

            var apartment = await _dbContext.Apartments
                .AsNoTracking()
                .Include(x => x.Owner)
                .FirstOrDefaultAsync(x => x.Id == request.ApartmentId, cancellationToken);

            if (apartment is null)
            {
                response.Success = false;
                response.Message = $"Apartment with Id {request.ApartmentId} not found";
                return response;
            }

            var overlaps = await _dbContext.Reservations
                .AsNoTracking()
                .AnyAsync(x =>
                    x.ApartmentId == request.ApartmentId &&
                    x.DeletedAt == null &&
                    x.CheckIn < normalizedDates.CheckOut &&
                    x.CheckOut > normalizedDates.CheckIn,
                    cancellationToken);

            if (overlaps)
            {
                response.Success = false;
                response.Message = "Apartment is already reserved for the selected dates";
                return response;
            }

            var reservation = new Reservation
            {
                UserId = userId,
                ApartmentId = request.ApartmentId,
                CheckIn = normalizedDates.CheckIn,
                CheckOut = normalizedDates.CheckOut,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Reservations.Add(reservation);
            await _dbContext.SaveChangesAsync(cancellationToken);

            reservation.User = user;
            reservation.Apartment = apartment;

            await SendReservationCreatedEmailsAsync(reservation, cancellationToken);
            await SendReservationCreatedNotificationsAsync(reservation, cancellationToken);

            response.Data = MapToDto(reservation);
            response.Success = true;
            response.Message = "Reservation created successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error creating reservation: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<ReservationResponse>> UpdateAsync(Guid reservationId, string userId, UpdateReservationRequest request, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<ReservationResponse>();
        try
        {
            var normalizedDates = ReservationDateRules.NormalizeStay(request.CheckIn, request.CheckOut);
            var validationError = ValidateReservationDates(normalizedDates.CheckIn, normalizedDates.CheckOut);
            if (validationError is not null)
            {
                response.Success = false;
                response.Message = validationError;
                return response;
            }

            var reservation = await _dbContext.Reservations
                .Include(x => x.User)
                .Include(x => x.Apartment)
                .FirstOrDefaultAsync(x => x.Id == reservationId, cancellationToken);

            if (reservation is null)
            {
                response.Success = false;
                response.Message = $"Reservation with Id {reservationId} not found";
                return response;
            }

            if (!string.Equals(reservation.UserId, userId, StringComparison.Ordinal))
            {
                response.Success = false;
                response.Message = "You are not allowed to update this reservation";
                return response;
            }

            var overlaps = await _dbContext.Reservations
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id != reservationId &&
                    x.ApartmentId == reservation.ApartmentId &&
                    x.DeletedAt == null &&
                    x.CheckIn < normalizedDates.CheckOut &&
                    x.CheckOut > normalizedDates.CheckIn,
                    cancellationToken);

            if (overlaps)
            {
                response.Success = false;
                response.Message = "Apartment is already reserved for the selected dates";
                return response;
            }

            reservation.CheckIn = normalizedDates.CheckIn;
            reservation.CheckOut = normalizedDates.CheckOut;
            reservation.UpdatedAt = DateTime.UtcNow;
            reservation.ReminderBeforeStartSentAt = null;
            reservation.ReminderBeforeEndSentAt = null;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await SendReservationUpdatedNotificationsAsync(reservation, cancellationToken);

            response.Data = MapToDto(reservation);
            response.Success = true;
            response.Message = "Reservation updated successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error updating reservation: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(Guid reservationId, string userId, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<bool>();
        try
        {
            var reservation = await _dbContext.Reservations
                .Include(x => x.User)
                .Include(x => x.Apartment)
                    .ThenInclude(x => x!.Owner)
                .FirstOrDefaultAsync(x => x.Id == reservationId, cancellationToken);

            if (reservation is null)
            {
                response.Success = false;
                response.Data = false;
                response.Message = $"Reservation with Id {reservationId} not found";
                return response;
            }

            if (!string.Equals(reservation.UserId, userId, StringComparison.Ordinal))
            {
                response.Success = false;
                response.Data = false;
                response.Message = "You are not allowed to delete this reservation";
                return response;
            }

            if (reservation.CheckIn - DateTime.UtcNow < TimeSpan.FromHours(24))
            {
                response.Success = false;
                response.Data = false;
                response.Message = "Reservations can only be cancelled at least 24 hours before check-in.";
                return response;
            }

            reservation.DeletedAt = DateTime.UtcNow;
            reservation.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            await SendReservationCancelledEmailsAsync(reservation, cancellationToken);
            await SendReservationCancelledNotificationsAsync(reservation, cancellationToken);

            response.Data = true;
            response.Success = true;
            response.Message = "Reservation deleted successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Data = false;
            response.Message = $"Error deleting reservation: {ex.Message}";
        }

        return response;
    }

    private static IQueryable<Reservation> ApplyFilters(IQueryable<Reservation> query, ReservationSearchRequest? request)
    {
        if (request is null)
        {
            return query;
        }

        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            var userId = request.UserId.Trim();
            query = query.Where(x => x.UserId == userId);
        }

        if (request.ApartmentId.HasValue)
        {
            query = query.Where(x => x.ApartmentId == request.ApartmentId.Value);
        }

        if (request.CheckInFrom.HasValue)
        {
            query = query.Where(x => x.CheckIn >= request.CheckInFrom.Value);
        }

        if (request.CheckInTo.HasValue)
        {
            query = query.Where(x => x.CheckIn <= request.CheckInTo.Value);
        }

        return query;
    }

    private static ReservationResponse MapToDto(Reservation reservation)
    {
        return new ReservationResponse
        {
            Id = reservation.Id,
            UserId = reservation.UserId,
            UserFullName = reservation.User?.FullName ?? string.Empty,
            ApartmentId = reservation.ApartmentId,
            ApartmentTitle = reservation.Apartment?.Title ?? string.Empty,
            ApartmentAddress = reservation.Apartment?.Address ?? string.Empty,
            CheckIn = reservation.CheckIn,
            CheckOut = reservation.CheckOut,
            CreatedAt = reservation.CreatedAt,
            UpdatedAt = reservation.UpdatedAt,
            DeletedAt = reservation.DeletedAt
        };
    }

    private async Task SendReservationCreatedEmailsAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        var pricePaid = CalculateReservationPrice(reservation);

        if (reservation.User?.Email is { Length: > 0 } clientEmail)
        {
            await _emailNotificationService.SendAsync(
                clientEmail,
                "Reservation confirmed",
                BuildReservationCreatedBody(reservation, pricePaid),
                cancellationToken: cancellationToken);
        }

        var ownerEmail = reservation.Apartment?.Owner?.Email;
        if (!string.IsNullOrWhiteSpace(ownerEmail))
        {
            await _emailNotificationService.SendAsync(
                ownerEmail,
                "New reservation received",
                BuildOwnerReservationCreatedBody(reservation, pricePaid),
                cancellationToken: cancellationToken);
        }
    }

    private async Task SendReservationCancelledEmailsAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        if (reservation.User?.Email is { Length: > 0 } clientEmail)
        {
            await _emailNotificationService.SendAsync(
                clientEmail,
                "Reservation cancelled",
                BuildReservationCancelledBody(reservation),
                cancellationToken: cancellationToken);
        }

        var ownerEmail = reservation.Apartment?.Owner?.Email;
        if (!string.IsNullOrWhiteSpace(ownerEmail))
        {
            await _emailNotificationService.SendAsync(
                ownerEmail,
                "Reservation cancelled by client",
                BuildOwnerReservationCancelledBody(reservation),
                cancellationToken: cancellationToken);
        }
    }

    private async Task SendReservationCreatedNotificationsAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(reservation.UserId))
        {
            await _appNotificationService.CreateAsync(
                reservation.UserId,
                "Reservation confirmed",
                BuildReservationCreatedBody(reservation, CalculateReservationPrice(reservation)),
                "reservation.created",
                $"/Reservation/Show/{reservation.Id}",
                cancellationToken);
        }

        if (reservation.Apartment?.OwnerId is { Length: > 0 } ownerId)
        {
            await _appNotificationService.CreateAsync(
                ownerId,
                "New reservation received",
                BuildOwnerReservationCreatedBody(reservation, CalculateReservationPrice(reservation)),
                "reservation.created",
                $"/Apartment/Detail/{reservation.ApartmentId}",
                cancellationToken);
        }
    }

    private async Task SendReservationUpdatedNotificationsAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(reservation.UserId))
        {
            await _appNotificationService.CreateAsync(
                reservation.UserId,
                "Reservation updated",
                BuildReservationUpdatedBody(reservation, CalculateReservationPrice(reservation)),
                "reservation.updated",
                $"/Reservation/Show/{reservation.Id}",
                cancellationToken);
        }

        if (reservation.Apartment?.OwnerId is { Length: > 0 } ownerId)
        {
            await _appNotificationService.CreateAsync(
                ownerId,
                "Reservation updated",
                BuildOwnerReservationUpdatedBody(reservation, CalculateReservationPrice(reservation)),
                "reservation.updated",
                $"/Apartment/Detail/{reservation.ApartmentId}",
                cancellationToken);
        }
    }

    private async Task SendReservationCancelledNotificationsAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(reservation.UserId))
        {
            await _appNotificationService.CreateAsync(
                reservation.UserId,
                "Reservation cancelled",
                BuildReservationCancelledBody(reservation),
                "reservation.cancelled",
                $"/Reservation/Show/{reservation.Id}",
                cancellationToken);
        }

        if (reservation.Apartment?.OwnerId is { Length: > 0 } ownerId)
        {
            await _appNotificationService.CreateAsync(
                ownerId,
                "Reservation cancelled",
                BuildOwnerReservationCancelledBody(reservation),
                "reservation.cancelled",
                $"/Apartment/Detail/{reservation.ApartmentId}",
                cancellationToken);
        }
    }

    private static decimal CalculateReservationPrice(Reservation reservation)
    {
        var apartmentPrice = reservation.Apartment?.Price ?? 0m;
        var stayDays = Math.Max(1, (reservation.CheckOut.Date - reservation.CheckIn.Date).Days);
        return stayDays * apartmentPrice;
    }

    private static string BuildReservationCreatedBody(Reservation reservation, decimal pricePaid)
    {
        return
            $"Hello {reservation.User?.FullName},{Environment.NewLine}{Environment.NewLine}" +
            $"Your reservation for {reservation.Apartment?.Title} has been confirmed.{Environment.NewLine}" +
            $"Check-in: {reservation.CheckIn:yyyy-MM-dd HH:mm}{Environment.NewLine}" +
            $"Check-out: {reservation.CheckOut:yyyy-MM-dd HH:mm}{Environment.NewLine}" +
            $"Total paid: {pricePaid:C}{Environment.NewLine}" +
            $"Address: {reservation.Apartment?.Address}";
    }

    private static string BuildOwnerReservationCreatedBody(Reservation reservation, decimal pricePaid)
    {
        return
            $"Hello,{Environment.NewLine}{Environment.NewLine}" +
            $"A new reservation was created for {reservation.Apartment?.Title}.{Environment.NewLine}" +
            $"Tenant: {reservation.User?.FullName} ({reservation.User?.Email}){Environment.NewLine}" +
            $"Check-in: {reservation.CheckIn:yyyy-MM-dd HH:mm}{Environment.NewLine}" +
            $"Check-out: {reservation.CheckOut:yyyy-MM-dd HH:mm}{Environment.NewLine}" +
            $"Expected revenue: {pricePaid:C}";
    }

    private static string BuildReservationCancelledBody(Reservation reservation)
    {
        return
            $"Hello {reservation.User?.FullName},{Environment.NewLine}{Environment.NewLine}" +
            $"Your reservation for {reservation.Apartment?.Title} has been cancelled successfully.{Environment.NewLine}" +
            $"Check-in: {reservation.CheckIn:yyyy-MM-dd HH:mm}{Environment.NewLine}" +
            $"Check-out: {reservation.CheckOut:yyyy-MM-dd HH:mm}";
    }

    private static string BuildReservationUpdatedBody(Reservation reservation, decimal pricePaid)
    {
        return
            $"Hello {reservation.User?.FullName},{Environment.NewLine}{Environment.NewLine}" +
            $"Your reservation for {reservation.Apartment?.Title} has been updated.{Environment.NewLine}" +
            $"Check-in: {reservation.CheckIn:yyyy-MM-dd HH:mm}{Environment.NewLine}" +
            $"Check-out: {reservation.CheckOut:yyyy-MM-dd HH:mm}{Environment.NewLine}" +
            $"Estimated total: {pricePaid:C}{Environment.NewLine}" +
            $"Address: {reservation.Apartment?.Address}";
    }

    private static string BuildOwnerReservationUpdatedBody(Reservation reservation, decimal pricePaid)
    {
        return
            $"Hello,{Environment.NewLine}{Environment.NewLine}" +
            $"A reservation was updated for {reservation.Apartment?.Title}.{Environment.NewLine}" +
            $"Tenant: {reservation.User?.FullName} ({reservation.User?.Email}){Environment.NewLine}" +
            $"Check-in: {reservation.CheckIn:yyyy-MM-dd HH:mm}{Environment.NewLine}" +
            $"Check-out: {reservation.CheckOut:yyyy-MM-dd HH:mm}{Environment.NewLine}" +
            $"Estimated revenue: {pricePaid:C}";
    }

    private static string BuildOwnerReservationCancelledBody(Reservation reservation)
    {
        return
            $"Hello,{Environment.NewLine}{Environment.NewLine}" +
            $"A reservation was cancelled for {reservation.Apartment?.Title}.{Environment.NewLine}" +
            $"Tenant: {reservation.User?.FullName} ({reservation.User?.Email}){Environment.NewLine}" +
            $"Check-in: {reservation.CheckIn:yyyy-MM-dd HH:mm}{Environment.NewLine}" +
            $"Check-out: {reservation.CheckOut:yyyy-MM-dd HH:mm}";
    }

    private static string? ValidateReservationDates(DateTime checkIn, DateTime checkOut)
    {
        if (checkIn == default || checkOut == default)
        {
            return "CheckIn and CheckOut are required.";
        }

        if (checkOut <= checkIn)
        {
            return "CheckOut must be greater than CheckIn.";
        }

        return null;
    }
}
