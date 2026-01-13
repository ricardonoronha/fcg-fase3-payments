using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIAP.MicroService.Payments.Domain.Dtos;

public sealed class CheckoutCompletedEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset PurchasedAt { get; init; } = DateTimeOffset.UtcNow;
    public decimal TotalAmount { get; init; }
    public UserInfo User { get; init; } = default!;
    public GameInfo Game { get; init; } = default!;
}
