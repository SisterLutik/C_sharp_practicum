using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Exceptions;
using Xunit;

namespace UserService.Tests.Application;

public class UserManagerTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IPasswordHasher> _hasherMock;
    private readonly Mock<ITokenService> _tokenMock;
    private readonly UserManager _manager;

    public UserManagerTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _hasherMock = new Mock<IPasswordHasher>();
        _tokenMock = new Mock<ITokenService>();

        _manager = new UserManager(
            _userRepoMock.Object,
            _hasherMock.Object,
            _tokenMock.Object,
            NullLogger<UserManager>.Instance);
    }

    // ============================================
    // ✅ Регистрация
    // ============================================

    [Fact]
    public async Task RegisterAsync_WithNewLogin_ShouldCreateUserAndReturnToken()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.ExistsByLoginAsync("newuser"))
            .ReturnsAsync(false);

        _hasherMock
            .Setup(h => h.Hash("password123"))
            .Returns("hashed_password");

        _tokenMock
            .Setup(t => t.GenerateToken(It.IsAny<Guid>(), "newuser", UserRole.User))
            .Returns("jwt_token");

        // Act
        var result = await _manager.RegisterAsync("newuser", "password123");

        // Assert
        result.Should().NotBeNull();
        result.Login.Should().Be("newuser");
        result.Role.Should().Be(UserRole.User);
        result.Token.Should().Be("jwt_token");

        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldAlwaysCreateUserRole_NotAdmin()
    {
        // Arrange
        _userRepoMock.Setup(r => r.ExistsByLoginAsync("hacker")).ReturnsAsync(false);
        _hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        _tokenMock.Setup(t => t.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<UserRole>()))
            .Returns("token");

        User? capturedUser = null;
        _userRepoMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        // Act
        await _manager.RegisterAsync("hacker", "password");

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateLogin_ShouldThrowValidationException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.ExistsByLoginAsync("existing"))
            .ReturnsAsync(true);

        // Act
        var act = () => _manager.RegisterAsync("existing", "password");

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*уже существует*");
    }

    // ============================================
    // ✅ Логин
    // ============================================

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var user = new User("testuser", "hashed_password", UserRole.User);

        _userRepoMock
            .Setup(r => r.GetByLoginAsync("testuser"))
            .ReturnsAsync(user);

        _hasherMock
            .Setup(h => h.Verify("password123", "hashed_password"))
            .Returns(true);

        _tokenMock
            .Setup(t => t.GenerateToken(user.Id, "testuser", UserRole.User))
            .Returns("jwt_token");

        // Act
        var result = await _manager.LoginAsync("testuser", "password123");

        // Assert
        result.Should().NotBeNull();
        result.Login.Should().Be("testuser");
        result.Token.Should().Be("jwt_token");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var user = new User("testuser", "hashed_password");

        _userRepoMock
            .Setup(r => r.GetByLoginAsync("testuser"))
            .ReturnsAsync(user);

        _hasherMock
            .Setup(h => h.Verify("wrong", "hashed_password"))
            .Returns(false);

        // Act
        var act = () => _manager.LoginAsync("testuser", "wrong");

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>()
            .WithMessage("Неверный логин или пароль");
    }

    [Fact]
    public async Task LoginAsync_WithNonExistingUser_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.GetByLoginAsync("unknown"))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _manager.LoginAsync("unknown", "password");

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>()
            .WithMessage("Неверный логин или пароль");
    }
}