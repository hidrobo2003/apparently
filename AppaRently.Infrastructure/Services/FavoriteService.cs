using AppaRently.App.DTOs.Favorites;
using AppaRently.App.Interfaces;
using AppaRently.App.ServiceResponse;
using AppaRently.Domain.Models;
using AppaRently.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppaRently.Infrastructure.Services;

public sealed class FavoriteService : IFavoriteService
{
    private readonly AppaRentlyDbContext _dbContext;

    public FavoriteService(AppaRentlyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ServiceResponse<IEnumerable<FavoriteResponse>>> GetAllAsync(FavoriteSearchRequest? request = null, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<IEnumerable<FavoriteResponse>>();
        try
        {
            var query = _dbContext.Favorites
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Apartment)
                .AsQueryable();

            query = ApplyFilters(query, request);

            response.Data = await query
                .OrderByDescending(x => x.Id)
                .Select(x => new FavoriteResponse
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    UserFullName = x.User!.FullName,
                    ApartmentId = x.ApartmentId,
                    ApartmentTitle = x.Apartment!.Title,
                    ApartmentAddress = x.Apartment.Address,
                    DeletedAt = x.DeletedAt
                })
                .ToListAsync(cancellationToken);

            response.Success = true;
            response.Message = "Favorites retrieved successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error retrieving favorites: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<FavoriteResponse>> GetByIdAsync(Guid favoriteId, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<FavoriteResponse>();
        try
        {
            var favorite = await _dbContext.Favorites
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Apartment)
                .FirstOrDefaultAsync(x => x.Id == favoriteId, cancellationToken);

            if (favorite is null)
            {
                response.Success = false;
                response.Message = $"Favorite with Id {favoriteId} not found";
                return response;
            }

            response.Data = MapToDto(favorite);
            response.Success = true;
            response.Message = "Favorite retrieved successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error retrieving favorite: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<FavoriteResponse>> CreateAsync(string userId, CreateFavoriteRequest request, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<FavoriteResponse>();
        try
        {
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
                .FirstOrDefaultAsync(x => x.Id == request.ApartmentId, cancellationToken);

            if (apartment is null)
            {
                response.Success = false;
                response.Message = $"Apartment with Id {request.ApartmentId} not found";
                return response;
            }

            var existingFavorite = await _dbContext.Favorites
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ApartmentId == request.ApartmentId, cancellationToken);

            if (existingFavorite is not null && existingFavorite.DeletedAt is null)
            {
                response.Success = false;
                response.Message = "Apartment is already in favorites";
                return response;
            }

            if (existingFavorite is not null)
            {
                existingFavorite.DeletedAt = null;
            }
            else
            {
                existingFavorite = new Favorite
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ApartmentId = request.ApartmentId
                };
                _dbContext.Favorites.Add(existingFavorite);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            existingFavorite.User = user;
            existingFavorite.Apartment = apartment;

            response.Data = MapToDto(existingFavorite);
            response.Success = true;
            response.Message = "Favorite created successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error creating favorite: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(Guid favoriteId, string userId, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<bool>();
        try
        {
            var favorite = await _dbContext.Favorites
                .FirstOrDefaultAsync(x => x.Id == favoriteId, cancellationToken);

            if (favorite is null)
            {
                response.Success = false;
                response.Data = false;
                response.Message = $"Favorite with Id {favoriteId} not found";
                return response;
            }

            if (!string.Equals(favorite.UserId, userId, StringComparison.Ordinal))
            {
                response.Success = false;
                response.Data = false;
                response.Message = "You are not allowed to delete this favorite";
                return response;
            }

            favorite.DeletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            response.Success = true;
            response.Data = true;
            response.Message = "Favorite deleted successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Data = false;
            response.Message = $"Error deleting favorite: {ex.Message}";
        }

        return response;
    }

    private static IQueryable<Favorite> ApplyFilters(IQueryable<Favorite> query, FavoriteSearchRequest? request)
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

        return query;
    }

    private static FavoriteResponse MapToDto(Favorite favorite)
    {
        return new FavoriteResponse
        {
            Id = favorite.Id,
            UserId = favorite.UserId,
            UserFullName = favorite.User?.FullName ?? string.Empty,
            ApartmentId = favorite.ApartmentId,
            ApartmentTitle = favorite.Apartment?.Title ?? string.Empty,
            ApartmentAddress = favorite.Apartment?.Address ?? string.Empty,
            DeletedAt = favorite.DeletedAt
        };
    }
}
