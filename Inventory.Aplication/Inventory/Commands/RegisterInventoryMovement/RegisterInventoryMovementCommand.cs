using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Inventory.Domain.Inventory;

namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public sealed record RegisterInventoryMovementCommand
{
    public required Guid ProductId { get; init; }
    public required InventoryMovementType MovementType { get; init; }
    public required decimal Quantity { get; init; }
    public required string IdempotencyKey { get; init; }
}