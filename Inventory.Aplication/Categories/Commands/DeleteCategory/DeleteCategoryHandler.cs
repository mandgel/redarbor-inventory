namespace Inventory.Application.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryHandler
{
    private readonly ICategoryWriteStore _store;

    public DeleteCategoryHandler(ICategoryWriteStore store)
    {
        _store = store;
    }

    public Task<bool> HandleAsync(
        DeleteCategoryCommand command,
        CancellationToken cancellationToken)
    {
        return _store.DeleteAsync(
            command.Id,
            cancellationToken);
    }
}
