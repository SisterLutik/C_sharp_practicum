using System.Net;
using System.Net.Http.Json;
using BookingService.Application.Interfaces;
using EventsApi.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace BookingService.Infrastructure.HttpClients;

public class EventServiceClient : IEventServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EventServiceClient> _logger;

    public EventServiceClient(HttpClient httpClient, ILogger<EventServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EventDto?> GetEventAsync(Guid eventId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/events/{eventId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<EventDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ошибка получения события {EventId} из EventService", eventId);
            throw;
        }
    }

    public async Task<ReserveSeatsResponse> TryReserveSeatsAsync(Guid eventId)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/events/{eventId}/reserve",
                content: null);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return new ReserveSeatsResponse
                {
                    Success = false,
                    Reason = "No available seats for this event"
                };
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ReserveSeatsResponse>();
            return result ?? new ReserveSeatsResponse { Success = false, Reason = "Empty response" };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ошибка резервирования места для события {EventId}", eventId);
            throw;
        }
    }

    public async Task ReleaseSeatsAsync(Guid eventId)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/events/{eventId}/release",
                content: null);

            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ошибка возврата места для события {EventId}", eventId);
            throw;
        }
    }
}