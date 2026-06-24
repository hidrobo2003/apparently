using System.ComponentModel.DataAnnotations;

namespace AppaRently.Domain.Models;

public class Apartment
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(450)]
    public string OwnerId { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? ImageUrl { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999999999999999")]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Department { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ApplicationUser? Owner { get; set; }
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
