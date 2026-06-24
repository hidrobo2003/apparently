using System.ComponentModel.DataAnnotations;

namespace AppaRently.App.DTOs.Users;

public sealed record CreateUserRequest
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;
}

public sealed record LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    public bool RememberMe { get; init; }
}

public sealed record JwtTokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public DateTime ExpiresAt { get; init; }
    public UserResponse User { get; init; } = new();
}

public sealed record EditUserRequest
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; init; } = string.Empty;
}

public sealed record UserResponse
{
    public string Id { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}
