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

    [Display(Name = "Change password")]
    public bool ChangePassword { get; init; }

    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string? NewPassword { get; init; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    public string? ConfirmNewPassword { get; init; }
}

public sealed record EditUserViewModel : IValidatableObject
{
    public string Id { get; init; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FullName { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public DateTime? DeletedAt { get; init; }

    [Display(Name = "Change password")]
    public bool ChangePassword { get; init; }

    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string? NewPassword { get; init; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    public string? ConfirmNewPassword { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ChangePassword)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            yield return new ValidationResult(
                "New password is required when changing the password.",
                new[] { nameof(NewPassword) });
        }
        else if (NewPassword.Length < 8)
        {
            yield return new ValidationResult(
                "New password must be at least 8 characters long.",
                new[] { nameof(NewPassword) });
        }

        if (string.IsNullOrWhiteSpace(ConfirmNewPassword))
        {
            yield return new ValidationResult(
                "Please confirm the new password.",
                new[] { nameof(ConfirmNewPassword) });
        }
        else if (!string.Equals(NewPassword, ConfirmNewPassword, StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "The new password and confirmation password do not match.",
                new[] { nameof(ConfirmNewPassword) });
        }
    }
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
