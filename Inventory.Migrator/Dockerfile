FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Inventory.sln ./
COPY Inventory.Migrator/Inventory.Migrator.csproj Inventory.Migrator/
COPY Inventory.Infrastructure/Inventory.Infrastructure.csproj Inventory.Infrastructure/
COPY Inventory.Aplication/Inventory.Application.csproj Inventory.Aplication/
COPY Inventory.Domain/Inventory.Domain.csproj Inventory.Domain/

RUN dotnet restore Inventory.Migrator/Inventory.Migrator.csproj

COPY Inventory.Migrator/ Inventory.Migrator/
COPY Inventory.Infrastructure/ Inventory.Infrastructure/
COPY Inventory.Aplication/ Inventory.Aplication/
COPY Inventory.Domain/ Inventory.Domain/

RUN dotnet publish Inventory.Migrator/Inventory.Migrator.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Inventory.Migrator.dll"]
