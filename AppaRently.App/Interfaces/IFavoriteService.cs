using AppaRently.App.DTOs.Favorites;
using AppaRently.App.ServiceResponse;

namespace AppaRently.App.Interfaces;

public interface IFavoriteService
{
    Task<ServiceResponse<IEnumerable<FavoriteResponse>>> GetAllAsync(FavoriteSearchRequest? request = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse<FavoriteResponse>> GetByIdAsync(Guid favoriteId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<FavoriteResponse>> CreateAsync(string userId, CreateFavoriteRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<bool>> DeleteAsync(Guid favoriteId, string userId, CancellationToken cancellationToken = default);
}
