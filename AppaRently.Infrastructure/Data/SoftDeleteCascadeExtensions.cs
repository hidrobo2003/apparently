using AppaRently.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AppaRently.Infrastructure.Data;

internal static class SoftDeleteCascadeExtensions
{
    public static async Task SoftDeleteUserDependenciesAsync(
        this AppaRentlyDbContext dbContext,
        string userId,
        DateTime deletedAt,
        CancellationToken cancellationToken = default)
    {
        var favorites = await dbContext.Favorites
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId && x.DeletedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var favorite in favorites)
        {
            favorite.DeletedAt = deletedAt;
        }

        var reservations = await dbContext.Reservations
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId && x.DeletedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var reservation in reservations)
        {
            reservation.DeletedAt = deletedAt;
            reservation.UpdatedAt = deletedAt;
        }
    }

    public static async Task SoftDeleteApartmentDependenciesAsync(
        this AppaRentlyDbContext dbContext,
        IEnumerable<Guid> apartmentIds,
        DateTime deletedAt,
        CancellationToken cancellationToken = default)
    {
        var ids = apartmentIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        var favorites = await dbContext.Favorites
            .IgnoreQueryFilters()
            .Where(x => ids.Contains(x.ApartmentId) && x.DeletedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var favorite in favorites)
        {
            favorite.DeletedAt = deletedAt;
        }

        var reservations = await dbContext.Reservations
            .IgnoreQueryFilters()
            .Where(x => ids.Contains(x.ApartmentId) && x.DeletedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var reservation in reservations)
        {
            reservation.DeletedAt = deletedAt;
            reservation.UpdatedAt = deletedAt;
        }
    }
}
