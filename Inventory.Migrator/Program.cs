using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("InventoryDatabase")
    ?? throw new InvalidOperationException(
        "ConnectionStrings__InventoryDatabase is not configured.");

var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
optionsBuilder.UseSqlServer(connectionString);

await using var dbContext = new InventoryDbContext(optionsBuilder.Options);

Console.WriteLine("Applying Inventory database migrations...");

await dbContext.Database.MigrateAsync();

Console.WriteLine("Inventory database migrations applied successfully.");

return 0;
