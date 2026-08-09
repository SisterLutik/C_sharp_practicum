using events_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace events_api.DataAccess.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            // 1. Имя таблицы
            builder.ToTable("Events");

            // 2. Первичный ключ
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .ValueGeneratedNever();

            // 3. Ограничения для свойств
            builder.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.Description)
                .HasMaxLength(1000);

            builder.Property(e => e.StartAt)
                .IsRequired();

            builder.Property(e => e.EndAt)
                .IsRequired();

            builder.Property(e => e.TotalSeats)
                .IsRequired();

            builder.Property(e => e.AvailableSeats)
                .IsRequired();

            // 4. Связь «один–ко–многим» с Booking
            builder.HasMany(e => e.Bookings)
                .WithOne(b => b.Event)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}