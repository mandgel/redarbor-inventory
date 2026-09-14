namespace Inventory.Application.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryHandler
{
    private readonly ICategoryWriteStore _store;

    public UpdateCategoryHandler(ICategoryWriteStore store)
    {
        _store = store;
    }

    public Task<CategoryDto?> HandleAsync(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        return _store.UpdateAsync(
            command.Id,
            command.Name,
            command.IsActive,
            cancellationToken);
    }
}
