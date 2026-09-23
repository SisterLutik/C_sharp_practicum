using EventsApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventsApi.Infrastructure
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
                .HasConversion<string>();  // Хранение enum как строки

            builder.Property(b => b.CreatedAt)
                .IsRequired();

            builder.Property(b => b.ProcessedAt)
                .IsRequired(false);

            // 4. Связь с Event
            // Один Event → много Bookings.
            // Внешний ключ — EventId.
            // При удалении события каскадно удаляются все его брони.
            builder.HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // 5. Внешний ключ на User
            // UserId обязателен: у каждой брони должен быть владелец.
            builder.Property(b => b.UserId).IsRequired();

            // 6. Связь с User
            // Один User → много Bookings.
            // Навигационное свойство Bookings у User не задано (WithMany() без аргументов),
            // потому что в доменной модели обратная коллекция не нужна.
            // Внешний ключ — UserId.
            // DeleteBehavior.Restrict: запрещаем удалять пользователя,
            // если у него есть бронирования (защита от потери истории).
            builder.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}