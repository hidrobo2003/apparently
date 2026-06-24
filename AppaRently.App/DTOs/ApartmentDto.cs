using System.ComponentModel.DataAnnotations;

namespace AppaRently.App.DTOs.Apartments;

public sealed record CreateApartmentRequest
{
    [Required]
    [MaxLength(120)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? ImageUrl { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999999999999999")]
    public decimal Price { get; init; }

    [Required]
    [MaxLength(300)]
    public string Address { get; init; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string City { get; init; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Department { get; init; } = string.Empty;
}

public sealed record UpdateApartmentRequest
{
    [Required]
    [MaxLength(120)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? ImageUrl { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999999999999999")]
    public decimal Price { get; init; }

    [Required]
    [MaxLength(300)]
    public string Address { get; init; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string City { get; init; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Department { get; init; } = string.Empty;
}

public sealed record ApartmentSearchRequest
{
    [MaxLength(120)]
    public string? City { get; init; }

    [MaxLength(120)]
    public string? Department { get; init; }

    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }

    public DateTime? AvailableFrom { get; init; }

    public DateTime? AvailableTo { get; init; }

    [MaxLength(450)]
    public string? OwnerId { get; init; }
}

public sealed record ApartmentResponse
{
    public Guid Id { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string OwnerFullName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal Price { get; init; }
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}
