using UserService.Application.DTOs;

namespace UserService.Application.Interfaces;

public interface IUserService
{
    Task<AuthResponse> RegisterAsync(string login, string password);
    Task<AuthResponse> LoginAsync(string login, string password);
}