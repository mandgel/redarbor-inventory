using Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

internal sealed class FakeIdempotencyStore : IIdempotencyStore
{
    private readonly Dictionary<string, RegisterInventoryMovementResult> _results = [];

    public Task<RegisterInventoryMovementResult?> GetAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        _results.TryGetValue(idempotencyKey, out var result);

        return Task.FromResult(result);
    }

    public Task SaveAsync(
        string idempotencyKey,
        RegisterInventoryMovementResult result,
        CancellationToken cancellationToken)
    {
        _results[idempotencyKey] = result;

        return Task.CompletedTask;
    }
}