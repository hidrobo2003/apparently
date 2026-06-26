using AppaRently.App.DTOs.Apartments;
using AppaRently.App.Interfaces;
using AppaRently.App.ServiceResponse;
using AppaRently.Domain.Models;
using AppaRently.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AppaRently.Infrastructure.Services;

public sealed class ApartmentService : IApartmentService
{
    private readonly AppaRentlyDbContext _dbContext;
    private readonly IAppNotificationService _appNotificationService;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApartmentService(
        AppaRentlyDbContext dbContext,
        IAppNotificationService appNotificationService,
        IEmailNotificationService emailNotificationService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _appNotificationService = appNotificationService;
        _emailNotificationService = emailNotificationService;
        _userManager = userManager;
    }

    public async Task<ServiceResponse<IEnumerable<ApartmentResponse>>> GetAllAsync(ApartmentSearchRequest? request = null, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<IEnumerable<ApartmentResponse>>();
        try
        {
            var query = _dbContext.Apartments
                .AsNoTracking()
                .Include(x => x.Owner)
                .AsQueryable();

            query = ApplyFilters(query, request);

            var apartments = await query
                .OrderBy(x => x.City)
                .ThenBy(x => x.Department)
                .ThenBy(x => x.Title)
                .ToListAsync(cancellationToken);

            response.Data = apartments.Select(MapToDto).ToList();
            response.Success = true;
            response.Message = "Apartments retrieved successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error retrieving apartments: {ex.Message}";
        }

        return response;
    }

    public Task<ServiceResponse<IEnumerable<ApartmentResponse>>> GetByOwnerIdAsync(
        string ownerId,
        ApartmentSearchRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var ownerRequest = request is null
            ? new ApartmentSearchRequest { OwnerId = ownerId }
            : request with { OwnerId = ownerId };

        return GetAllAsync(ownerRequest, cancellationToken);
    }

    public Task<ServiceResponse<IEnumerable<ApartmentResponse>>> GetDeletedAsync(
        ApartmentSearchRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        return GetApartmentsAsync(includeDeleted: true, deletedOnly: true, request, cancellationToken);
    }

    public Task<ServiceResponse<IEnumerable<ApartmentResponse>>> GetDeletedByOwnerIdAsync(
        string ownerId,
        ApartmentSearchRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var ownerRequest = request is null
            ? new ApartmentSearchRequest { OwnerId = ownerId }
            : request with { OwnerId = ownerId };

        return GetApartmentsAsync(includeDeleted: true, deletedOnly: true, ownerRequest, cancellationToken);
    }

    public async Task<ServiceResponse<ApartmentResponse>> GetByIdAsync(Guid apartmentId, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<ApartmentResponse>();
        try
        {
            var apartment = await _dbContext.Apartments
                .AsNoTracking()
                .Include(x => x.Owner)
                .FirstOrDefaultAsync(x => x.Id == apartmentId, cancellationToken);

            if (apartment is null)
            {
                response.Success = false;
                response.Message = $"Apartment with Id {apartmentId} not found";
                return response;
            }

            response.Data = MapToDto(apartment);
            response.Success = true;
            response.Message = "Apartment retrieved successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error retrieving apartment: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<ApartmentResponse>> GetByIdByOwnerIdAsync(
        Guid apartmentId,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<ApartmentResponse>();
        try
        {
            var apartment = await _dbContext.Apartments
                .AsNoTracking()
                .Include(x => x.Owner)
                .FirstOrDefaultAsync(x => x.Id == apartmentId && x.OwnerId == ownerId, cancellationToken);

            if (apartment is null)
            {
                response.Success = false;
                response.Message = $"Apartment with Id {apartmentId} not found";
                return response;
            }

            response.Data = MapToDto(apartment);
            response.Success = true;
            response.Message = "Apartment retrieved successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error retrieving apartment: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<ApartmentResponse>> CreateAsync(string ownerId, CreateApartmentRequest request, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<ApartmentResponse>();
        try
        {
            var validationError = ValidateApartmentRequest(
                request.Title,
                request.Description,
                request.ImageUrl,
                request.Price,
                request.Address,
                request.City,
                request.Department);

            if (validationError is not null)
            {
                response.Success = false;
                response.Message = validationError;
                return response;
            }

            var locationExists = await _dbContext.Apartments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(x =>
                    x.DeletedAt == null &&
                    x.Address == request.Address.Trim() &&
                    x.City == request.City.Trim() &&
                    x.Department == request.Department.Trim(),
                    cancellationToken);

            if (locationExists)
            {
                response.Success = false;
                response.Message = "An active apartment already exists at that location.";
                return response;
            }

            var owner = await _dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == ownerId && x.DeletedAt == null, cancellationToken);

            if (owner is null)
            {
                response.Success = false;
                response.Message = $"Owner with Id {ownerId} not found";
                return response;
            }

            if (!await _userManager.IsInRoleAsync(owner, AppaRentlyRoles.Owner))
            {
                response.Success = false;
                response.Message = $"User with Id {ownerId} is not an owner";
                return response;
            }

            var apartment = new Apartment
            {
                OwnerId = ownerId,
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                ImageUrl = NormalizeOptionalString(request.ImageUrl),
                Price = request.Price,
                Address = request.Address.Trim(),
                City = request.City.Trim(),
                Department = request.Department.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Apartments.Add(apartment);
            await _dbContext.SaveChangesAsync(cancellationToken);

            apartment.Owner = owner;
            await NotifyApartmentCreatedAsync(apartment, cancellationToken);

            response.Data = MapToDto(apartment);
            response.Success = true;
            response.Message = "Apartment created successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error creating apartment: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<ApartmentResponse>> UpdateAsync(Guid apartmentId, string ownerId, UpdateApartmentRequest request, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<ApartmentResponse>();
        try
        {
            var validationError = ValidateApartmentRequest(
                request.Title,
                request.Description,
                request.ImageUrl,
                request.Price,
                request.Address,
                request.City,
                request.Department);

            if (validationError is not null)
            {
                response.Success = false;
                response.Message = validationError;
                return response;
            }

            var apartment = await _dbContext.Apartments
                .Include(x => x.Owner)
                .FirstOrDefaultAsync(x => x.Id == apartmentId, cancellationToken);

            if (apartment is null)
            {
                response.Success = false;
                response.Message = $"Apartment with Id {apartmentId} not found";
                return response;
            }

            if (!string.Equals(apartment.OwnerId, ownerId, StringComparison.Ordinal))
            {
                response.Success = false;
                response.Message = "You are not allowed to update this apartment";
                return response;
            }

            var locationExists = await _dbContext.Apartments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id != apartmentId &&
                    x.DeletedAt == null &&
                    x.Address == request.Address.Trim() &&
                    x.City == request.City.Trim() &&
                    x.Department == request.Department.Trim(),
                    cancellationToken);

            if (locationExists)
            {
                response.Success = false;
                response.Message = "An active apartment already exists at that location.";
                return response;
            }

            apartment.Title = request.Title.Trim();
            apartment.Description = request.Description.Trim();
            apartment.ImageUrl = NormalizeOptionalString(request.ImageUrl);
            apartment.Price = request.Price;
            apartment.Address = request.Address.Trim();
            apartment.City = request.City.Trim();
            apartment.Department = request.Department.Trim();
            apartment.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            await NotifyApartmentChangedAsync(apartment, cancellationToken);

            response.Data = MapToDto(apartment);
            response.Success = true;
            response.Message = "Apartment updated successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error updating apartment: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(Guid apartmentId, string ownerId, CancellationToken cancellationToken = default)
    {
        return await DeleteApartmentInternalAsync(apartmentId, ownerId, validateOwner: true, cancellationToken);
    }

    public Task<ServiceResponse<bool>> DeleteAsSuperAdminAsync(Guid apartmentId, CancellationToken cancellationToken = default)
    {
        return DeleteApartmentInternalAsync(apartmentId, ownerId: null, validateOwner: false, cancellationToken);
    }

    private IQueryable<Apartment> ApplyFilters(IQueryable<Apartment> query, ApartmentSearchRequest? request)
    {
        if (request is null)
        {
            return query;
        }

        if (!string.IsNullOrWhiteSpace(request.OwnerId))
        {
            var ownerId = request.OwnerId.Trim();
            query = query.Where(x => x.OwnerId == ownerId);
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim();
            query = query.Where(x => x.City.Contains(city));
        }

        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            var department = request.Department.Trim();
            query = query.Where(x => x.Department.Contains(department));
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(x => x.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(x => x.Price <= request.MaxPrice.Value);
        }

        if (request.AvailableFrom.HasValue && request.AvailableTo.HasValue)
        {
            var availableFrom = ReservationDateRules.NormalizeCheckIn(request.AvailableFrom.Value);
            var availableTo = ReservationDateRules.NormalizeCheckOut(request.AvailableTo.Value);

            if (availableTo > availableFrom)
            {
                query = query.Where(apartment => 
                    !_dbContext.Reservations.IgnoreQueryFilters().Any(reservation =>
                        reservation.ApartmentId == apartment.Id &&
                        reservation.DeletedAt == null &&
                        reservation.CheckIn < availableTo &&
                        reservation.CheckOut > availableFrom));
            }
        }

        return query;
    }

    private async Task<ServiceResponse<bool>> DeleteApartmentInternalAsync(
        Guid apartmentId,
        string? ownerId,
        bool validateOwner,
        CancellationToken cancellationToken)
    {
        var response = new ServiceResponse<bool>();
        try
        {
            var apartment = await _dbContext.Apartments
                .Include(x => x.Owner)
                .FirstOrDefaultAsync(x => x.Id == apartmentId, cancellationToken);

            if (apartment is null)
            {
                response.Success = false;
                response.Data = false;
                response.Message = $"Apartment with Id {apartmentId} not found";
                return response;
            }

            if (validateOwner && !string.Equals(apartment.OwnerId, ownerId, StringComparison.Ordinal))
            {
                response.Success = false;
                response.Data = false;
                response.Message = "You are not allowed to delete this apartment";
                return response;
            }

            var deletedAt = DateTime.UtcNow;

            await _dbContext.SoftDeleteApartmentDependenciesAsync(
                new[] { apartmentId },
                deletedAt,
                cancellationToken);

            apartment.DeletedAt = deletedAt;
            apartment.UpdatedAt = deletedAt;

            await _dbContext.SaveChangesAsync(cancellationToken);

            await NotifyApartmentDeletedAsync(apartment, cancellationToken);

            response.Data = true;
            response.Success = true;
            response.Message = "Apartment deleted successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Data = false;
            response.Message = $"Error deleting apartment: {ex.Message}";
        }

        return response;
    }

    private async Task<ServiceResponse<IEnumerable<ApartmentResponse>>> GetApartmentsAsync(
        bool includeDeleted,
        bool deletedOnly,
        ApartmentSearchRequest? request,
        CancellationToken cancellationToken)
    {
        var response = new ServiceResponse<IEnumerable<ApartmentResponse>>();
        try
        {
            var query = _dbContext.Apartments
                .AsNoTracking()
                .Include(x => x.Owner)
                .AsQueryable();

            if (includeDeleted)
            {
                query = query.IgnoreQueryFilters();
            }

            if (deletedOnly)
            {
                query = query.Where(x => x.DeletedAt != null);
            }

            query = ApplyFilters(query, request);

            var apartments = await query
                .OrderBy(x => x.City)
                .ThenBy(x => x.Department)
                .ThenBy(x => x.Title)
                .ToListAsync(cancellationToken);

            response.Data = apartments.Select(MapToDto).ToList();
            response.Success = true;
            response.Message = "Apartments retrieved successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error retrieving apartments: {ex.Message}";
        }

        return response;
    }

    private static ApartmentResponse MapToDto(Apartment apartment)
    {
        return new ApartmentResponse
        {
            Id = apartment.Id,
            OwnerId = apartment.OwnerId,
            OwnerFullName = apartment.Owner?.FullName ?? string.Empty,
            Title = apartment.Title,
            Description = apartment.Description,
            ImageUrl = apartment.ImageUrl,
            Price = apartment.Price,
            Address = apartment.Address,
            City = apartment.City,
            Department = apartment.Department,
            CreatedAt = apartment.CreatedAt,
            UpdatedAt = apartment.UpdatedAt,
            DeletedAt = apartment.DeletedAt
        };
    }

    private async Task NotifyApartmentChangedAsync(Apartment apartment, CancellationToken cancellationToken)
    {
        var body = BuildApartmentUpdatedBody(apartment, apartment.Owner?.FullName ?? string.Empty);
        var emails = await LoadApartmentStakeholderEmailsAsync(apartment.Id, cancellationToken: cancellationToken);
        foreach (var email in emails)
        {
            await _emailNotificationService.SendAsync(
                email,
                "Apartment updated",
                body,
                cancellationToken: cancellationToken);
        }

    }

    private async Task NotifyApartmentDeletedAsync(Apartment apartment, CancellationToken cancellationToken)
    {
        var body = BuildApartmentDeletedBody(apartment, apartment.Owner?.FullName ?? string.Empty);
        var emails = await LoadApartmentStakeholderEmailsAsync(apartment.Id, includeDeletedRelationships: true, cancellationToken);
        foreach (var email in emails)
        {
            await _emailNotificationService.SendAsync(
                email,
                "Apartment removed",
                body,
                cancellationToken: cancellationToken);
        }
    }

    private static Task NotifyApartmentCreatedAsync(Apartment apartment, CancellationToken cancellationToken)
    {
        _ = apartment;
        _ = cancellationToken;
        return Task.CompletedTask;
    }

    private async Task<HashSet<string>> LoadApartmentStakeholderEmailsAsync(
        Guid apartmentId,
        bool includeDeletedRelationships = false,
        CancellationToken cancellationToken = default)
    {
        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var favoriteQuery = _dbContext.Favorites
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId);

        if (!includeDeletedRelationships)
        {
            favoriteQuery = favoriteQuery.Where(x => x.DeletedAt == null);
        }

        var favoriteEmails = await favoriteQuery
            .Select(x => x.User!.Email)
            .ToListAsync(cancellationToken);

        foreach (var email in favoriteEmails)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                emails.Add(email.Trim());
            }
        }

        var reservationQuery = _dbContext.Reservations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId);

        if (!includeDeletedRelationships)
        {
            reservationQuery = reservationQuery.Where(x => x.DeletedAt == null);
        }

        var reservationEmails = await reservationQuery
            .Select(x => x.User!.Email)
            .ToListAsync(cancellationToken);

        foreach (var email in reservationEmails)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                emails.Add(email.Trim());
            }
        }

        return emails;
    }

    private async Task<HashSet<string>> LoadApartmentStakeholderUserIdsAsync(
        Guid apartmentId,
        bool includeDeletedRelationships = false,
        CancellationToken cancellationToken = default)
    {
        var userIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var favoriteQuery = _dbContext.Favorites
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId);

        if (!includeDeletedRelationships)
        {
            favoriteQuery = favoriteQuery.Where(x => x.DeletedAt == null);
        }

        var favoriteUserIds = await favoriteQuery
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        foreach (var userId in favoriteUserIds)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                userIds.Add(userId.Trim());
            }
        }

        var reservationQuery = _dbContext.Reservations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId);

        if (!includeDeletedRelationships)
        {
            reservationQuery = reservationQuery.Where(x => x.DeletedAt == null);
        }

        var reservationUserIds = await reservationQuery
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        foreach (var userId in reservationUserIds)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                userIds.Add(userId.Trim());
            }
        }

        return userIds;
    }

    private static string BuildApartmentUpdatedBody(Apartment apartment, string ownerName)
    {
        return
            $"Hello,{Environment.NewLine}{Environment.NewLine}" +
            $"The apartment '{apartment.Title}' has been updated.{Environment.NewLine}" +
            $"Owner: {ownerName}{Environment.NewLine}" +
            $"Location: {apartment.Address}, {apartment.City}, {apartment.Department}{Environment.NewLine}" +
            $"Daily price: {apartment.Price:C}";
    }

    private static string BuildApartmentDeletedBody(Apartment apartment, string ownerName)
    {
        return
            $"Hello,{Environment.NewLine}{Environment.NewLine}" +
            $"The apartment '{apartment.Title}' has been removed from the platform.{Environment.NewLine}" +
            $"Owner: {ownerName}{Environment.NewLine}" +
            $"Location: {apartment.Address}, {apartment.City}, {apartment.Department}";
    }

    private static string? ValidateApartmentRequest(
        string title,
        string description,
        string? imageUrl,
        decimal price,
        string address,
        string city,
        string department)
    {
        if (string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(description) ||
            string.IsNullOrWhiteSpace(address) ||
            string.IsNullOrWhiteSpace(city) ||
            string.IsNullOrWhiteSpace(department))
        {
            return "Apartment data is required.";
        }

        if (price < 0)
        {
            return "Price must be greater than or equal to zero.";
        }

        if (!string.IsNullOrWhiteSpace(imageUrl) && imageUrl.Trim().Length > 2000)
        {
            return "Image URL must not exceed 2000 characters.";
        }

        return null;
    }

    private static string? NormalizeOptionalString(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
