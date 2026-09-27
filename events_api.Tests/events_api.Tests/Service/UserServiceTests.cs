using EventsApi.Application.Interfaces;
using EventsApi.Application.Services;
using EventsApi.Domain.Entities;
using EventsApi.Domain.Enums;
using EventsApi.Domain.Exceptions;
using EventsApi.Infrastructure.DataAccess;
using EventsApi.Infrastructure.Repositories;
using EventsApi.Infrastructure.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EventsApi.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly string _dbName;
    private readonly IServiceProvider _serviceProvider;

    public UserServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        services.AddSingleton<ILogger<UserService>>(NullLogger<UserService>.Instance);

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, FakeTokenService>();
        services.AddScoped<IUserService, UserService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    // =============================================
    // ✅ Регистрация
    // =============================================

    [Fact]
    public async Task RegisterAsync_WithNewLogin_ShouldCreateUserWithUserRole()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act
        var result = await userService.RegisterAsync("newuser", "password123");

        // Assert
        result.Should().NotBeNull();
        result.Login.Should().Be("newuser");
        result.Role.Should().Be(UserRole.User);   // ✅ всегда User
        result.Token.Should().NotBeNullOrEmpty();

        var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Login == "newuser");
        userInDb.Should().NotBeNull();
        userInDb!.PasswordHash.Should().NotBe("password123");   // хеш, а не открытый пароль
        userInDb.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingLogin_ShouldThrowValidationException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        await userService.RegisterAsync("duplicate", "password123");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            userService.RegisterAsync("duplicate", "another_password"));

        exception.Message.Should().Contain("уже существует");
    }

    [Fact]
    public async Task RegisterAsync_SamePasswordDifferentUsers_ShouldProduceDifferentHashes()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act — два пользователя с одинаковым паролем
        await userService.RegisterAsync("user1", "same_password");
        await userService.RegisterAsync("user2", "same_password");

        // Assert — хеши разные (из-за соли)
        var user1 = await context.Users.FirstAsync(u => u.Login == "user1");
        var user2 = await context.Users.FirstAsync(u => u.Login == "user2");

        user1.PasswordHash.Should().NotBe(user2.PasswordHash);
    }

    // =============================================
    // ✅ Логин
    // =============================================

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        await userService.RegisterAsync("validuser", "correct_password");

        // Act
        var result = await userService.LoginAsync("validuser", "correct_password");

        // Assert
        result.Should().NotBeNull();
        result.Login.Should().Be("validuser");
        result.Role.Should().Be(UserRole.User);
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        await userService.RegisterAsync("user1", "correct_password");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            userService.LoginAsync("user1", "wrong_password"));

        exception.Message.Should().Be("Неверный логин или пароль");
    }

    [Fact]
    public async Task LoginAsync_WithNonExistingUser_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            userService.LoginAsync("unknown", "password"));

        exception.Message.Should().Be("Неверный логин или пароль");
    }

    // =============================================
    //  Dispose
    // =============================================

    public void Dispose()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureDeleted();
        context.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }

    // =============================================
    //  Заглушка для ITokenService (чтобы не настраивать JWT в тестах)
    // =============================================

    private class FakeTokenService : ITokenService
    {
        public string GenerateToken(Guid userId, string login, UserRole role)
            => $"fake-token-{userId}";
    }
}