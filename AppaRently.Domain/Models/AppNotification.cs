using System.ComponentModel.DataAnnotations;

namespace AppaRently.Domain.Models;

public class AppNotification
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Type { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    public Guid? ApartmentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ApplicationUser? User { get; set; }
    public Apartment? Apartment { get; set; }
}
