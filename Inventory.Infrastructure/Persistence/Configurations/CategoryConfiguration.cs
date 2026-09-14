using Inventory.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).HasMaxLength(150).IsRequired();
        builder.Property(category => category.IsActive).IsRequired();
        builder.Property(category => category.IsDeleted).IsRequired();
        builder.HasQueryFilter(category => !category.IsDeleted);
        builder.Property(x => x.CreatedAt)
          .HasColumnType("datetime2")
          .IsRequired();
        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .IsRequired();
        builder.Property(x => x.DeletedAt)
            .HasColumnType("datetime2");
    }
}
