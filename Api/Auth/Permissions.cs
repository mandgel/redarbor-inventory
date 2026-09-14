namespace Inventory.Api.Auth;

public static class Permissions
{
    public const string ProductsRead = "products.read";
    public const string ProductsCreate = "products.create";
    public const string ProductsUpdate = "products.update";
    public const string ProductsDelete = "products.delete";

    public const string CategoriesRead = "categories.read";
    public const string CategoriesCreate = "categories.create";
    public const string CategoriesUpdate = "categories.update";
    public const string CategoriesDelete = "categories.delete";

    public const string InventoryRead = "inventory.read";
    public const string InventoryCreate = "inventory.create";
}
