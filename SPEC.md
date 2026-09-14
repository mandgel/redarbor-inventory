Inventory API — Technical Specification

1. Purpose

Build a RESTful inventory management API using .NET 8 or later and SQL Server.

The system must support:

- Product management.
- Category management.
- Inventory inbound and outbound movements.
- Current inventory balance queries.
- OAuth2/OIDC authentication.
- Permission-based authorization.
- Containerized execution.
- Automated testing.
- API documentation.
- Local debugging and diagnostics.

The solution should favor maintainability, explicit responsibilities, testability, reproducibility, and minimal accidental complexity.

---

2. Functional Scope

The API must support:

- Create, retrieve, update, partially update, and delete products.
- Create, retrieve, update, partially update, and delete categories.
- Register inbound inventory movements.
- Register outbound inventory movements.
- Retrieve inventory movement history.
- Retrieve current product inventory balances.
- Filter and paginate collection endpoints.
- Preserve historical inventory information.
- Protect endpoints through OAuth2/OIDC.
- Enforce operation permissions.

---

3. Domain Model

3.1 Category

Categories
- Id uniqueidentifier PK
- Name varchar(150) NOT NULL
- IsActive bit NOT NULL
- IsDeleted bit NOT NULL
- CreatedAt datetime2 NOT NULL
- UpdatedAt datetime2 NOT NULL
- DeletedAt datetime2 NULL

"IsActive" and "IsDeleted" represent different concepts.

- "IsActive = false": category exists but is disabled.
- "IsDeleted = true": category was logically deleted.

Categories use soft deletion.

---

3.2 Product

Products
- Id uniqueidentifier PK
- Name varchar(150) NOT NULL
- Description varchar(MAX) NULL
- SKU varchar(50) NOT NULL UNIQUE
- CategoryId uniqueidentifier NOT NULL FK
- IsActive bit NOT NULL
- IsDeleted bit NOT NULL
- CreatedAt datetime2 NOT NULL
- UpdatedAt datetime2 NOT NULL
- DeletedAt datetime2 NULL

Products use soft deletion.

Product deletion must preserve:

- Inventory history.
- Referential integrity.
- Audit information.

"SKU" uniquely identifies a product.

---

3.3 Inventory Movement

InventoryMovements
- Id bigint IDENTITY(1,1) PK
- ProductId uniqueidentifier NOT NULL FK
- MovementType tinyint NOT NULL
- Quantity decimal(18,2) NOT NULL
- IdempotencyKey varchar(100) NOT NULL UNIQUE
- RequestFingerprint char(64) NOT NULL
- CreatedAt datetime2 NOT NULL

Supported movement types:

1 = Inbound
2 = Outbound

Constraint:

Quantity > 0

Inventory movements are immutable historical records.

They cannot be updated or deleted through the public API.

---

3.4 Inventory Balance

InventoryBalances
- ProductId uniqueidentifier PK/FK
- CurrentStock decimal(18,2) NOT NULL
- UpdatedAt datetime2 NOT NULL

There is exactly one current balance per product.

"InventoryBalance" optimizes current-stock queries.

"InventoryMovement" preserves the full auditable inventory history.

Periodic inventory snapshots remain outside the MVP scope.

---

4. Identity Ownership

The inventory database does not store:

- Passwords.
- Authentication credentials.
- Application-owned users.
- Roles.
- Permission assignments.

Authentication identities, credentials, roles, claims, and permissions belong to the OAuth2/OIDC provider.

The API trusts the "sub" and authorization claims contained in valid access tokens.

---

5. Inventory Business Rules

5.1 Quantity

Inventory movement quantity must be greater than zero.

---

5.2 Product Availability

An inventory movement may only target a product that:

- Exists.
- Is active.
- Is not soft deleted.

---

5.3 Negative Stock

Negative-stock behavior is configurable.

Supported values:

Reject
Allow

Default:

Reject

When "Reject" is configured, outbound movements that exceed available stock return:

409 Conflict

No inventory state is modified.

When "Allow" is configured, outbound movements may create negative inventory balances.

---

5.4 Atomic Inventory Operations

Inventory balance modification and inventory movement persistence form one atomic operation.

Both execute inside the same SQL Server transaction.

BEGIN TRANSACTION

Update InventoryBalance
Insert InventoryMovement

COMMIT

If any operation fails:

ROLLBACK

No partially completed inventory operation may remain persisted.

---

5.5 Concurrent Inventory Updates

Outbound movements must avoid read-check-write race conditions.

When negative stock is rejected, balance modification uses an atomic conditional update.

Conceptually:

UPDATE InventoryBalances
SET CurrentStock = CurrentStock - @Quantity,
    UpdatedAt = @UpdatedAt
WHERE ProductId = @ProductId
  AND CurrentStock >= @Quantity;

Affected rows:

1 → operation accepted
0 → operation cannot be completed

This prevents concurrent requests from independently validating against the same previous stock value.

---

6. Idempotency

Inventory movement commands require:

Idempotency-Key

The key represents one client intention.

A deterministic SHA-256 fingerprint is generated from business-significant values:

ProductId
MovementType
Quantity

The fingerprint excludes transport-specific information such as:

TraceId
Request timestamp
JSON property order
Connection details

6.1 Same Key + Same Fingerprint

The request is treated as a retry of the same intention.

The movement is not executed again.

The existing successful result is returned.

---

6.2 Same Key + Different Fingerprint

The key represents a different intention.

Return:

409 Conflict

The response instructs the client to generate a new idempotency key.

Inventory is not modified.

---

6.3 Failed Transaction

An idempotency key is considered consumed only after a successful transaction commit.

If the transaction rolls back, the same key may be retried.

The database enforces uniqueness of successfully persisted idempotency keys.

---

7. Persistence Strategy

The application follows lightweight CQRS.

7.1 Queries

Read operations use Entity Framework Core.

Guidelines:

- Filter in SQL whenever practical.
- Project only required fields.
- Avoid unnecessary materialization.
- Use "AsNoTracking()" for read-only queries.
- Use global query filters for soft-deleted entities.

---

7.2 Commands

Write operations use Dapper.

Dapper handles:

- Product creation.
- Product updates.
- Product soft deletion.
- Category creation.
- Category updates.
- Category soft deletion.
- Inventory balance updates.
- Inventory movement inserts.

Multi-operation commands use explicit transactions.

---

8. CQRS

CQRS is implemented explicitly without MediatR.

Example structure:

Application/
├── Products/
│   ├── Commands/
│   └── Queries/
├── Categories/
│   ├── Commands/
│   └── Queries/
└── Inventory/
    ├── Commands/
    └── Queries/

Commands and queries use dedicated handlers.

Dependencies are provided through constructor dependency injection.

MediatR is intentionally excluded because the current project size does not justify the additional abstraction.

---

9. API Contract Principles

The API must not expose persistence/domain entities directly.

Dedicated request and response DTOs are used.

Example:

Product
CreateProductRequest
ReplaceProductRequest
PatchProductRequest
ProductResponse

The public contract must remain independent from database persistence details.

---

10. Pagination Contract

Collection endpoints use page-based pagination.

Supported parameters:

page
pageSize

Defaults:

page = 1
pageSize = 20

Maximum:

pageSize = 100

Invalid values return:

400 Bad Request

Paginated responses use:

{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 125,
  "totalPages": 7
}

---

11. Sorting Contract

Collection endpoints may support:

sortBy
sortDirection

Supported directions:

asc
desc

Sorting fields must be explicitly whitelisted.

Arbitrary SQL column names must never be accepted from client input.

Example:

GET /api/products?sortBy=name&sortDirection=asc

---

12. Product API

12.1 Create Product

POST /api/products

Permission:

products.create

Request:

{
  "name": "LED Headlamp",
  "description": "24V heavy-duty LED headlamp",
  "sku": "LED-001",
  "categoryId": "UUID",
  "isActive": true
}

Validation:

- "name" required.
- "name <= 150".
- "sku" required.
- "sku <= 50".
- "sku" must be unique.
- "categoryId" must identify an existing, active, non-deleted category.

Success:

201 Created

Response:

{
  "id": "UUID",
  "name": "LED Headlamp",
  "description": "24V heavy-duty LED headlamp",
  "sku": "LED-001",
  "categoryId": "UUID",
  "isActive": true,
  "createdAt": "UTC_TIMESTAMP",
  "updatedAt": "UTC_TIMESTAMP"
}

---

12.2 Get Product

GET /api/products/{id}

Permission:

products.read

Success:

200 OK

Not found:

404 Not Found

Example response:

{
  "id": "UUID",
  "name": "LED Headlamp",
  "description": "24V heavy-duty LED headlamp",
  "sku": "LED-001",
  "categoryId": "UUID",
  "categoryName": "Lighting",
  "isActive": true,
  "currentStock": 25.00,
  "createdAt": "UTC_TIMESTAMP",
  "updatedAt": "UTC_TIMESTAMP"
}

---

12.3 List Products

GET /api/products

Permission:

products.read

Supported query parameters:

page
pageSize
search
sku
categoryId
isActive
sortBy
sortDirection

Examples:

GET /api/products?page=1&pageSize=20

GET /api/products?search=headlamp

GET /api/products?categoryId={UUID}&isActive=true

GET /api/products?sortBy=name&sortDirection=asc

"search" may match:

Name
SKU

Soft-deleted products are excluded from normal queries.

---

12.4 Replace Product

PUT /api/products/{id}

Permission:

products.update

"PUT" represents replacement of all editable product properties.

Request:

{
  "name": "LED Headlamp Updated",
  "description": "Updated description",
  "sku": "LED-001",
  "categoryId": "UUID",
  "isActive": true
}

All editable fields must be provided.

Server-managed fields cannot be modified:

Id
CreatedAt
UpdatedAt
DeletedAt
IsDeleted
CurrentStock

Success:

200 OK

Not found:

404 Not Found

Invalid category or business state:

400 / 409 depending on failure semantics

---

12.5 Partially Update Product

PATCH /api/products/{id}

Permission:

products.update

"PATCH" modifies only explicitly supplied editable properties.

Example:

{
  "description": "New description",
  "isActive": false
}

Unspecified properties remain unchanged.

The MVP uses a typed partial-update contract rather than arbitrary client-provided SQL or persistence field names.

Success:

200 OK

No-op requests may return:

400 Bad Request

---

12.6 Delete Product

DELETE /api/products/{id}

Permission:

products.delete

Deletion performs soft delete.

Conceptually:

IsDeleted = true
IsActive = false
DeletedAt = current UTC timestamp

Historical inventory movements remain unchanged.

Success:

204 No Content

Not found:

404 Not Found

---

13. Category API

13.1 Create Category

POST /api/categories

Permission:

categories.create

Request:

{
  "name": "Lighting",
  "isActive": true
}

Success:

201 Created

---

13.2 Get Category

GET /api/categories/{id}

Permission:

categories.read

Success:

200 OK

Not found:

404 Not Found

---

13.3 List Categories

GET /api/categories

Supported parameters:

page
pageSize
search
isActive
sortBy
sortDirection

Example:

GET /api/categories?search=light&page=1&pageSize=20

---

13.4 Replace Category

PUT /api/categories/{id}

Permission:

categories.update

Request:

{
  "name": "Commercial Lighting",
  "isActive": true
}

All editable category fields must be supplied.

Success:

200 OK

---

13.5 Partially Update Category

PATCH /api/categories/{id}

Permission:

categories.update

Example:

{
  "isActive": false
}

Only supplied values are modified.

Success:

200 OK

---

13.6 Delete Category

DELETE /api/categories/{id}

Permission:

categories.delete

Categories are soft deleted.

A category containing products must not cause historical relationships to be removed.

Success:

204 No Content

---

14. Inventory Movement API

14.1 Register Inventory Movement

POST /api/inventory-movements

Permission:

inventory.create

Required header:

Idempotency-Key: <unique-client-key>

Request:

{
  "productId": "UUID",
  "movementType": "Outbound",
  "quantity": 5.00
}

Validation:

- ProductId required.
- Quantity > 0.
- Valid movement type.
- Product must exist.
- Product must be active.
- Product must not be deleted.

Success:

201 Created

Example response:

{
  "id": 125,
  "productId": "UUID",
  "movementType": "Outbound",
  "quantity": 5.00,
  "currentStock": 20.00,
  "createdAt": "UTC_TIMESTAMP"
}

Business conflicts:

409 Conflict

Examples:

- Insufficient stock.
- Idempotency key reused with a different payload.

---

15. Inventory Movement Queries

15.1 Get Movement

GET /api/inventory-movements/{id}

Permission:

inventory.read

Success:

200 OK

Not found:

404 Not Found

---

15.2 List Movements

GET /api/inventory-movements

Permission:

inventory.read

Supported query parameters:

page
pageSize
productId
movementType
from
to
sortDirection

Example:

GET /api/inventory-movements?productId={UUID}&movementType=Outbound

Date-range example:

GET /api/inventory-movements?from=2026-09-01T00:00:00Z&to=2026-09-30T23:59:59Z

Default sorting:

CreatedAt DESC

---

16. Inventory Balance Query

GET /api/products/{id}/inventory

Permission:

inventory.read

Response:

{
  "productId": "UUID",
  "currentStock": 25.00,
  "updatedAt": "UTC_TIMESTAMP"
}

---

17. PUT vs PATCH Semantics

PUT

"PUT" replaces the complete editable representation of a resource.

Clients must send all editable fields.

Example:

PUT /api/products/{id}

Suitable when:

- The caller knows the complete desired resulting state.
- Replacing all editable values is intentional.

PATCH

"PATCH" modifies only selected properties.

Suitable when:

- Only one or a few values need to change.
- Sending the complete representation would be unnecessary.

The MVP does not expose server-controlled fields through either operation.

---

18. Validation Responsibilities

Input Validation

Handles structural contract validity:

Required values
Maximum lengths
Positive quantities
Valid enum values
Valid UUIDs
Pagination boundaries
Supported sort fields

Invalid input:

400 Bad Request

Business Validation

Handles state-dependent rules:

Product existence
Category existence
Entity active/deleted state
Stock availability
SKU conflicts
Idempotency intent conflicts

Business conflicts map to the appropriate HTTP status.

---

19. Authentication and Authorization

Authentication uses a local OAuth2/OIDC provider.

Authorization uses claim-based ASP.NET Core policies.

Example permissions:

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

Authentication and authorization remain separate concerns.

---

20. HTTP Status Contract

200 OK
Successful read/update with response content.

201 Created
Resource or inventory movement successfully created.

204 No Content
Successful operation with no response body.

400 Bad Request
Request violates structural validation.

401 Unauthorized
Authentication missing or invalid.

403 Forbidden
Authenticated identity lacks required permission.

404 Not Found
Requested resource does not exist.

409 Conflict
Request conflicts with current system state.

500 Internal Server Error
Unexpected server-side failure.

---

21. Error Handling

Errors are represented using ASP.NET Core "ProblemDetails".

Business/application errors remain independent from HTTP concerns.

Example mapping:

ProductNotFound
→ 404

CategoryNotFound
→ 404

InsufficientStock
→ 409

SkuAlreadyExists
→ 409

IdempotencyConflict
→ 409

InputValidationFailure
→ 400

UnexpectedFailure
→ 500

Problem responses contain a trace identifier for diagnostics.

---

22. Logging and Diagnostics

Structured logging uses:

ILogger<T>

Relevant context may include:

TraceId
Authenticated subject
Operation
ProductId
IdempotencyKey
Execution duration
Result

Sensitive values and credentials must never be logged.

Docker application logs are available through standard container output.

---

23. Database Schema Management

Schema evolution uses EF Core migrations.

A dedicated migration process executes migrations before API startup.

SQL Server
    ↓
Database migration process
    ↓
Identity provider
    ↓
Inventory API

The API does not automatically modify schema during normal startup.

---

24. Configuration

Runtime configuration uses environment variables.

Examples:

ConnectionStrings__InventoryDatabase
Authentication__Authority
Authentication__Audience
Inventory__NegativeStockPolicy
Logging__LogLevel__Default

Secrets must never be committed to source control.

---

25. Docker Environment

Required services:

inventory-api
sql-server
database-migrations
identity-provider

Startup:

docker compose up --build

The complete environment must be evaluable without requiring a locally installed SQL Server.

---

26. Testing Strategy

Testing focuses on critical business behavior.

Unit Tests

Cover:

Inventory business rules
Negative-stock policy
Validation
Soft deletion
Fingerprint generation/comparison
Idempotency behavior
Command handlers
Query filtering behavior where appropriate
Pagination validation
Business-error behavior

Integration Tests

Cover:

Dapper command + EF query interoperability
Database migrations
Transaction commit
Transaction rollback
Atomic concurrent inventory updates
Idempotency unique constraint
Idempotent retry
Idempotency conflict
Soft-delete filtering
Authentication
Authorization policies
API status behavior

---

27. TDD Workflow

Development follows small Red-Green-Refactor cycles.

Specification
    ↓
Contract
    ↓
Failing test
    ↓
Minimal implementation
    ↓
Passing test
    ↓
Refactor

Tests must precede implementation for critical business behavior.

---

28. Initial TDD Scenarios

Product Creation

Given a valid product request and active category:

When the create command executes
Then the product is persisted
And 201 Created is returned

Duplicate SKU

Given an existing SKU:

When another product uses the same SKU
Then the operation is rejected
And 409 Conflict is returned

Product Soft Delete

Given an existing product:

When delete is executed
Then IsDeleted becomes true
And historical inventory remains intact
And normal product queries exclude the product

Successful Outbound Movement

Stock = 10
OUT = 4
Expected stock = 6

Insufficient Stock

Stock = 3
OUT = 5
Negative policy = Reject

Expected:
409 Conflict
Stock remains 3
No movement persisted

Idempotent Retry

Same key + same fingerprint:

Movement executes once only
Stock changes once only
Previous result is returned

Idempotency Conflict

Same key + different fingerprint:

409 Conflict
No additional stock modification

Failed Transaction Retry

Transaction fails before commit:

Rollback occurs
Same key may be retried

Concurrent Outbound Movement

Stock = 5

Request A: OUT 4
Request B: OUT 4

Expected:
At most one succeeds
Stock never becomes negative

Pagination

Given more records than one page:

When page and pageSize are supplied
Then only the requested slice is returned
And pagination metadata is correct

PATCH

Given an existing product:

When only IsActive is provided
Then only IsActive changes
And all other editable values remain unchanged

PUT

Given an existing product:

When a complete replacement representation is provided
Then all editable fields match the supplied representation

---

29. Swagger/OpenAPI

Swagger documents:

- Requests.
- Responses.
- Query parameters.
- Pagination.
- Sorting.
- Authentication.
- Permissions.
- HTTP status codes.
- ProblemDetails errors.
- Idempotency requirements.

Swagger supports OAuth2/Bearer authentication for local evaluation.

---

30. Local Debugging

README instructions include:

- Environment configuration.
- Docker startup.
- Database health.
- Database migrations.
- Running API locally.
- Obtaining OAuth2 token.
- Swagger authentication.
- Running unit tests.
- Running integration tests.
- Viewing Docker logs.
- Correlating TraceId with logs.
- SQL Server troubleshooting.
- Authentication troubleshooting.

---

31. Explicitly Out of Scope

The MVP excludes:

Microservices
Event sourcing
Message brokers
Periodic inventory snapshots
Distributed caching
Rate limiting
IP blocking
Complex role hierarchy
Application-owned password storage
Custom OAuth2 server implementation
Arbitrary dynamic sorting
Arbitrary JSON Patch operations

Features may be introduced later only when justified by concrete requirements.

---

32. Architectural Decisions Summary

Negative stock:
Configurable; Reject by default.

Current stock:
Materialized in InventoryBalances.

Inventory history:
Immutable InventoryMovements.

Concurrency:
Atomic conditional updates.

Idempotency:
Client key + SHA-256 request fingerprint.

Same key / same intention:
Return previous successful result.

Same key / different intention:
409 Conflict.

Failed transaction:
Idempotency key may be retried.

Deletion:
Soft delete.

Authentication:
Local OAuth2/OIDC provider.

Authorization:
Claim-based policies.

Users/passwords:
Owned by identity provider.

Queries:
Entity Framework Core.

Commands:
Dapper.

CQRS:
Explicit handlers without MediatR.

PUT:
Complete replacement of editable state.

PATCH:
Partial update of explicitly supplied fields.

Pagination:
Page-based, default 20, maximum 100.

Errors:
ProblemDetails.

Logging:
Structured ILogger<T> + TraceId.

Schema:
EF Core migrations executed independently.

Testing:
TDD for critical behavior plus integration tests for infrastructure-dependent guarantees.
33. Final MVP Implementation Notes

This specification records the design explored during development. Some capabilities described above were deliberately deferred from the final MVP after implementation review. The final scope was kept aligned with the technical challenge while avoiding unnecessary complexity.

Implemented in the final MVP:

- Product CRUD with soft deletion.
- Category CRUD with soft deletion.
- Inbound and outbound inventory movements.
- Configurable negative-stock policy, with Reject as the default.
- Atomic inventory balance and movement persistence.
- Concurrency-safe conditional stock updates.
- Idempotency using Idempotency-Key plus a deterministic SHA-256 request fingerprint.
- Entity Framework Core for reads.
- Dapper for writes.
- Lightweight CQRS with explicit handlers and no MediatR.
- ProblemDetails-based HTTP errors.
- OAuth2/OIDC authentication using Keycloak.
- Claim-based permission policies.
- Swagger/OpenAPI with authentication support.
- SQL Server, Keycloak, database migrator and API orchestrated through Docker Compose.
- EF Core migrations executed by a dedicated one-shot migrator rather than by the API at runtime.
- Automated unit and integration tests for the critical inventory behavior and persistence guarantees implemented in the MVP.

Deliberately deferred from the final MVP:

- PATCH endpoints for products and categories.
- Public inventory movement history query endpoints.
- A dedicated GET /api/products/{id}/inventory endpoint.
- Client-configurable sorting.
- The broader test matrix proposed in this specification beyond the critical scenarios implemented for the challenge.

Implementation decisions that evolved from the original specification:

- Invalid pagination values are normalized by the controllers instead of returning 400 Bad Request: page is clamped to a minimum of 1 and pageSize to the range 1..100.
- PUT is used for the implemented update operations; typed PATCH contracts were deferred.
- Inventory movement writes were consolidated behind a single transactional IInventoryMovementStore so balance modification, idempotency and movement persistence could share one SQL transaction.
- A missing product is distinguished from insufficient stock and maps to 404 Not Found.
- Database schema migration is a deployment concern handled by Inventory.Migrator; Inventory.Api does not call Database.Migrate() during startup.

These differences are intentional scope and implementation decisions rather than undocumented behavior. The AI-assisted development record in AI_DEVELOPMENT.md explains the reasoning and review process behind the main changes.
