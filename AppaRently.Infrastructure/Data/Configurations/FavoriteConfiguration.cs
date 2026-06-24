using AppaRently.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppaRently.Infrastructure.Data.Configurations;

public sealed class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("Favorites");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.HasIndex(x => new { x.UserId, x.ApartmentId })
            .IsUnique();

        builder.HasIndex(x => x.ApartmentId);
        builder.HasIndex(x => x.DeletedAt);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Favorites)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Apartment)
            .WithMany(x => x.Favorites)
            .HasForeignKey(x => x.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
