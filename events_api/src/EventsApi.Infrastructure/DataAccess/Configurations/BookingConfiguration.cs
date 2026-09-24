using EventsApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventsApi.Infrastructure.DataAccess.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        // 1. Имя таблицы
        builder.ToTable("Bookings");

        // 2. Первичный ключ
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        // 3. Ограничения для свойств
        builder.Property(b => b.EventId).IsRequired();

        builder.Property(b => b.UserId).IsRequired();   // ← новый внешний ключ

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.ProcessedAt).IsRequired(false);

        // 4. Связь с Event (каскадное удаление)
        builder.HasOne(b => b.Event)
            .WithMany(e => e.Bookings)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // 5. Связь с User (Restrict — не удалять пользователя с бронями)
        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}