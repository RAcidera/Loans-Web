using LoanManagementSystem.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("app_settings");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new AppSettingId(value))
            .HasColumnName("setting_id")
            .ValueGeneratedNever();

        builder.Property(s => s.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
        builder.HasIndex(s => s.Key).IsUnique();

        builder.Property(s => s.Value).HasColumnName("value").HasMaxLength(500).IsRequired();
    }
}
