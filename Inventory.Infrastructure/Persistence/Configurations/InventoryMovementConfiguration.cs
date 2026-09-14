using Inventory.Domain.Inventory;
using Inventory.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class InventoryMovementConfiguration
    : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements");
        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.Id)
            .ValueGeneratedOnAdd();
        builder.Property(movement => movement.Quantity)
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(movement => movement.StockAfterMovement)
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(movement => movement.IdempotencyKey)
            .HasMaxLength(100)
             .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(movement => movement.IdempotencyKey)
            .IsUnique();

        builder.Property(movement => movement.RequestFingerprint)
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsFixedLength()
            .IsRequired();
        builder.Property(movement => movement.MovementType)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(movement => movement.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(
            "InventoryMovements",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_InventoryMovements_MovementType",
                    "MovementType IN (1, 2)");

                table.HasCheckConstraint(
                    "CK_InventoryMovements_Quantity",
                    "Quantity > 0");
            });
    }
}