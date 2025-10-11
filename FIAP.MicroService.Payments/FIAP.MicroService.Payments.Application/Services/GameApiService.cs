using FIAP.MicroService.Payments.Application.Settings;
using FIAP.MicroService.Payments.Domain.Dtos;
using FIAP.MicroService.Payments.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;


namespace FIAP.MicroService.Payments.Application.Services;

public class GameApiService(ILogger<GameApiService> logger , HttpClient httpClient, IOptions<GameApiSettings> settingsOptions) : IGameApiService
{
    private readonly GameApiSettings _settings = settingsOptions.Value;

    public async Task<GameInfo?> GetById(Guid gameId)
    {
        logger.LogInformation("Getting game '{GameId}' data", gameId);

        // var response = await httpClient.GetAsync($"api/games/{gameId}");
        var response = await httpClient.GetAsync(string.Format(_settings.EndpointFormat, gameId));

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GameInfo?>();
    }
}
