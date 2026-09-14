namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public sealed record IdempotencyRecord
{
    public required string IdempotencyKey { get; init; }
    public required string RequestFingerprint { get; init; }
    public required RegisterInventoryMovementResult Result { get; init; }
}