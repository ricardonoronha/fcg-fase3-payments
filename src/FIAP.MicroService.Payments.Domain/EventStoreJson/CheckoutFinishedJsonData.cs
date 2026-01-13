namespace FIAP.MicroService.Payments.Domain.EventStoreJson;

public record CheckoutFinishedJsonData(Guid UserId, Guid GameId, decimal Amount);

