using Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

namespace Inventory.UnitTests.Inventory;

internal sealed class FakeIdempotencyStore : IIdempotencyStore
{
    private readonly Dictionary<string, IdempotencyRecord> _records = [];

    public Task<IdempotencyRecord?> GetAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        _records.TryGetValue(idempotencyKey, out var record);

        return Task.FromResult(record);
    }

    public Task SaveAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken)
    {
        _records[record.IdempotencyKey] = record;

        return Task.CompletedTask;
    }
}