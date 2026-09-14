using Inventory.Domain.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public sealed class RegisterInventoryMovementHandler
{
    private readonly IInventoryBalanceStore _store;
    private readonly NegativeStockPolicy _negativeStockPolicy;
    public RegisterInventoryMovementHandler(IInventoryBalanceStore store, 
        NegativeStockPolicy negativeStockPolicy)
    {
        _store = store;
        _negativeStockPolicy = negativeStockPolicy;
    }
    public async Task<RegisterInventoryMovementResult> HandleAsync(
        RegisterInventoryMovementCommand command,
        CancellationToken cancellationToken)
    {
        var change = CreateBalanceChange(command);
        var balanceResult = await _store.ApplyMovementAsync(
            change,
            cancellationToken);

        if (!balanceResult.Applied)
        {
            throw new InsufficientStockException();
        }

        return CreateResult(
            command,
            balanceResult.CurrentStock!.Value);
    }

    private InventoryBalanceChange CreateBalanceChange(
    RegisterInventoryMovementCommand command)
    {
        return new InventoryBalanceChange
        {
            ProductId = command.ProductId,
            QuantityChange = GetQuantityChange(command),
            PreventNegativeStock =
            _negativeStockPolicy == NegativeStockPolicy.Reject
        };
    }

    private static decimal GetQuantityChange(
    RegisterInventoryMovementCommand command)
    {
        return command.MovementType switch
        {
            InventoryMovementType.Inbound => command.Quantity,
            InventoryMovementType.Outbound => -command.Quantity,
            _ => throw new ArgumentOutOfRangeException(
                nameof(command.MovementType))
        };
    }

    private static RegisterInventoryMovementResult CreateResult(
    RegisterInventoryMovementCommand command,
    decimal currentStock)
    {
        return new RegisterInventoryMovementResult
        {
            MovementId = 1,
            ProductId = command.ProductId,
            MovementType = command.MovementType,
            Quantity = command.Quantity,
            CurrentStock = currentStock,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}