using EventsApi.Domain.Enums;

namespace EventsApi.Application.Interfaces;

/// <summary>
/// Сервис для генерации JWT-токенов по данным пользователя.
/// </summary>
public interface ITokenService
{
    string GenerateToken(Guid userId, string login, UserRole role);
}