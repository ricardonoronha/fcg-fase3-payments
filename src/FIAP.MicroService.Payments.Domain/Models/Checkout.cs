using FIAP.MicroService.Payments.Domain.Constants;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FIAP.MicroService.Payments.Domain.Models;

public class Checkout
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid UserId { get; set; }
    [Required] public Guid GameId { get; set; }

    // campos opcionais para futura demo de valor
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }

    public CheckoutStatus Status { get; set; } = CheckoutStatus.Processing;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }

    public List<CheckoutEvent> Events { get; set; } = new();
}