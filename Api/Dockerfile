FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Inventory.sln ./
COPY Api/Inventory.Api.csproj Api/
COPY Inventory.Application/Inventory.Application.csproj Inventory.Application/
COPY Inventory.Domain/Inventory.Domain.csproj Inventory.Domain/
COPY Inventory.Infrastructure/Inventory.Infrastructure.csproj Inventory.Infrastructure/
COPY Inventory.UnitTests/Inventory.UnitTests.csproj Inventory.UnitTests/
COPY Inventory.IntegrationTests/Inventory.IntegrationTests.csproj Inventory.IntegrationTests/
COPY Inventory.Migrator/Inventory.Migrator.csproj Inventory.Migrator/

RUN dotnet restore Api/Inventory.Api.csproj

COPY Api/ Api/
COPY Inventory.Application/ Inventory.Application/
COPY Inventory.Domain/ Inventory.Domain/
COPY Inventory.Infrastructure/ Inventory.Infrastructure/

RUN dotnet publish Api/Inventory.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
