using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Inventory.Application.Inventory.Commands.RegisterInventoryMovement;
using Inventory.Domain.Inventory;
namespace Inventory.UnitTests.Inventory;
internal sealed class FakeInventoryBalanceStore : IInventoryBalanceStore
{
    public decimal CurrentStock { get; private set; }
    public FakeInventoryBalanceStore(decimal currentStock)
    {
        CurrentStock = currentStock;
    }
    public Task<decimal> ApplyMovementAsync(
        InventoryBalanceChange change,
        CancellationToken cancellationToken)
    { 
        CurrentStock += change.QuantityChange;

        return Task.FromResult(CurrentStock);  
    }
}