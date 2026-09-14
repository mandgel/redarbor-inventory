using Inventory.Domain.Inventory;
using Inventory.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable("InventoryBalances");
        builder.HasKey(balance => balance.ProductId);
        builder.Property(balance => balance.CurrentStock)
            .HasPrecision(18, 2)
            .IsRequired();
        builder.HasOne<Product>()
            .WithOne()
            .HasForeignKey<InventoryBalance>(
                balance => balance.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
