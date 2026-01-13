namespace FIAP.MicroService.Payments.Domain.Dtos;

public class FinishCheckoutRequest
{
    public Guid CheckoutId { get; set; }
    public string PaymentMethod { get; set; } = default!;

    public static bool IsValid(FinishCheckoutRequest request, out string message)
    {
        if (request is null || request.CheckoutId == Guid.Empty || string.IsNullOrEmpty(request.PaymentMethod))
        {
            message = $"{nameof(CheckoutId)} e {nameof(PaymentMethod)} são obrigatórios.";
            return false;
        }

        message = string.Empty;
        return true;
    }
}

