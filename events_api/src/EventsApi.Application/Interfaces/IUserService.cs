using EventsApi.Application.DTOs;

namespace EventsApi.Application.Interfaces;

public interface IUserService
{
    Task<AuthResponse> RegisterAsync(string login, string password);
    Task<AuthResponse> LoginAsync(string login, string password);
}