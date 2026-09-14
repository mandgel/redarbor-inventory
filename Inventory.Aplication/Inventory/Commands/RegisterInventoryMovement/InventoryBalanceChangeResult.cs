using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public sealed record InventoryBalanceChangeResult
{
    public required bool Applied { get; init; }
    public decimal? CurrentStock { get; init; }
}