using EventService.Domain.Entities;
using EventService.Infrastructure.Repositories;
using FluentAssertions;
using IntegrationTests.Fixtures;
using Xunit;

namespace IntegrationTests;

public class EventRepositoryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public EventRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_ShouldSaveEvent()
    {
        await _fixture.ResetAsync();
        var repo = new EventRepository(_fixture.EventContext);

        var ev = new Event("Тест", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        await repo.AddAsync(ev);

        var saved = await repo.GetByIdAsync(ev.Id);

        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Тест");
    }

    [Fact]
    public async Task GetAllAsync_WithTitleFilter_ShouldReturnMatching()
    {
        await _fixture.ResetAsync();
        var repo = new EventRepository(_fixture.EventContext);

        await repo.AddAsync(new Event("Уникальное", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10));
        await repo.AddAsync(new Event("Другое", DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(4), 10));

        var result = await repo.GetAllAsync("уникальное", null, null, 1, 10);

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Уникальное");
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldReturnCorrectPage()
    {
        await _fixture.ResetAsync();
        var repo = new EventRepository(_fixture.EventContext);

        for (int i = 0; i < 25; i++)
        {
            await repo.AddAsync(new Event($"Событие {i}", DateTime.UtcNow.AddDays(i + 1), DateTime.UtcNow.AddDays(i + 2), 10));
        }

        var result = await repo.GetAllAsync(null, null, null, 2, 10);

        result.Page.Should().Be(2);
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEvent()
    {
        await _fixture.ResetAsync();
        var repo = new EventRepository(_fixture.EventContext);

        var ev = new Event("Удалить", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        await repo.AddAsync(ev);

        await repo.DeleteAsync(ev.Id);

        var result = await repo.GetByIdAsync(ev.Id);
        result.Should().BeNull();
    }
}