using EventsApi.Domain.Enums;

namespace EventsApi.Domain.Entities;

public class User
{
    // Приватный конструктор для EF Core
    private User() { }

    public User(string login, string passwordHash, UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(login))
            throw new ArgumentException("Логин не может быть пустым", nameof(login));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Хеш пароля не может быть пустым", nameof(passwordHash));

        Id = Guid.NewGuid();
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }

    public Guid Id { get; private set; }
    public string Login { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }

    public bool IsAdmin() => Role == UserRole.Admin;

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Хеш пароля не может быть пустым", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
    }

    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
    }
}