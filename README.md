# Inventory API

RESTful inventory management API built with .NET 8 and SQL Server for
the technical challenge.

The solution implements product and category management,
inbound/outbound inventory movements, transactional stock updates,
idempotency, OAuth2/OIDC authentication, permission-based authorization,
automated tests, Swagger/OpenAPI documentation, and a reproducible
Docker environment.

## Architecture

The solution uses a lightweight layered architecture with explicit
CQRS-style command/query handlers.

``` text
Inventory.Api
    HTTP contracts
    Authentication / authorization
    ProblemDetails
    Swagger / OpenAPI
        |
Inventory.Application
    Commands
    Queries
    Handlers
    DTOs
    Use-case abstractions
        |
Inventory.Domain
    Entities
    Inventory policy concepts
        |
Inventory.Infrastructure
    EF Core queries
    Dapper commands
    SQL Server persistence
```

Runtime infrastructure:

``` text
SQL Server ---> Inventory.Migrator ---> Inventory.Api
                    Keycloak --------> Inventory.Api
```

### Main technical decisions

-   .NET 8.
-   SQL Server 2022.
-   Entity Framework Core for reads.
-   Dapper for writes.
-   Explicit CQRS-style handlers without MediatR.
-   Soft deletion for products and categories.
-   Materialized current stock in `InventoryBalances`.
-   Immutable inventory movement history.
-   Atomic SQL updates for concurrency-safe stock changes.
-   `Idempotency-Key` plus deterministic SHA-256 request fingerprinting.
-   OAuth2/OIDC with Keycloak.
-   Claim-based authorization policies.
-   RFC-style `ProblemDetails` error responses.
-   EF Core migrations executed by a dedicated one-shot migrator rather
    than by API startup.

For the original design specification and the implementation decisions
that evolved during development, see `SPEC.md`.

For AI-assisted development evidence and human review decisions, see
`AI_DEVELOPMENT.md`.

## Requirements

For the recommended Docker workflow:

-   Docker Desktop or Docker Engine with Docker Compose.
-   Ports `1433`, `5177`, and `8080` available.

A local SQL Server installation is not required.

For development outside Docker:

-   .NET 8 SDK.
-   Docker is still recommended for SQL Server and Keycloak.

## Environment configuration

Create a local `.env` file from `.env.example`.

PowerShell:

``` powershell
Copy-Item .env.example .env
```

Bash:

``` bash
cp .env.example .env
```

Configure the development secrets required by the compose file,
including:

``` text
SQL_SERVER_PASSWORD
KEYCLOAK_ADMIN
KEYCLOAK_ADMIN_PASSWORD
```

Do not commit `.env` or real credentials.

Runtime configuration follows standard ASP.NET Core environment-variable
conventions, including:

``` text
ConnectionStrings__InventoryDatabase
Authentication__Authority
Authentication__Audience
Inventory__NegativeStockPolicy
```

The default negative-stock policy is:

``` text
Reject
```

## Windows prerequisite for Keycloak

The containerized API and the host use the same Keycloak issuer:

``` text
http://keycloak.localhost:8080
```

On Windows, add this entry to the hosts file if `keycloak.localhost`
does not resolve automatically:

``` text
127.0.0.1 keycloak.localhost
```

The hosts file is normally located at:

``` text
C:\Windows\System32\drivers\etc\hosts
```

An elevated editor is required to modify it.

If Windows still uses a cached DNS result:

``` powershell
ipconfig /flushdns
```

This hostname is also configured as a Docker network alias, allowing the
browser and the containerized API to use the same OIDC issuer.

## Start the complete environment

From the repository root:

``` bash
docker compose up --build
```

This starts:

1.  SQL Server.
2.  `Inventory.Migrator` after SQL Server becomes healthy.
3.  Keycloak and imports the `inventory` realm.
4.  `Inventory.Api` after the database migration process completes
    successfully.

The API does **not** execute migrations during normal startup.

Available services:

``` text
Swagger:  http://localhost:5177/swagger
Keycloak: http://keycloak.localhost:8080
SQL:      localhost:1433
```

To stop the environment:

``` bash
docker compose down
```

To rebuild after code changes:

``` bash
docker compose up --build
```

## Database migrations

Schema evolution uses EF Core migrations stored in
`Inventory.Infrastructure`.

During Docker startup, the dedicated `Inventory.Migrator` service:

``` text
SQL Server healthy
        |
        v
Inventory.Migrator
        |
        v
MigrateAsync()
        |
        v
exit 0
        |
        v
Inventory.Api starts
```

This keeps deployment/schema responsibilities separate from API runtime
responsibilities.

To inspect migrator output:

``` bash
docker compose logs migrator
```

A successful execution should report that the Inventory database
migrations were applied successfully.

## Authentication and authorization

Authentication is provided by the local Keycloak `inventory` realm.

The API validates JWT bearer access tokens using:

``` text
Authority: http://keycloak.localhost:8080/realms/inventory
Audience:  inventory-api
```

Identity and credential ownership remains in Keycloak; the Inventory
database does not store application passwords or authentication users.

### Test identities

The imported realm contains two development identities:

``` text
inventory-reader
inventory-admin
```

`inventory-reader` has read-only permissions.

`inventory-admin` has the complete set of API permissions.

Development credentials are defined by the local Keycloak realm
configuration and are intended only for local evaluation. They must not
be reused as production credentials.

### Permissions

The authorization model uses permission claims rather than coupling
controllers directly to Keycloak roles.

``` text
products.read
products.create
products.update
products.delete

categories.read
categories.create
categories.update
categories.delete

inventory.read
inventory.create
```

Expected authorization behavior:

``` text
No/invalid token                  -> 401 Unauthorized
Valid token without permission    -> 403 Forbidden
Valid token with permission       -> endpoint executes normally
```

## Swagger authentication

Open:

``` text
http://localhost:5177/swagger
```

Swagger exposes the API security scheme and protected endpoints.

Obtain an access token from the local Keycloak `inventory` realm using
one of the development users, then authorize Swagger with the bearer
token before executing protected endpoints.

The imported Keycloak configuration includes the clients required for
local API/Swagger evaluation.

## API endpoints

### Categories

``` text
POST   /api/categories
GET    /api/categories
GET    /api/categories/{id}
PUT    /api/categories/{id}
DELETE /api/categories/{id}
```

Authorization:

``` text
GET       categories.read
POST      categories.create
PUT       categories.update
DELETE    categories.delete
```

Collection pagination:

``` text
GET /api/categories?page=1&pageSize=20
```

`pageSize` is bounded to a maximum of 100.

Deletion is logical. Deleted categories are excluded from normal reads.

### Products

``` text
POST   /api/products
GET    /api/products
GET    /api/products/{id}
PUT    /api/products/{id}
DELETE /api/products/{id}
```

Authorization:

``` text
GET       products.read
POST      products.create
PUT       products.update
DELETE    products.delete
```

Product list queries support pagination and the implemented filters
exposed by Swagger, including search, SKU, category, and active status.

Products use soft deletion. Creating a product also creates its
`InventoryBalance` with zero stock atomically.

SKU is unique.

A product can only reference a category that exists, is active, and is
not deleted.

### Inventory movements

``` text
POST /api/inventory-movements
```

Required permission:

``` text
inventory.create
```

The request requires:

``` text
Idempotency-Key: <client-generated-key>
```

Supported movement types:

``` text
1 = Inbound
2 = Outbound
```

Quantity must be greater than zero.

With the default `Reject` negative-stock policy, an outbound movement
that exceeds available stock returns `409 Conflict` and does not modify
inventory.

## Idempotency

Inventory writes use an idempotency key plus a deterministic request
fingerprint.

Behavior:

``` text
same key + same payload
    -> replay previous successful result
    -> stock is not modified again

different key + same payload
    -> new inventory operation
    -> stock is modified

same key + different payload
    -> 409 Conflict
    -> no additional stock modification
```

The fingerprint is SHA-256 based and protects against accidentally
reusing the same key for a different intention.

Balance modification, movement persistence, and idempotency handling
execute within the same SQL Server transaction.

## Error responses

Business and validation failures use structured `ProblemDetails`
responses.

Example:

``` json
{
  "type": "https://httpstatuses.com/409",
  "title": "Conflict",
  "status": 409,
  "detail": "The idempotency key has already been used for a different request.",
  "instance": "/api/inventory-movements",
  "traceId": "..."
}
```

Important status codes:

``` text
200 OK
201 Created
204 No Content
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
500 Internal Server Error
```

Unexpected exceptions are converted to a generic `500` response without
exposing stack traces to clients.

`traceId` can be correlated with application logs for diagnostics.

## Run tests

### Unit tests

``` bash
dotnet test Inventory.UnitTests
```

Current verified result:

``` text
7/7 passing
```

### Integration tests

Integration tests run against SQL Server and require
`ConnectionStrings__InventoryDatabase` to point to the test
database/server.

Example PowerShell environment-variable syntax:

``` powershell
$env:ConnectionStrings__InventoryDatabase="<local integration-test connection string>"
dotnet test Inventory.IntegrationTests
```

Current verified result:

``` text
4/4 passing
```

Complete verified test result:

``` text
11/11 passing
```

Integration tests cover infrastructure-dependent inventory guarantees
including persistence/idempotency behavior and missing-product handling.

## Run the API locally for debugging

The complete Docker workflow is the recommended evaluation path.

For local API debugging, SQL Server and Keycloak may remain in Docker
while the API runs from the host.

Ensure the required environment variables/configuration are available,
then run:

``` bash
dotnet run --project ./Api/Inventory.Api.csproj
```

The local launch URL may differ from the Docker-mapped `5177` port
depending on the launch profile.

When debugging OAuth locally, the configured Authority and the token
issuer must match exactly.

## Diagnostics and troubleshooting

### Inspect containers

``` bash
docker compose ps
```

### Follow all logs

``` bash
docker compose logs -f
```

### API logs

``` bash
docker compose logs -f api
```

### SQL Server logs

``` bash
docker compose logs -f sql-server
```

### Migrator logs

``` bash
docker compose logs migrator
```

### Keycloak logs

``` bash
docker compose logs -f keycloak
```

### SQL Server does not become healthy

Check:

-   `SQL_SERVER_PASSWORD` is configured in `.env`.
-   Port `1433` is available.
-   The SQL Server container logs do not report password-policy/startup
    failures.

Then inspect:

``` bash
docker compose ps
docker compose logs sql-server
```

### Migrator fails

The API intentionally waits for the migrator to complete.

Inspect:

``` bash
docker compose logs migrator
```

Verify that the internal connection string targets the Docker service
name `sql-server`, not `localhost`.

### Keycloak or authentication fails

Verify that:

``` text
http://keycloak.localhost:8080/realms/inventory/.well-known/openid-configuration
```

is reachable from the host.

The discovery document issuer must be:

``` text
http://keycloak.localhost:8080/realms/inventory
```

If Windows cannot resolve `keycloak.localhost`, configure the hosts-file
entry described above.

A `401` normally indicates missing/invalid authentication,
issuer/audience mismatch, or an expired token.

A `403` means authentication succeeded but the token does not contain
the permission required by the endpoint.

### ProblemDetails and TraceId

Error responses contain a `traceId`.

Use the value to correlate the failing HTTP request with the API's
structured logs:

``` bash
docker compose logs api
```

## Verified smoke tests

The final Dockerized stack was manually verified with:

``` text
No token
    -> protected endpoint returns 401

inventory-reader
    -> GET /api/products returns 200
    -> POST /api/products returns 403

inventory-admin
    -> POST /api/categories returns 201

Swagger
    -> http://localhost:5177/swagger returns 200

Keycloak discovery
    -> issuer matches http://keycloak.localhost:8080/realms/inventory
```

Inventory idempotency was also manually verified:

``` text
same payload + same Idempotency-Key
    -> replay without applying stock twice

same payload + different Idempotency-Key
    -> new movement applied

different payload + same Idempotency-Key
    -> 409 Conflict
```

### Obtain a bearer token with PowerShell

The imported Keycloak realm contains development-only test users:

```text
Admin
Username: inventory-admin
Password: AdminDev_2026!

Reader
Username: inventory-reader
Password: ReaderDev_2026!
```

To obtain an access token for `inventory-admin`:

```powershell
$body = @{
    client_id  = "inventory-swagger"
    grant_type = "password"
    username   = "inventory-admin"
    password   = "AdminDev_2026!"
}

$response = Invoke-RestMethod `
    -Method Post `
    -Uri "http://keycloak.localhost:8080/realms/inventory/protocol/openid-connect/token" `
    -ContentType "application/x-www-form-urlencoded" `
    -Body $body

$response.access_token
```

To copy the token directly to the clipboard:

```powershell
$response.access_token | Set-Clipboard
```

Open `http://localhost:5177/swagger`, select **Authorize**, and use the generated bearer token.

To verify permission enforcement, repeat the request using `inventory-reader` and `ReaderDev_2026!`.

## Project scope

Implemented in the final MVP:

-   Product CRUD.
-   Category CRUD.
-   Inbound/outbound inventory movements.
-   Soft deletion.
-   Filtering and pagination.
-   Configurable negative-stock behavior.
-   Atomic stock/movement persistence.
-   Concurrency-safe conditional stock updates.
-   Idempotent inventory writes.
-   EF Core reads.
-   Dapper writes.
-   Lightweight CQRS.
-   OAuth2/OIDC.
-   Permission-based authorization.
-   ProblemDetails.
-   Swagger/OpenAPI.
-   Docker Compose.
-   Dedicated database migrator.
-   Unit and integration tests.

Deliberately deferred from the broader design specification:

-   PATCH endpoints.
-   Public inventory-movement history query endpoints.
-   Dedicated `GET /api/products/{id}/inventory`.
-   Client-configurable sorting.
-   Broader non-critical test matrix.

These scope decisions are documented in `SPEC.md` and
`AI_DEVELOPMENT.md`.

## AI-assisted development

AI tools were used as implementation and review accelerators during the
challenge.

The repository includes:

``` text
SPEC.md
AI_DEVELOPMENT.md
```

`SPEC.md` records the design/specification used to guide implementation
and explicitly identifies capabilities later deferred from the final
MVP.

`AI_DEVELOPMENT.md` documents the main prompt areas,
accepted/modified/rejected AI proposals, human review and corrections,
final architecture, and the requested technical decision-making answers.

## Final verification

Before submission, the following sequence is recommended:

``` bash
docker compose down
docker compose up --build
```

Then verify:

``` text
SQL Server       healthy
Migrator         completed successfully
Keycloak         inventory realm imported
Inventory.Api    running
Swagger          http://localhost:5177/swagger
Tests            11/11 passing
```

The final implementation was verified with the complete stack running in
Docker.

## Decision-Making Questions

### 1.1 Tell us about a recent technical decision you made with incomplete information. How did you proceed?

A recent example was the transaction boundary for inventory movements. At first, separate abstractions for inventory balance and idempotency looked clean, but before the SQL implementation existed it was not completely clear whether that separation would remain practical.

I proceeded by defining the invariant first: changing current stock and recording the successful movement/idempotency result must be atomic. Once the Dapper implementation made the transaction boundary concrete, the separate stores made that guarantee harder to express. I changed the design to a single `IInventoryMovementStore` whose implementation owns one SQL connection and transaction for the complete operation.

The important part was treating the original abstraction as provisional and using the business invariant to decide when more implementation information became available.

### 1.2 When two approaches seem equally valid, how do you decide which one to use?

I compare them against the concrete constraints of the project rather than choosing the more sophisticated option by default. I look at correctness, clarity of the resulting code, operational complexity, testability, dependencies introduced, and how difficult the decision will be to reverse later.

For this project, explicit CQRS handlers and MediatR could both have worked. Explicit handlers were selected because the project is small enough that a mediator would mostly add indirection and another dependency. The simpler option still preserved the command/query separation required by the architecture.

When the trade-off remains genuinely close, I prefer the option with fewer moving parts and a clear path to evolve later.

### 1.3 Describe a decision you made that you later had to reverse or change. What did you learn?

The initial inventory design used separate balance and idempotency stores. I changed that decision when implementing the SQL transaction because the abstractions divided an operation that needed one atomic persistence boundary.

The replacement `IInventoryMovementStore` is less generic but expresses the actual use case more accurately: validate/replay idempotency, modify stock safely, persist the movement, and commit or roll back as one operation.

The lesson was that smaller abstractions are not automatically better abstractions. A boundary should follow the consistency requirement of the business operation. It is better to change an early design than to preserve it and compensate with fragile transaction coordination later.
