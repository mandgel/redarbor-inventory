using Inventory.Application.Categories;
using Inventory.Api.Common.Errors;
using Inventory.Application.Categories.Commands.CreateCategory;
using Inventory.Application.Categories.Commands.DeleteCategory;
using Inventory.Application.Categories.Commands.UpdateCategory;
using Inventory.Application.Categories.Queries.GetCategories;
using Inventory.Application.Categories.Queries.GetCategoryById;
using Inventory.Application.Inventory.Commands.RegisterInventoryMovement;
using Inventory.Application.Products;
using Inventory.Application.Products.Commands.CreateProduct;
using Inventory.Application.Products.Commands.DeleteProduct;
using Inventory.Application.Products.Commands.UpdateProduct;
using Inventory.Application.Products.Queries.GetProductById;
using Inventory.Application.Products.Queries.GetProducts;
using Inventory.Domain.Inventory;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence.Commands;
using Inventory.Infrastructure.Persistence.Queries;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
    .GetConnectionString("InventoryDatabase")
    ?? throw new InvalidOperationException(
        "Inventory database connection string is not configured.");

var negativeStockPolicyValue = builder.Configuration[
    "Inventory:NegativeStockPolicy"] ?? "Reject";

var negativeStockPolicy = Enum.Parse<NegativeStockPolicy>(
    negativeStockPolicyValue,
    ignoreCase: true);

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddScoped<IInventoryMovementStore>(
    _ => new DapperInventoryMovementStore(connectionString));

builder.Services.AddScoped(
    provider => new RegisterInventoryMovementHandler(
        provider.GetRequiredService<IInventoryMovementStore>(),
        negativeStockPolicy));

builder.Services.AddScoped<ICategoryWriteStore>(
    _ => new DapperCategoryWriteStore(connectionString));
builder.Services.AddScoped<ICategoryReadStore, EfCategoryReadStore>();

builder.Services.AddScoped<CreateCategoryHandler>();
builder.Services.AddScoped<UpdateCategoryHandler>();
builder.Services.AddScoped<DeleteCategoryHandler>();
builder.Services.AddScoped<GetCategoriesHandler>();
builder.Services.AddScoped<GetCategoryByIdHandler>();

builder.Services.AddScoped<IProductWriteStore>(
    _ => new DapperProductWriteStore(connectionString));
builder.Services.AddScoped<IProductReadStore, EfProductReadStore>();

builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddScoped<UpdateProductHandler>();
builder.Services.AddScoped<DeleteProductHandler>();
builder.Services.AddScoped<GetProductsHandler>();
builder.Services.AddScoped<GetProductByIdHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();