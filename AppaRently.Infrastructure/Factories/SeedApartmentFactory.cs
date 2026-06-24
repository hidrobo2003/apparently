namespace AppaRently.Infrastructure.Factories;

public sealed record SeedApartmentDefinition(
    string Title,
    string Description,
    string? ImageUrl,
    decimal Price,
    string Address,
    string City,
    string Department);

public static class SeedApartmentFactory
{
    public static IReadOnlyList<SeedApartmentDefinition> CreateForOwner(string ownerLabel, int ownerNumber)
    {
        var city = "Medellin";
        var department = "Antioquia";
        var ownerSuffix = ownerLabel;

        return ownerNumber switch
        {
            1 => new[]
            {
                new SeedApartmentDefinition(
                    Title: $"Loft Ejecutivo {ownerNumber}",
                    Description: $"Loft moderno y funcional para {ownerSuffix}.",
                    ImageUrl: null,
                    Price: 185000m,
                    Address: "Calle 10 # 45-12",
                    City: city,
                    Department: department),
                new SeedApartmentDefinition(
                    Title: $"Suite Urbana {ownerNumber}",
                    Description: $"Suite amplia y luminosa para {ownerSuffix}.",
                    ImageUrl: null,
                    Price: 215000m,
                    Address: "Carrera 73 # 34-18",
                    City: city,
                    Department: department),
                new SeedApartmentDefinition(
                    Title: $"Estudio Panorama {ownerNumber}",
                    Description: $"Estudio con vista abierta para {ownerSuffix}.",
                    ImageUrl: null,
                    Price: 165000m,
                    Address: "Carrera 43A # 8-20",
                    City: city,
                    Department: department)
            },
            2 => new[]
            {
                new SeedApartmentDefinition(
                    Title: $"Loft Ejecutivo {ownerNumber}",
                    Description: $"Loft moderno y funcional para {ownerSuffix}.",
                    ImageUrl: null,
                    Price: 188000m,
                    Address: "Calle 12 # 45-14",
                    City: city,
                    Department: department),
                new SeedApartmentDefinition(
                    Title: $"Suite Urbana {ownerNumber}",
                    Description: $"Suite amplia y luminosa para {ownerSuffix}.",
                    ImageUrl: null,
                    Price: 218000m,
                    Address: "Carrera 75 # 34-20",
                    City: city,
                    Department: department),
                new SeedApartmentDefinition(
                    Title: $"Estudio Panorama {ownerNumber}",
                    Description: $"Estudio con vista abierta para {ownerSuffix}.",
                    ImageUrl: null,
                    Price: 168000m,
                    Address: "Carrera 44A # 8-22",
                    City: city,
                    Department: department)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(ownerNumber), ownerNumber, "Only the seeded owners are supported.")
        };
    }
}
