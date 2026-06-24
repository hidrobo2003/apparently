using AppaRently.App.DTOs.Users;

namespace AppaRently.App.Interfaces;

public interface IJwtTokenService
{
    Task<JwtTokenResponse> CreateTokenAsync(UserResponse user, IEnumerable<string> roles, bool rememberMe = false, CancellationToken cancellationToken = default);
}
