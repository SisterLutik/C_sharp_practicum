using EventService.Application.DTOs;
using EventService.Application.Settings;
using EventService.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace EventService.Tests.Infrastructure;

public class RedisCacheServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _dbMock;
    private readonly RedisCacheService _service;

    public RedisCacheServiceTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _dbMock = new Mock<IDatabase>();

        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_dbMock.Object);

        var settings = Options.Create(new CacheSettings
        {
            InstanceName = "events:",
            EventTtlMinutes = 5,
            TopEventsTtlMinutes = 10
        });

        _service = new RedisCacheService(
            _redisMock.Object,
            settings,
            NullLogger<RedisCacheService>.Instance);
    }

    [Fact]
    public async Task GetAsync_WhenRedisThrows_ShouldReturnDefault()
    {
        _dbMock
            .Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        var result = await _service.GetAsync<EventResponse>("event:1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WhenRedisThrows_ShouldNotThrow()
    {
        _dbMock
            .Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        var act = () => _service.SetAsync("event:1", new EventResponse { Id = Guid.NewGuid() });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveAsync_WhenRedisThrows_ShouldNotThrow()
    {
        _dbMock
            .Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        var act = () => _service.RemoveAsync("event:1");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAsync_WhenTimeout_ShouldReturnDefault()
    {
        _dbMock
            .Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new TimeoutException("timeout"));

        var result = await _service.GetAsync<EventResponse>("event:1");

        result.Should().BeNull();
    }
}