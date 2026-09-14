namespace Inventory.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryHandler
{
    private readonly ICategoryWriteStore _store;

    public CreateCategoryHandler(ICategoryWriteStore store)
    {
        _store = store;
    }

    public Task<CategoryDto> HandleAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        return _store.CreateAsync(
            command.Name,
            command.IsActive,
            cancellationToken);
    }
}
