using Inventory.Application.Inventory.Commands.RegisterInventoryMovement;
using Inventory.Domain.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static System.Formats.Asn1.AsnWriter;

namespace Inventory.UnitTests.Inventory;

public sealed class RegisterInventoryMovementHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenOutboundMovementIsValid_ShouldReturnUpdatedStock()
    {
        var productId = Guid.NewGuid();

        var command = new RegisterInventoryMovementCommand
        {
            ProductId = productId,
            MovementType = InventoryMovementType.Outbound,
            Quantity = 4m,
            IdempotencyKey = "test-key-001"
        };

        var store = new FakeInventoryBalanceStore(10m);
        var handler = new RegisterInventoryMovementHandler(store);

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.Equal(productId, result.ProductId);
        Assert.Equal(InventoryMovementType.Outbound, result.MovementType);
        Assert.Equal(4m, result.Quantity);
        Assert.Equal(6m, result.CurrentStock);
    }
}