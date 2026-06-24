using AppaRently.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppaRently.Infrastructure.Data.Configurations;

public sealed class ApartmentConfiguration : IEntityTypeConfiguration<Apartment>
{
    public void Configure(EntityTypeBuilder<Apartment> builder)
    {
        builder.ToTable(
            "Apartments",
            tableBuilder => tableBuilder.HasCheckConstraint("CK_Apartments_Price_Positive", "\"Price\" >= 0"));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Price)
            .HasPrecision(18, 2);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => x.City);
        builder.HasIndex(x => x.Department);
        builder.HasIndex(x => x.DeletedAt);

        builder.HasOne(x => x.Owner)
            .WithMany(x => x.OwnedApartments)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Favorites)
            .WithOne(x => x.Apartment)
            .HasForeignKey(x => x.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Reservations)
            .WithOne(x => x.Apartment)
            .HasForeignKey(x => x.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
