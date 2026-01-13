using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FIAP.MicroService.Payments.Domain.Models;

public class CheckoutEvent
{
    [Key] public long Id { get; set; }
    [Required] public Guid CheckoutId { get; set; }
    [ForeignKey(nameof(CheckoutId))] public Checkout Checkout { get; set; } = default!;
    [Required, MaxLength(64)] public string Type { get; set; } = default!; 
    public string? DataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
