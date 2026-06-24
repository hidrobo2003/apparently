using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AppaRently.Infrastructure.Data;

public sealed class AppaRentlyDbContextFactory : IDesignTimeDbContextFactory<AppaRentlyDbContext>
{
    public AppaRentlyDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__AppaRentlyDb")
            ?? "Host=localhost;Port=5432;Database=AppaRentlyDb;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AppaRentlyDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppaRentlyDbContext(optionsBuilder.Options);
    }
}
