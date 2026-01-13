namespace FIAP.MicroService.Payments.Domain.EventStoreJson;

public record CheckoutStartJsonData(Guid UserId, Guid GameId, decimal Amount);
