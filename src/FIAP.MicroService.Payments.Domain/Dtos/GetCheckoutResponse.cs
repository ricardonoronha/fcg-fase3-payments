namespace FIAP.MicroService.Payments.Domain.Dtos;

public class GetCheckoutResponse
{
    public Guid CheckoutId { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public string Status { get; set; } = string.Empty;
}

