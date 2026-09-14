using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public sealed record InventoryBalanceChange
{
    public required Guid ProductId { get; init; }
    public required decimal QuantityChange { get; init; }
}