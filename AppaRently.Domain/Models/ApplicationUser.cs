using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AppaRently.Domain.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<Apartment> OwnedApartments { get; set; } = new List<Apartment>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<AppNotification> Notifications { get; set; } = new List<AppNotification>();
}
