using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public sealed class RegisterInventoryMovementHandler
{
    private readonly IInventoryBalanceStore _store;
    public RegisterInventoryMovementHandler(IInventoryBalanceStore store)
    {
        _store = store;
    }
    public async Task<RegisterInventoryMovementResult> HandleAsync(
        RegisterInventoryMovementCommand command,
        CancellationToken cancellationToken)
    {
        var currentStock = await _store.ApplyMovementAsync(
            command,
            cancellationToken);

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