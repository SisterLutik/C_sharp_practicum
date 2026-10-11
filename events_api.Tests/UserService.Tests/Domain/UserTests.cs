using FluentAssertions;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using Xunit;

namespace UserService.Tests.Domain;

public class UserTests
{
    // ============================================
    // ✅ Успешные сценарии
    // ============================================

    [Fact]
    public void Constructor_WithValidData_ShouldCreateUser()
    {
        var user = new User("testuser", "hash123", UserRole.User);

        user.Id.Should().NotBe(Guid.Empty);
        user.Login.Should().Be("testuser");
        user.PasswordHash.Should().Be("hash123");
        user.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public void Constructor_WithAdminRole_ShouldCreateAdmin()
    {
        var user = new User("admin", "hash", UserRole.Admin);

        user.Role.Should().Be(UserRole.Admin);
        user.IsAdmin().Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithDefaultRole_ShouldCreateUser()
    {
        var user = new User("testuser", "hash");

        user.Role.Should().Be(UserRole.User);
        user.IsAdmin().Should().BeFalse();
    }

    [Fact]
    public void ChangePassword_WithValidHash_ShouldUpdatePasswordHash()
    {
        var user = new User("testuser", "old_hash");

        user.ChangePassword("new_hash");

        user.PasswordHash.Should().Be("new_hash");
    }

    [Fact]
    public void ChangeRole_ShouldUpdateRole()
    {
        var user = new User("testuser", "hash", UserRole.User);

        user.ChangeRole(UserRole.Admin);

        user.Role.Should().Be(UserRole.Admin);
    }

    // ============================================
    // ❌ Неуспешные сценарии
    // ============================================

    [Fact]
    public void Constructor_WithEmptyLogin_ShouldThrow()
    {
        var act = () => new User("", "hash");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Логин*");
    }

    [Fact]
    public void Constructor_WithNullLogin_ShouldThrow()
    {
        var act = () => new User(null!, "hash");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithWhitespaceLogin_ShouldThrow()
    {
        var act = () => new User("   ", "hash");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyHash_ShouldThrow()
    {
        var act = () => new User("testuser", "");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Хеш*");
    }

    [Fact]
    public void ChangePassword_WithEmptyHash_ShouldThrow()
    {
        var user = new User("testuser", "hash");

        var act = () => user.ChangePassword("");

        act.Should().Throw<ArgumentException>();
    }
}