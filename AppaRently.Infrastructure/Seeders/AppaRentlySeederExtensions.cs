using AppaRently.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppaRently.Infrastructure.Seeders;

public static class AppaRentlySeederExtensions
{
    public static async Task SeedAppaRentlyAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        await using var scope = host.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppaRentlyDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var seeder = scope.ServiceProvider.GetRequiredService<AppaRentlySeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
