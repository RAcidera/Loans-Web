using LoanManagementSystem.Domain.Diary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class DiaryCategoryConfiguration : IEntityTypeConfiguration<DiaryCategory>
{
    public void Configure(EntityTypeBuilder<DiaryCategory> builder)
    {
        builder.ToTable("diary_categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new DiaryCategoryId(value))
            .HasColumnName("category_id")
            .ValueGeneratedNever();

        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Icon).HasColumnName("icon").HasMaxLength(50);
        builder.Property(c => c.DisplayColor).HasColumnName("display_color").HasMaxLength(20);
        builder.Property(c => c.IsActive).HasColumnName("is_active");
        builder.Property(c => c.SortOrder).HasColumnName("sort_order");
    }
}
