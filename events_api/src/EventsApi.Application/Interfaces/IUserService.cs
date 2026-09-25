using EventsApi.Application.DTOs;
using EventsApi.Domain.Enums;

namespace EventsApi.Application.Interfaces;

public interface IUserService
{
    Task<AuthResponse> RegisterAsync(string login, string password, UserRole role = UserRole.User);
    Task<AuthResponse> LoginAsync(string login, string password);
}