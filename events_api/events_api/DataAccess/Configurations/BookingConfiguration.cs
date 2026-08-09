using events_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace events_api.DataAccess.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            // 1. Имя таблицы
            builder.ToTable("Bookings");

            // 2. Первичный ключ
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id)
                .ValueGeneratedNever();

            // 3. Ограничения для свойств
            builder.Property(b => b.EventId)
                .IsRequired();

            builder.Property(b => b.Status)
                .IsRequired()
                .HasConversion<string>();  // ✅ Хранение enum как строки

            builder.Property(b => b.CreatedAt)
                .IsRequired();

            builder.Property(b => b.ProcessedAt)
                .IsRequired(false);

            // 4. Связь с Event
            builder.HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}