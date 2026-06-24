using AppaRently.Infrastructure.Data;
using AppaRently.Infrastructure.Seeders;

namespace AppaRently.Infrastructure.Factories;

public sealed record SeedUserDefinition(
    string FullName,
    string Email,
    string Password,
    string Role);

public static class SeedUserFactory
{
    public static SeedUserDefinition CreateSuperAdmin()
    {
        return new SeedUserDefinition(
            AppaRentlySeedDefaults.SuperAdminFullName,
            AppaRentlySeedDefaults.SuperAdminEmail,
            AppaRentlySeedDefaults.DefaultPassword,
            AppaRentlyRoles.SuperAdmin);
    }

    public static SeedUserDefinition CreateOwner1()
    {
        return new SeedUserDefinition(
            "Lukre Roll 1",
            "lukreroll1@gmail.com",
            AppaRentlySeedDefaults.DefaultPassword,
            AppaRentlyRoles.Owner);
    }

    public static SeedUserDefinition CreateOwner2()
    {
        return new SeedUserDefinition(
            "Lukre Roll 2",
            "lukreroll2@gmail.com",
            AppaRentlySeedDefaults.DefaultPassword,
            AppaRentlyRoles.Owner);
    }
}
