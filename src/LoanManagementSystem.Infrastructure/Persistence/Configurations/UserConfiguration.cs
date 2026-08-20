using LoanManagementSystem.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("user_id")
            .ValueGeneratedNever();

        builder.Property(u => u.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasColumnName("role")
            .HasMaxLength(20);

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20);

        builder.Property(u => u.CreatedAtUtc).HasColumnName("created_at").HasColumnType("datetime2");
    }
}
