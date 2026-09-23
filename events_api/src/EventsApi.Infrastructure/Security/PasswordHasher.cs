using System.Security.Cryptography;
using System.Text;
using EventsApi.Application.Interfaces;

namespace EventsApi.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Пароль не может быть пустым", nameof(password));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        var computedHash = Hash(password);
        return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
    }
}