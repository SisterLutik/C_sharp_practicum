using EventsApi.Domain.Entities;
using EventsApi.Domain.Enums;
using EventsApi.Infrastructure.Repositories;
using EventsApi.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventsApi.IntegrationTests.Tests;

public class UserRepositoryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public UserRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // =============================================
    // ✅ Успешные сценарии
    // =============================================

    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new UserRepository(_fixture.DbContext);

        var login = $"user_{Guid.NewGuid():N}";
        var user = new User(login, "hashedpassword", UserRole.User);

        // Act
        await repository.AddAsync(user);

        // Assert
        var saved = await _fixture.DbContext.Users
            .FirstOrDefaultAsync(u => u.Login == login);

        saved.Should().NotBeNull();
        saved!.Login.Should().Be(login);
        saved.PasswordHash.Should().Be("hashedpassword");
        saved.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnUser()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new UserRepository(_fixture.DbContext);

        var user = new User($"user_{Guid.NewGuid():N}", "hash");
        await repository.AddAsync(user);

        // Act
        var result = await repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Login.Should().Be(user.Login);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new UserRepository(_fixture.DbContext);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByLoginAsync_WithExistingLogin_ShouldReturnUser()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new UserRepository(_fixture.DbContext);

        var login = $"user_{Guid.NewGuid():N}";
        var user = new User(login, "hash");
        await repository.AddAsync(user);

        // Act
        var result = await repository.GetByLoginAsync(login);

        // Assert
        result.Should().NotBeNull();
        result!.Login.Should().Be(login);
    }

    [Fact]
    public async Task GetByLoginAsync_WithNonExistingLogin_ShouldReturnNull()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new UserRepository(_fixture.DbContext);

        // Act
        var result = await repository.GetByLoginAsync("nonexistent_user");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsByLoginAsync_WithExistingLogin_ShouldReturnTrue()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new UserRepository(_fixture.DbContext);

        var login = $"user_{Guid.NewGuid():N}";
        await repository.AddAsync(new User(login, "hash"));

        // Act
        var exists = await repository.ExistsByLoginAsync(login);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByLoginAsync_WithNonExistingLogin_ShouldReturnFalse()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new UserRepository(_fixture.DbContext);

        // Act
        var exists = await repository.ExistsByLoginAsync("nonexistent_user");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUser()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new UserRepository(_fixture.DbContext);

        var user = new User($"user_{Guid.NewGuid():N}", "old_hash", UserRole.User);
        await repository.AddAsync(user);

        // Act
        user.ChangePassword("new_hash");
        user.ChangeRole(UserRole.Admin);
        await repository.UpdateAsync(user);

        // Assert
        var updated = await repository.GetByIdAsync(user.Id);
        updated.Should().NotBeNull();
        updated!.PasswordHash.Should().Be("new_hash");
        updated.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteUser()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new UserRepository(_fixture.DbContext);

        var user = new User($"user_{Guid.NewGuid():N}", "hash");
        await repository.AddAsync(user);

        // Act
        await repository.DeleteAsync(user.Id);

        // Assert
        var result = await repository.GetByIdAsync(user.Id);
        result.Should().BeNull();
    }

    // =============================================
    // ❌ UNIQUE constraint IX_Users_Login
    // =============================================

    [Fact]
    public async Task AddAsync_WithDuplicateLogin_ShouldThrowDbUpdateException()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new UserRepository(_fixture.DbContext);

        var login = $"duplicate_{Guid.NewGuid():N}";
        var user1 = new User(login, "hash1");
        await repository.AddAsync(user1);

        // Act & Assert
        var user2 = new User(login, "hash2");

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            repository.AddAsync(user2));

        exception.InnerException.Should().NotBeNull();
        exception.InnerException!.Message.Should().Contain("IX_Users_Login");
    }

    [Fact]
    public async Task AddAsync_WithDuplicateLogin_DifferentCase_ShouldNotThrow_WhenCaseSensitive()
    {
        // Arrange — PostgreSQL по умолчанию регистрозависим для UNIQUE
        await _fixture.ResetDatabaseAsync();
        var repository = new UserRepository(_fixture.DbContext);

        await repository.AddAsync(new User("CaseTestUser", "hash1"));

        // Act — другой регистр, должен пройти
        var exception = await Record.ExceptionAsync(() =>
            repository.AddAsync(new User("casetestuser", "hash2")));

        // Assert — PostgreSQL UNIQUE регистрозависим по умолчанию
        exception.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_AfterDeleteSameLogin_ShouldSucceed()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new UserRepository(_fixture.DbContext);

        var login = $"reuse_{Guid.NewGuid():N}";
        var user1 = new User(login, "hash1");
        await repository.AddAsync(user1);

        await repository.DeleteAsync(user1.Id);

        // Act — можно занять освободившийся логин
        var user2 = new User(login, "hash2");
        var exception = await Record.ExceptionAsync(() => repository.AddAsync(user2));

        // Assert
        exception.Should().BeNull();

        var saved = await repository.GetByLoginAsync(login);
        saved.Should().NotBeNull();
        saved!.Id.Should().Be(user2.Id);
    }

    [Fact]
    public async Task AddAsync_MultipleDistinctLogins_ShouldAllSucceed()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new UserRepository(_fixture.DbContext);

        // Act
        for (int i = 0; i < 10; i++)
        {
            await repository.AddAsync(new User($"user_{i}_{Guid.NewGuid():N}", "hash"));
        }

        // Assert
        var count = await _fixture.DbContext.Users.CountAsync();
        count.Should().Be(10);
    }

    [Fact]
    public async Task AddAsync_WithNullLogin_ShouldThrow()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new UserRepository(_fixture.DbContext);

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            // Доменный конструктор не даст создать User с пустым логином
            _ = new User(null!, "hash");
        });

        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task UniqueConstraint_ShouldBeEnforcedByName()
    {
        // Arrange — проверяем, что constraint создан с ожидаемым именем
        await _fixture.ResetDatabaseAsync();

        // Act — читаем список constraint-ов из information_schema
        var sql = @"
            SELECT COUNT(*) 
            FROM pg_indexes 
            WHERE tablename = 'Users' 
              AND indexname = 'IX_Users_Login' 
              AND indexdef ILIKE '%UNIQUE%'";

        using var command = _fixture.DbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        await _fixture.DbContext.Database.OpenConnectionAsync();
        var result = await command.ExecuteScalarAsync();
        var count = Convert.ToInt32(result);
        await _fixture.DbContext.Database.CloseConnectionAsync();

        // Assert
        count.Should().Be(1, "UNIQUE индекс IX_Users_Login должен существовать");
    }
}