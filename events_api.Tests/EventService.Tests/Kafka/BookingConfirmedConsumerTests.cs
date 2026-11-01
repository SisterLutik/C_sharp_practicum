using EventService.Application.Caching;
using EventService.Application.Interfaces;
using EventService.Application.Settings;
using EventService.Domain.Entities;
using EventService.Infrastructure.DataAccess;
using EventService.Infrastructure.Kafka;
using EventsApi.Contracts.Kafka;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Xunit;

namespace EventService.Tests.Kafka;

public class BookingConfirmedConsumerTests
{
    [Fact]
    public async Task HandleMessageAsync_ShouldDecreaseSeatsAndInvalidateCache()
    {
        var dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        services.AddDbContext<EventDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddSingleton<ICacheService>(sp => _cacheMock.Object);
        var provider = services.BuildServiceProvider();

        var eventEntity = new Event(
            "Тест",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10);

        using (var scope = provider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<EventDbContext>();
            ctx.Events.Add(eventEntity);
            await ctx.SaveChangesAsync();
        }

        var settings = Options.Create(new KafkaSettings
        {
            BootstrapServers = "unused",
            ConsumerGroup = "unused"
        });

        var consumer = new BookingConfirmedConsumer(
            provider,
            settings,
            NullLogger<BookingConfirmedConsumer>.Instance);

        var booking = new BookingConfirmed(
            Guid.NewGuid(),
            eventEntity.Id,
            Guid.NewGuid(),
            1,
            DateTime.UtcNow);

        var json = JsonSerializer.Serialize(booking, KafkaJsonOptions.Default);

        await consumer.HandleMessageAsync(json, CancellationToken.None);

        using (var scope = provider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<EventDbContext>();
            var updated = await ctx.Events.FindAsync(eventEntity.Id);
            updated!.AvailableSeats.Should().Be(9);
        }

        _cacheMock.Verify(
            c => c.RemoveAsync(CacheKeys.Event(eventEntity.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private readonly Mock<ICacheService> _cacheMock = new();
}