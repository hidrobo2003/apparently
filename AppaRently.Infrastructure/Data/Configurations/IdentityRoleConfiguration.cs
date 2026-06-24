using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppaRently.Infrastructure.Data.Configurations;

public sealed class IdentityRoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        builder.HasData(
            new IdentityRole
            {
                Id = "5d4e5e4f-5f1e-4f4e-8f7f-9d6f7c2c4a01",
                Name = AppaRentlyRoles.Client,
                NormalizedName = AppaRentlyRoles.Client.ToUpperInvariant(),
                ConcurrencyStamp = "5d4e5e4f-5f1e-4f4e-8f7f-9d6f7c2c4a01"
            },
            new IdentityRole
            {
                Id = "8a8a91f9-1d0b-4c67-9f56-0d0f7cc3f1b2",
                Name = AppaRentlyRoles.Owner,
                NormalizedName = AppaRentlyRoles.Owner.ToUpperInvariant(),
                ConcurrencyStamp = "8a8a91f9-1d0b-4c67-9f56-0d0f7cc3f1b2"
            },
            new IdentityRole
            {
                Id = "d7b8d7dd-8d7b-4ef5-8f5b-0f2d4d7d4a11",
                Name = AppaRentlyRoles.SuperAdmin,
                NormalizedName = AppaRentlyRoles.SuperAdmin.ToUpperInvariant(),
                ConcurrencyStamp = "d7b8d7dd-8d7b-4ef5-8f5b-0f2d4d7d4a11"
            });
    }
}
