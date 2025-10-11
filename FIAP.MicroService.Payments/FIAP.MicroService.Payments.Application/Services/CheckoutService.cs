using Azure.Core;
using Azure.Messaging.ServiceBus;
using FIAP.MicroService.Payments.Application.Settings;
using FIAP.MicroService.Payments.Domain.Constants;
using FIAP.MicroService.Payments.Domain.Dtos;
using FIAP.MicroService.Payments.Domain.EventStoreJson;
using FIAP.MicroService.Payments.Domain.Models;
using FIAP.MicroService.Payments.Domain.Repositories;
using FIAP.MicroService.Payments.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json;

namespace FIAP.MicroService.Payments.Application.Services;

public class CheckoutService(
    ILogger<CheckoutService> logger,
    IUserApiService userApiService,
    IGameApiService gameApiService,
    ICheckoutRepository repository,
    IOptions<ServiceBusSettings> serviceBusSettings) : ICheckoutService
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
        Checkout checkout = await repository.GetByIdAsync(checkoutId, cancelamentoToken)
                       ?? throw new KeyNotFoundException("Checkout não encontrado.");

        //if (checkout.Status == CheckoutStatus.Finished)
        //    return new FinishCheckoutResponse { CheckoutId = checkout.Id, UserId = checkout.UserId, GameId = checkout.GameId };

        checkout.Status = CheckoutStatus.Finished;
        checkout.FinishedAt = DateTimeOffset.UtcNow;

        checkout.Events.Add(new CheckoutEvent
        {
            Type = "Finished",
            DataJson = JsonSerializer.Serialize(new CheckoutFinishedJsonData(checkout.UserId, checkout.GameId, checkout.Amount))
        });

        await repository.SaveChangesAsync(cancelamentoToken);

        await PublishMessageToTopic(checkout);

        logger.LogInformation("Checkout finished | CheckoutId {CheckoutId}", checkout.Id);

        return new FinishCheckoutResponse { CheckoutId = checkout.Id, UserId = checkout.UserId, GameId = checkout.GameId };
    }

    public async Task PublishMessageToTopic(Checkout checkout)
    {
        var settings = serviceBusSettings.Value;

        // cria o client
        await using var client = new ServiceBusClient(settings.ConnectionString);

        // cria o sender para o tópico
        ServiceBusSender sender = client.CreateSender(settings.Topic);


        var getGameInfo = gameApiService.GetById(checkout.GameId);
        var getUserInfo = userApiService.GetById(checkout.UserId);

        await getGameInfo;
        await getUserInfo;


        var gameResult = getGameInfo.Result!;
        var userResult = getUserInfo.Result!;

        // cria o payload (pode ser qualquer objeto)
        var payload = new
        {
            DadosCliente = new
            {
                Nome = userResult.Username,
                Email = userResult.Email
            },
            Valor = checkout.Amount,
            JogosComprados = new[] { gameResult.Nome }
        };

        // serializa pra JSON e cria a mensagem
        var message = new ServiceBusMessage(JsonSerializer.Serialize(payload))
        {
            ContentType = "application/json"
        };

        // envia
        await sender.SendMessageAsync(message);

        logger.LogInformation("Checkout enviado para processamentos assíncronos | CheckoutId {CheckoutId}", checkout.Id);

    }
}
