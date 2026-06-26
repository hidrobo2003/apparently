using AppaRently.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppaRently.Infrastructure.Data.Configurations;

public sealed class AppNotificationConfiguration : IEntityTypeConfiguration<AppNotification>
{
    public void Configure(EntityTypeBuilder<AppNotification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.ReadAt });
        builder.HasIndex(x => x.ApartmentId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.DeletedAt);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Apartment)
            .WithMany()
            .HasForeignKey(x => x.ApartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
