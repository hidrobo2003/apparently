using AppaRently.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Common;

namespace AppaRently.Infrastructure.Seeders;

public static class AppaRentlySeederExtensions
{
    private const int MigrationLockKey1 = 20260624;
    private const int MigrationLockKey2 = 1;

    public static async Task SeedAppaRentlyAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        await using var scope = host.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppaRentlyDbContext>();
        await dbContext.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await ExecuteAdvisoryLockAsync(
                dbContext.Database.GetDbConnection(),
                $"SELECT pg_advisory_lock({MigrationLockKey1}, {MigrationLockKey2});",
                cancellationToken);

            try
            {
                await dbContext.Database.MigrateAsync(cancellationToken);

                var seeder = scope.ServiceProvider.GetRequiredService<AppaRentlySeeder>();
                await seeder.SeedAsync(cancellationToken);
            }
            finally
            {
                await ExecuteAdvisoryLockAsync(
                    dbContext.Database.GetDbConnection(),
                    $"SELECT pg_advisory_unlock({MigrationLockKey1}, {MigrationLockKey2});",
                    cancellationToken);
            }
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private static async Task ExecuteAdvisoryLockAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
