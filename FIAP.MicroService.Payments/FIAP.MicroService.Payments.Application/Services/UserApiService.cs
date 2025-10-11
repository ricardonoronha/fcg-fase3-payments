using FIAP.MicroService.Payments.Application.Settings;
using FIAP.MicroService.Payments.Domain.Dtos;
using FIAP.MicroService.Payments.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;


namespace FIAP.MicroService.Payments.Application.Services;

public class UserApiService(ILogger<UserApiService> logger, HttpClient httpClient, IOptions<UserApiSettings> settingsOptions) : IUserApiService
{
    private readonly UserApiSettings _settings = settingsOptions.Value;

    public async Task<UserInfo?> GetById(Guid userId)
    {
        logger.LogInformation("Getting user '{UserId}' data", userId);

        // var response = await httpClient.GetAsync($"/users/{userId}");
        string route = string.Format(_settings.EndpointFormat, userId);

        var response = await httpClient.GetAsync(route);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserInfo?>();
    }
}