using AppaRently.Domain.Models;
using AppaRently.Infrastructure.Data;
using AppaRently.Infrastructure.Factories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppaRently.Infrastructure.Seeders;

public sealed class AppaRentlySeeder
{
    private readonly AppaRentlyDbContext _dbContext;
    private readonly ILogger<AppaRentlySeeder> _logger;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AppaRentlySeeder(
        AppaRentlyDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AppaRentlySeeder> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRolesAsync(cancellationToken);

        var superAdmin = await EnsureUserAsync(SeedUserFactory.CreateSuperAdmin(), cancellationToken);
        var owner1 = await EnsureUserAsync(SeedUserFactory.CreateOwner1(), cancellationToken);
        var owner2 = await EnsureUserAsync(SeedUserFactory.CreateOwner2(), cancellationToken);

        await EnsureApartmentsAsync(owner1, 1, cancellationToken);
        await EnsureApartmentsAsync(owner2, 2, cancellationToken);

        _logger.LogInformation(
            "Seed completed. SuperAdmin: {SuperAdminEmail}. Owners: {Owner1Email}, {Owner2Email}.",
            superAdmin.Email,
            owner1.Email,
            owner2.Email);
    }

    private async Task EnsureRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var roleName in new[]
        {
            AppaRentlyRoles.Client,
            AppaRentlyRoles.Owner,
            AppaRentlyRoles.SuperAdmin
        })
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await _roleManager.CreateAsync(new IdentityRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            });

            if (!result.Succeeded)
            {
                throw CreateSeedException($"Unable to create role '{roleName}'.", result.Errors);
            }
        }
    }

    private async Task<ApplicationUser> EnsureUserAsync(SeedUserDefinition seed, CancellationToken cancellationToken)
    {
        var normalizedEmail = _userManager.NormalizeEmail(seed.Email);
        var existingUser = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            await EnsureUserRoleAsync(existingUser, seed.Role);
            return existingUser;
        }

        var user = new ApplicationUser
        {
            FullName = seed.FullName.Trim(),
            Email = seed.Email.Trim(),
            UserName = seed.Email.Trim(),
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, seed.Password);
        if (!createResult.Succeeded)
        {
            throw CreateSeedException($"Unable to create user '{seed.Email}'.", createResult.Errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, seed.Role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw CreateSeedException($"Unable to assign role '{seed.Role}' to '{seed.Email}'.", roleResult.Errors);
        }

        return user;
    }

    private async Task EnsureUserRoleAsync(ApplicationUser user, string role)
    {
        if (await _userManager.IsInRoleAsync(user, role))
        {
            return;
        }

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            throw CreateSeedException($"Unable to assign role '{role}' to '{user.Email}'.", result.Errors);
        }
    }

    private async Task EnsureApartmentsAsync(ApplicationUser owner, int ownerNumber, CancellationToken cancellationToken)
    {
        var apartmentSeeds = SeedApartmentFactory.CreateForOwner(owner.FullName, ownerNumber);

        foreach (var seed in apartmentSeeds)
        {
            var exists = await _dbContext.Apartments
                .IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.OwnerId == owner.Id &&
                    x.Address == seed.Address.Trim() &&
                    x.City == seed.City.Trim() &&
                    x.Department == seed.Department.Trim(),
                    cancellationToken);

            if (exists)
            {
                continue;
            }

            _dbContext.Apartments.Add(new Apartment
            {
                Id = Guid.NewGuid(),
                OwnerId = owner.Id,
                Title = seed.Title.Trim(),
                Description = seed.Description.Trim(),
                ImageUrl = seed.ImageUrl,
                Price = seed.Price,
                Address = seed.Address.Trim(),
                City = seed.City.Trim(),
                Department = seed.Department.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static InvalidOperationException CreateSeedException(string message, IEnumerable<IdentityError> errors)
    {
        var details = string.Join("; ", errors.Select(x => x.Description));
        return new InvalidOperationException($"{message} {details}".Trim());
    }
}
