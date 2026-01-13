namespace FIAP.MicroService.Payments.Domain.Dtos;

public class StartCheckoutResponse
{
    public Guid CheckoutId { get; set; }
    public UserInfo User { get; set; } = default!;
    public GameInfo Game { get; set; } = default!;
    public decimal Total { get; set; }
    public string[] PaymentMethods { get; set; } = [];
}

