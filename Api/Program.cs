using Inventory.Application.Inventory.Commands.RegisterInventoryMovement;
using Inventory.Domain.Inventory;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence.Commands;
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

builder.Services.AddScoped<IInventoryMovementStore>(
    _ => new DapperInventoryMovementStore(connectionString));

builder.Services.AddScoped(
    provider => new RegisterInventoryMovementHandler(
        provider.GetRequiredService<IInventoryMovementStore>(),
        negativeStockPolicy));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();