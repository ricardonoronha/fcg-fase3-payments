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
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Text.Json;

namespace FIAP.MicroService.Payments.Application.Services;

public class CheckoutService(
    ILogger<CheckoutService> logger,
    IUserApiService userApiService,
    IGameApiService gameApiService,
    ICheckoutRepository repository,
    IOptions<ServiceBusSettings> serviceBusSettings, 
    ConnectionFactory connectionFactory) : ICheckoutService
{
    private readonly ConnectionFactory _connectionFactory = connectionFactory;
    private IConnection? _connection;

    public async Task<StartCheckoutResponse> StartCheckout(StartCheckoutRequest request, CancellationToken cancelamentoToken = default)
    {
        var getUserInfo = userApiService.GetById(request.UserId);
        await getUserInfo;

        
        var getGameInfo = gameApiService.GetById(request.GameId);
        await getGameInfo;


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

        await PublishMessageToExchangeAsync(checkout, cancelamentoToken);

        logger.LogInformation("Checkout finished | CheckoutId {CheckoutId}", checkout.Id);

        return new FinishCheckoutResponse { CheckoutId = checkout.Id, UserId = checkout.UserId, GameId = checkout.GameId };
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        _connection = await _connectionFactory.CreateConnectionAsync(ct);
        return _connection;
    }

    public async Task PublishMessageToExchangeAsync(Checkout checkout, CancellationToken ct = default)
    {
        // busca infos em paralelo
        var getGameInfoTask = gameApiService.GetById(checkout.GameId);
        var getUserInfoTask = userApiService.GetById(checkout.UserId);

        await Task.WhenAll(getGameInfoTask, getUserInfoTask);

        var game = getGameInfoTask.Result;
        var user = getUserInfoTask.Result;

        if (game is null || user is null)
        {
            logger.LogWarning(
                "Não foi possível recuperar informações do usuário ou do jogo para o checkout | CheckoutId {CheckoutId}",
                checkout.Id);
            return;
        }

        var payload = new CheckoutCompletedEvent
        {
            TotalAmount = checkout.Amount,
            User = user,
            Game = game
        };

        var conn = await GetConnectionAsync(ct);
        await using var channel = await conn.CreateChannelAsync();

        // (opcional mas recomendado) garante que o exchange existe
        await channel.ExchangeDeclareAsync(
            exchange: "user_exchange",
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        var json = JsonSerializer.Serialize(payload);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            Type = "CheckoutCompletedEvent",
            DeliveryMode = DeliveryModes.Persistent
        };

        await channel.BasicPublishAsync(
            exchange: "user_exchange",
            routingKey: "", // fanout ignora
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);

        logger.LogInformation(
            "Checkout enviado para processamentos assíncronos (RabbitMQ) | CheckoutId {CheckoutId}",
            checkout.Id);
    }
}
