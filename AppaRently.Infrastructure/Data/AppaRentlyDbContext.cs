using AppaRently.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AppaRently.Infrastructure.Data;

public class AppaRentlyDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public AppaRentlyDbContext(DbContextOptions<AppaRentlyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Apartment> Apartments => Set<Apartment>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppaRentlyDbContext).Assembly);
    }
}
