namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public sealed class IdempotencyConflictException : Exception
{
    public IdempotencyConflictException()
        : base("The idempotency key has already been used for a different request.")
    {
    }
}