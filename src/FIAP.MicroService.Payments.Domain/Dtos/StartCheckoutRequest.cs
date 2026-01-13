namespace FIAP.MicroService.Payments.Domain.Dtos;

public class StartCheckoutRequest
{
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public decimal Amount { get; set; }

    public static bool IsValid(StartCheckoutRequest request, out string message)
    {
        if (request is null || request.UserId == Guid.Empty || request.GameId == Guid.Empty)
        {
            message = $"{nameof(UserId)} e {nameof(GameId)} são obrigatórios.";
            return false;
        }

        message = string.Empty;
        return true;
    }
}

