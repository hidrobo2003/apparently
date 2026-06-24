using System.ComponentModel.DataAnnotations;

namespace AppaRently.Domain.Models;

public class Favorite
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    public Guid ApartmentId { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ApplicationUser? User { get; set; }
    public Apartment? Apartment { get; set; }
}
