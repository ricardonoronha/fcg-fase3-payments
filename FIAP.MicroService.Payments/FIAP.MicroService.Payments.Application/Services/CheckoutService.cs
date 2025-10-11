using FIAP.MicroService.Payments.Domain.Constants;
using FIAP.MicroService.Payments.Domain.Dtos;
using FIAP.MicroService.Payments.Domain.EventStoreJson;
using FIAP.MicroService.Payments.Domain.Models;
using FIAP.MicroService.Payments.Domain.Repositories;
using FIAP.MicroService.Payments.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FIAP.MicroService.Payments.Application.Services;

public class CheckoutService(
    ILogger<CheckoutService> logger,
    IUserApiService userApiService,
    IGameApiService gameApiService,
    ICheckoutRepository repository) : ICheckoutService
{
    public async Task<StartCheckoutResponse> StartCheckout(StartCheckoutRequest request, CancellationToken cancelamentoToken = default)
    {
        var getGameInfo = gameApiService.GetById(request.GameId);
        var getUserInfo = userApiService.GetById(request.UserId);

        await getGameInfo;
        await getUserInfo;


        var gameResult = getGameInfo.Result!;

        var checkout = new Checkout
        {
            UserId = request.UserId,
            GameId = request.GameId,
            Amount = request.Amount,
            Status = CheckoutStatus.Processing
        };

        await repository.AddAsync(checkout, cancelamentoToken);

        checkout.Events.Add(new CheckoutEvent
        {
            Type = "Started",
            DataJson = JsonSerializer.Serialize(new CheckoutStartJsonData(checkout.UserId, checkout.GameId, checkout.Amount))
        });

        await repository.SaveChangesAsync(cancelamentoToken);

        logger.LogInformation("Checkout started | CheckoutId {CheckoutId}", checkout.Id);

        return new StartCheckoutResponse()
        {
            CheckoutId = checkout.Id,
            Game = gameResult,
            User = getUserInfo.Result!,
            PaymentMethods = ["PIX", "Boleto"],
            Total = gameResult.Preco
        };
    }


    public async Task<GetCheckoutResponse> GetCheckout(Guid checkoutId, CancellationToken cancelamentoToken = default)
    {
        var checkout = await repository.GetByIdAsync(checkoutId, cancelamentoToken)
                ?? throw new KeyNotFoundException("Checkout não encontrado.");

        logger.LogInformation("Checkout read  | CheckoutId {CheckoutId}", checkout.Id);

        return new GetCheckoutResponse
        {
            CheckoutId = checkout.Id,
            UserId = checkout.UserId,
            GameId = checkout.GameId,
            Status = checkout.Status.ToString()
        };
    }


    public async Task<FinishCheckoutResponse> FinishCheckout(Guid checkoutId, CancellationToken cancelamentoToken = default)
    {
        var checkout = await repository.GetByIdAsync(checkoutId, cancelamentoToken)
                       ?? throw new KeyNotFoundException("Checkout não encontrado.");

        if (checkout.Status == CheckoutStatus.Finished)
            return new FinishCheckoutResponse { CheckoutId = checkout.Id, UserId = checkout.UserId, GameId = checkout.GameId };

        checkout.Status = CheckoutStatus.Finished;
        checkout.FinishedAt = DateTimeOffset.UtcNow;
        
        checkout.Events.Add(new CheckoutEvent
        {
            Type = "Finished",
            DataJson = JsonSerializer.Serialize(new CheckoutFinishedJsonData(checkout.UserId, checkout.GameId, checkout.Amount))
        });

        await repository.SaveChangesAsync(cancelamentoToken);

        logger.LogInformation("Checkout finished | CheckoutId {CheckoutId}", checkout.Id);

        return new FinishCheckoutResponse { CheckoutId = checkout.Id, UserId = checkout.UserId, GameId = checkout.GameId };
    }
}
