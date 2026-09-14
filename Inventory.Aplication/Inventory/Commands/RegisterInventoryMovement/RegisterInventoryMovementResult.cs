using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Inventory.Domain.Inventory;

namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public sealed record RegisterInventoryMovementResult
{
    public required long MovementId { get; init; }
    public required Guid ProductId { get; init; }
    public required InventoryMovementType MovementType { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal CurrentStock { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}