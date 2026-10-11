using FluentAssertions;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Infrastructure.Repositories;
using Xunit;

namespace IntegrationTests;

public class UserRepositoryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public UserRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_ShouldSaveUser()
    {
        await _fixture.ResetAsync();
        var repo = new UserRepository(_fixture.UserContext);

        var user = new User($"user_{Guid.NewGuid():N}", "hash", UserRole.User);
        await repo.AddAsync(user);

        var saved = await _fixture.UserContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

        saved.Should().NotBeNull();
        saved!.Login.Should().Be(user.Login);
        saved.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateLogin_ShouldThrowDbUpdateException()
    {
        await _fixture.ResetAsync();
        var repo = new UserRepository(_fixture.UserContext);
        var login = $"user_{Guid.NewGuid():N}";

        await repo.AddAsync(new User(login, "h1"));

        var act = () => repo.AddAsync(new User(login, "h2"));

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task GetByLoginAsync_ShouldReturnUser()
    {
        await _fixture.ResetAsync();
        var repo = new UserRepository(_fixture.UserContext);
        var login = $"user_{Guid.NewGuid():N}";

        await repo.AddAsync(new User(login, "hash"));

        var result = await repo.GetByLoginAsync(login);

        result.Should().NotBeNull();
        result!.Login.Should().Be(login);
    }

    [Fact]
    public async Task ExistsByLoginAsync_WithDuplicate_ShouldReturnTrue()
    {
        await _fixture.ResetAsync();
        var repo = new UserRepository(_fixture.UserContext);
        var login = $"user_{Guid.NewGuid():N}";
        await repo.AddAsync(new User(login, "hash"));

        var exists = await repo.ExistsByLoginAsync(login);

        exists.Should().BeTrue();
    }
}