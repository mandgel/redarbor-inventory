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
        var idempotencyStore = new FakeIdempotencyStore();
        var handler = new RegisterInventoryMovementHandler(store, idempotencyStore, NegativeStockPolicy.Reject);

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.Equal(productId, result.ProductId);
        Assert.Equal(InventoryMovementType.Outbound, result.MovementType);
        Assert.Equal(4m, result.Quantity);
        Assert.Equal(6m, result.CurrentStock);
    }
    [Fact]
    public async Task HandleAsync_WhenOutboundMovementExceedsStock_ShouldThrowInsufficientStockException()
    {
        var productId = Guid.NewGuid();

        var command = new RegisterInventoryMovementCommand
        {
            ProductId = productId,
            MovementType = InventoryMovementType.Outbound,
            Quantity = 5m,
            IdempotencyKey = "test-key-002"
        };

        var store = new FakeInventoryBalanceStore(
            currentStock: 3m);

        var idempotencyStore = new FakeIdempotencyStore();
        var handler = new RegisterInventoryMovementHandler(store, idempotencyStore, NegativeStockPolicy.Reject);

        await Assert.ThrowsAsync<InsufficientStockException>(
            () => handler.HandleAsync(
                command,
                CancellationToken.None));

        Assert.Equal(3m, store.CurrentStock);
    }
    [Fact]
    public async Task HandleAsync_WhenNegativeStockIsAllowed_ShouldApplyOutboundMovement()
    {
        var productId = Guid.NewGuid();

        var command = new RegisterInventoryMovementCommand
        {
            ProductId = productId,
            MovementType = InventoryMovementType.Outbound,
            Quantity = 5m,
            IdempotencyKey = "test-key-003"
        };

        var store = new FakeInventoryBalanceStore(
            currentStock: 3m);

        var idempotencyStore = new FakeIdempotencyStore();
        var handler = new RegisterInventoryMovementHandler(
            store,
            idempotencyStore,
            NegativeStockPolicy.Allow);

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.Equal(-2m, result.CurrentStock);
        Assert.Equal(-2m, store.CurrentStock);
    }
    [Fact]
    public async Task HandleAsync_WhenSameRequestIsRetried_ShouldNotApplyMovementTwice()
    {
        var productId = Guid.NewGuid();

        var command = new RegisterInventoryMovementCommand
        {
            ProductId = productId,
            MovementType = InventoryMovementType.Outbound,
            Quantity = 4m,
            IdempotencyKey = "test-key-004"
        };

        var store = new FakeInventoryBalanceStore(
            currentStock: 10m);

        var idempotencyStore = new FakeIdempotencyStore();

        var handler = new RegisterInventoryMovementHandler(
            store,
            idempotencyStore,
            NegativeStockPolicy.Reject);

        var firstResult = await handler.HandleAsync(
            command,
            CancellationToken.None);

        var secondResult = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.Equal(6m, firstResult.CurrentStock);
        Assert.Equal(6m, secondResult.CurrentStock);
        Assert.Equal(6m, store.CurrentStock);
        Assert.Equal(
            firstResult.MovementId,
            secondResult.MovementId);
    }
    [Fact]
    public async Task HandleAsync_WhenIdempotencyKeyIsReusedWithDifferentPayload_ShouldThrowIdempotencyConflictException()
    {
        var productId = Guid.NewGuid();

        var firstCommand = new RegisterInventoryMovementCommand
        {
            ProductId = productId,
            MovementType = InventoryMovementType.Outbound,
            Quantity = 4m,
            IdempotencyKey = "test-key-005"
        };

        var secondCommand = new RegisterInventoryMovementCommand
        {
            ProductId = productId,
            MovementType = InventoryMovementType.Outbound,
            Quantity = 2m,
            IdempotencyKey = "test-key-005"
        };

        var store = new FakeInventoryBalanceStore(
            currentStock: 10m);

        var idempotencyStore = new FakeIdempotencyStore();

        var handler = new RegisterInventoryMovementHandler(
            store,
            idempotencyStore,
            NegativeStockPolicy.Reject);

        await handler.HandleAsync(
            firstCommand,
            CancellationToken.None);

        await Assert.ThrowsAsync<IdempotencyConflictException>(
            () => handler.HandleAsync(
                secondCommand,
                CancellationToken.None));

        Assert.Equal(6m, store.CurrentStock);
    }
}