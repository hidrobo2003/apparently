using System.ComponentModel.DataAnnotations;

namespace AppaRently.Domain.Models;

public class Reservation
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    public Guid ApartmentId { get; set; }

    public DateTime CheckIn { get; set; }

    public DateTime CheckOut { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime? ReminderBeforeStartSentAt { get; set; }

    public DateTime? ReminderBeforeEndSentAt { get; set; }

    public ApplicationUser? User { get; set; }
    public Apartment? Apartment { get; set; }
}
