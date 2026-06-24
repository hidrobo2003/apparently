using AppaRently.App.DTOs.Users;
using AppaRently.App.ServiceResponse;
using AppaRently.App.Interfaces;
using AppaRently.Domain.Models;
using AppaRently.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AppaRently.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly AppaRentlyDbContext _dbContext;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(
        AppaRentlyDbContext dbContext,
        IEmailNotificationService emailNotificationService,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _dbContext = dbContext;
        _emailNotificationService = emailNotificationService;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<ServiceResponse<IEnumerable<UserResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<IEnumerable<UserResponse>>();
        try
        {
            var users = await _dbContext.Users
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var items = new List<UserResponse>(users.Count);
            foreach (var user in users)
            {
                var role = await GetUserRoleAsync(user);
                items.Add(MapToDto(user, role));
            }

            response.Data = items;
            response.Success = true;
            response.Message = "Users retrieved successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error retrieving users: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<UserResponse>> CreateClientAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<UserResponse>();
        try
        {
            response.Data = await CreateAsync(request, AppaRentlyRoles.Client, cancellationToken);
            response.Success = true;
            response.Message = "Client created successfully";

            _ = await _emailNotificationService.SendAsync(
                response.Data.Email,
                "Welcome to AppaRently",
                BuildWelcomeBody(response.Data.FullName, AppaRentlyRoles.Client));
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error creating client: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<UserResponse>> CreateOwnerAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<UserResponse>();
        try
        {
            response.Data = await CreateAsync(request, AppaRentlyRoles.Owner, cancellationToken);
            response.Success = true;
            response.Message = "Owner created successfully";

            _ = await _emailNotificationService.SendAsync(
                response.Data.Email,
                "Welcome to AppaRently",
                BuildWelcomeBody(response.Data.FullName, AppaRentlyRoles.Owner));
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error creating owner: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<UserResponse>> CreateSuperAdminAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<UserResponse>();
        try
        {
            response.Data = await CreateAsync(request, AppaRentlyRoles.SuperAdmin, cancellationToken);
            response.Success = true;
            response.Message = "SuperAdmin created successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error creating superadmin: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<UserResponse>> EditUserAsync(string userId, EditUserRequest request, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<UserResponse>();
        try
        {
            var user = await _dbContext.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user is null || user.DeletedAt is not null)
            {
                response.Success = false;
                response.Message = $"User with Id {userId} not found";
                return response;
            }

            user.FullName = request.FullName.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = $"Error updating user: {string.Join("; ", result.Errors.Select(x => x.Description))}";
                return response;
            }

            var role = await GetUserRoleAsync(user);
            response.Data = MapToDto(user, role);
            response.Success = true;
            response.Message = "User updated successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error updating user: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<bool>> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<bool>();
        try
        {
            var user = await _dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user is null || user.DeletedAt is not null)
            {
                response.Success = false;
                response.Data = false;
                response.Message = $"User with Id {userId} not found";
                return response;
            }

            var role = await GetUserRoleAsync(user);
            var deletedAt = DateTime.UtcNow;

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            await _dbContext.SoftDeleteUserDependenciesAsync(userId, deletedAt, cancellationToken);

            if (string.Equals(role, AppaRentlyRoles.Owner, StringComparison.Ordinal))
            {
                var ownedApartments = await _dbContext.Apartments
                    .IgnoreQueryFilters()
                    .Where(x => x.OwnerId == userId && x.DeletedAt == null)
                    .ToListAsync(cancellationToken);

                await _dbContext.SoftDeleteApartmentDependenciesAsync(
                    ownedApartments.Select(x => x.Id),
                    deletedAt,
                    cancellationToken);

                foreach (var apartment in ownedApartments)
                {
                    apartment.DeletedAt = deletedAt;
                    apartment.UpdatedAt = deletedAt;
                }
            }

            user.DeletedAt = deletedAt;
            user.UpdatedAt = deletedAt;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Data = false;
                response.Message = $"Error deleting user: {string.Join("; ", result.Errors.Select(x => x.Description))}";
                return response;
            }

            await transaction.CommitAsync(cancellationToken);

            response.Success = true;
            response.Data = true;
            response.Message = "User deleted successfully";

            _ = await _emailNotificationService.SendAsync(
                user.Email ?? string.Empty,
                "Your AppaRently account was removed",
                BuildDeletedAccountBody(user.FullName, role));
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Data = false;
            response.Message = $"Error deleting user: {ex.Message}";
        }

        return response;
    }

    public async Task<ServiceResponse<UserResponse>> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var response = new ServiceResponse<UserResponse>();
        try
        {
            var user = await _dbContext.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId && x.DeletedAt == null, cancellationToken);

            if (user is null)
            {
                response.Success = false;
                response.Message = $"User with Id {userId} not found";
                return response;
            }

            var role = await GetUserRoleAsync(user);
            response.Data = MapToDto(user, role);
            response.Success = true;
            response.Message = "User retrieved successfully";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error retrieving user: {ex.Message}";
        }

        return response;
    }

    private async Task<UserResponse> CreateAsync(CreateUserRequest request, string roleName, CancellationToken cancellationToken)
    {
        var trimmedEmail = request.Email.Trim();
        var trimmedFullName = request.FullName.Trim();

        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            throw new InvalidOperationException($"Role '{roleName}' is not registered.");
        }

        var existingUser = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Email == trimmedEmail, cancellationToken);

        if (existingUser is not null)
        {
            throw new InvalidOperationException("A user with that email already exists.");
        }

        var user = new ApplicationUser
        {
            FullName = trimmedFullName,
            Email = trimmedEmail,
            UserName = trimmedEmail,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            throw CreateIdentityException(createResult, "Unable to create the user.");
        }

        var roleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw CreateIdentityException(roleResult, "Unable to assign the role.");
        }

        return MapToDto(user, roleName);
    }

    private async Task<string> GetUserRoleAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault() ?? string.Empty;
    }

    private static UserResponse MapToDto(ApplicationUser user, string role)
    {
        return new UserResponse
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

    private static InvalidOperationException CreateIdentityException(IdentityResult result, string message)
    {
        var details = string.Join("; ", result.Errors.Select(x => x.Description));
        return new InvalidOperationException($"{message} {details}".Trim());
    }

    private static string BuildWelcomeBody(string fullName, string role)
    {
        return
            $"Hello {fullName},{Environment.NewLine}{Environment.NewLine}" +
            $"Your {role} account on AppaRently has been created successfully.{Environment.NewLine}" +
            "You can now sign in with your email and password.";
    }

    private static string BuildDeletedAccountBody(string fullName, string role)
    {
        return
            $"Hello {fullName},{Environment.NewLine}{Environment.NewLine}" +
            $"Your {role} account on AppaRently was removed by a SuperAdmin.{Environment.NewLine}" +
            "If you believe this is an error, contact the support team.";
    }
}
