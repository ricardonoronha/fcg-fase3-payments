namespace FIAP.MicroService.Payments.Domain.Dtos;

public class FinishCheckoutResponse
{
    public Guid CheckoutId { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }

}

