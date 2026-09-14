using Inventory.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Name).HasMaxLength(150).IsRequired();
        builder.Property(product => product.Description).HasColumnType("varchar(max)");
        builder.Property(product => product.Sku).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.HasIndex(product => product.Sku).IsUnique();
        builder.Property(product => product.IsActive).IsRequired();
        builder.Property(product => product.IsDeleted).IsRequired();
        builder.HasOne<Domain.Categories.Category>()
          .WithMany()
          .HasForeignKey(product => product.CategoryId)
          .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(product => !product.IsDeleted);
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
