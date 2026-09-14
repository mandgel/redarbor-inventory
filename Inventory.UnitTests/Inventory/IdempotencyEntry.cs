using Inventory.Application.Inventory.Commands.RegisterInventoryMovement;
using Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

namespace Inventory.UnitTests.Inventory;

internal sealed record IdempotencyEntry(
    string Fingerprint,
    RegisterInventoryMovementResult Result);
