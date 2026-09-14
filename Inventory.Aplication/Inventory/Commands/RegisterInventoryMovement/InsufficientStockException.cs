using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public sealed class InsufficientStockException : Exception
{
    public InsufficientStockException()
        : base("Insufficient stock to complete the inventory movement.")
    {
    }
}