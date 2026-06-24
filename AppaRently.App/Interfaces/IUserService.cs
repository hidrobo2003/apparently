using AppaRently.App.DTOs.Users;
using AppaRently.App.ServiceResponse;

namespace AppaRently.App.Interfaces;

public interface IUserService
{
    Task<ServiceResponse<IEnumerable<UserResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServiceResponse<UserResponse>> CreateClientAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<UserResponse>> CreateOwnerAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<UserResponse>> CreateSuperAdminAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<UserResponse>> EditUserAsync(string userId, EditUserRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<bool>> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<UserResponse>> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
}
