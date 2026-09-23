namespace EventsApi.Application.Interfaces;

public interface IPasswordHasher
{
    /// <summary>
    /// Возвращает SHA-256 хеш пароля в hex-формате.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Проверяет, соответствует ли пароль указанному хешу.
    /// </summary>
    bool Verify(string password, string hash);
}