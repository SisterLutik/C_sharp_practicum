using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.DataAccess.Configurations;

public class ProcessedBookingConfiguration : IEntityTypeConfiguration<ProcessedBooking>
{
    public void Configure(EntityTypeBuilder<ProcessedBooking> builder)
    {
        builder.ToTable("ProcessedBookings");

        builder.HasKey(p => p.BookingId);

        builder.Property(p => p.ProcessedAt).IsRequired();
    }
}