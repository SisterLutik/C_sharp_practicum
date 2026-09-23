using EventsApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventsApi.Infrastructure.DataAccess.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // 1. Имя таблицы
        builder.ToTable("Users");

        // 2. Первичный ключ
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        // 3. Ограничения
        builder.Property(u => u.Login)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(u => u.Login).IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>();
    }
}