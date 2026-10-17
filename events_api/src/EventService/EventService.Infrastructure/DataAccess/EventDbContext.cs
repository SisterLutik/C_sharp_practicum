using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.DataAccess;

public sealed class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();

    public DbSet<ProcessedBooking> ProcessedBookings => Set<ProcessedBooking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventDbContext).Assembly);
    }
}