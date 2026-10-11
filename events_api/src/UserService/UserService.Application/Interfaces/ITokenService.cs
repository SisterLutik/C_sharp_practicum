using UserService.Domain.Enums;

namespace UserService.Application.Interfaces;

/// <summary>
/// Сервис для генерации JWT-токенов по данным пользователя.
/// </summary>
public interface ITokenService
{
    string GenerateToken(Guid userId, string login, UserRole role);
}