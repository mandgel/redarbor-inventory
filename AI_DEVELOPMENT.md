# AI-Assisted Development Evidence

## 1. Purpose

This document records how AI was used during development of the Inventory API, the main prompts and specifications that guided the work, and the technical decisions that were accepted, modified, or rejected after human review.

AI was used as an implementation accelerator and review assistant. Architectural choices, scope decisions, execution of the application, inspection of generated code, smoke testing, and corrections remained part of the development process rather than being delegated without verification.

## 2. AI Tools Used

AI assistance was used for:

- Refining the technical specification before implementation.
- Breaking the work into small implementation blocks.
- Generating repetitive project/application/infrastructure/API code from explicit requirements.
- Proposing tests and implementation changes.
- Reviewing build and runtime failures.
- Identifying ambiguous business behavior.
- Generating Docker, Keycloak and deployment configuration from constrained prompts.
- Summarizing each implementation block for manual review.

Generated changes were reviewed and the application was compiled, tested, and exercised manually before continuing to the next block.

## 3. Spec-Driven Development

Development started from `SPEC.md`, which defined the intended domain model, API behavior, persistence strategy, idempotency semantics, concurrency requirements, authentication/authorization model, Docker environment, testing strategy, and acceptance scenarios before the corresponding implementation blocks were generated.

The specification was intentionally broader than the final MVP. During implementation, capabilities that were not required to demonstrate the core challenge were deferred rather than added only because they had appeared in the initial design.

The general workflow was:

```text
Specification
    -> contract / business rule
    -> test or verifiable acceptance scenario
    -> implementation
    -> build and test
    -> manual review / smoke test
    -> correction or simplification when required
```

For critical inventory behavior, the process followed small Red-Green-Refactor-style iterations. Infrastructure-dependent guarantees were then verified through integration tests against SQL Server.

## 4. Main Prompt Areas

The full conversation contained iterative prompts rather than one single code-generation request. The principal prompt areas are summarized below.

### 4.1 Inventory movement domain and tests

The AI was asked to implement the inventory movement use case from an explicit contract covering:

- inbound and outbound movements;
- positive quantity validation;
- configurable negative-stock behavior;
- idempotency;
- deterministic request fingerprinting;
- replay of an already successful identical request;
- conflict when the same idempotency key represents a different intention;
- test-first coverage of the critical behavior.

The initial implementation used separate balance and idempotency stores. That design was later changed after reviewing the transactional requirements.

### 4.2 Transactional Dapper inventory store

The persistence prompt constrained the implementation to Dapper and SQL Server and required balance modification, movement persistence, and idempotency to participate in one transaction.

The resulting design uses `IInventoryMovementStore` and `DapperInventoryMovementStore`. An atomic conditional UPDATE prevents the classic read-check-write race when negative stock is rejected.

### 4.3 Categories CRUD

The AI was asked to implement Categories using the established lightweight CQRS structure:

- EF Core for queries;
- Dapper for commands;
- soft delete;
- page-based pagination;
- explicit handlers and stores;
- no MediatR, AutoMapper, or generic repository.

The generated endpoints were subsequently exercised manually through Swagger.

### 4.4 Products CRUD

The Products prompt followed the same CQRS/persistence split and additionally required:

- unique SKU handling;
- validation that the category exists, is active, and is not deleted;
- creation of `InventoryBalance` with zero stock atomically with the product;
- server-side EF projection including category name and current stock;
- filtering and pagination;
- soft deletion without changing inventory history.

### 4.5 Inventory movement HTTP endpoint

The AI was asked to expose the existing movement use case through HTTP without duplicating business rules in the controller. `Idempotency-Key` is required and business exceptions are mapped to appropriate HTTP semantics.

A later review identified that a nonexistent product and insufficient stock could produce the same store result. The implementation was corrected to distinguish `ProductNotFound` from `InsufficientStock`.

### 4.6 ProblemDetails

The API initially returned a mixture of plain strings and framework responses. A later prompt centralized exception handling with ASP.NET Core `ProblemDetails` and `IExceptionHandler` while preserving the existing HTTP status semantics.

Expected business failures are structured and unexpected failures return a generic 500 response without exposing stack traces to the client.

### 4.7 OAuth2/OIDC and permissions

The authentication prompt required:

- local Keycloak;
- OAuth2/OIDC access tokens;
- JWT Bearer validation in ASP.NET Core;
- permission claims rather than controller-level role coupling;
- policies for product, category and inventory operations;
- reader and administrator test identities;
- Swagger authentication;
- 401 for missing/invalid authentication and 403 for insufficient permission.

The completed implementation was smoke-tested with both test identities.

### 4.8 Docker and deployment

The final infrastructure prompt required one command:

```bash
docker compose up --build
```

to bring up SQL Server, a one-shot migration process, Keycloak, and the API.

The prompt explicitly prohibited running EF migrations from API startup. A dedicated `Inventory.Migrator` owns schema migration during deployment.

The Keycloak issuer also had to remain identical when accessed by the host and by the containerized API. The final setup uses `keycloak.localhost` as the shared hostname/network alias and documents the Windows hosts-file prerequisite.

## 5. Architecture Resulting from the Collaboration

The final solution uses a lightweight layered/CQRS architecture:

```text
Inventory.Api
    HTTP contracts, authentication, authorization, ProblemDetails
        |
Inventory.Application
    commands, queries, handlers, DTOs, use-case abstractions
        |
Inventory.Domain
    domain entities and inventory policy concepts
        |
Inventory.Infrastructure
    EF Core reads, Dapper writes, SQL Server persistence
```

Runtime infrastructure is:

```text
SQL Server -> Inventory.Migrator -> Inventory.Api
                  Keycloak -------> Inventory.Api
```

Key architectural characteristics are:

- EF Core for query/read paths.
- Dapper for command/write paths.
- Explicit command/query handlers without MediatR.
- Soft deletion for products and categories.
- Materialized current balance plus immutable movement history.
- Atomic SQL updates for concurrent stock safety.
- Idempotency key plus SHA-256 request fingerprint.
- OAuth2/OIDC identity owned by Keycloak.
- Claim-based ASP.NET Core authorization policies.
- Deployment-time EF Core migrations.

## 6. AI Proposals Accepted

Several proposals were retained because they directly supported the requirements without adding unnecessary abstraction:

- Lightweight CQRS with dedicated handlers.
- EF Core `AsNoTracking()` query stores and server-side projection.
- Dapper command stores.
- `ProblemDetails` for consistent API errors.
- Keycloak as a reproducible local OAuth2/OIDC provider.
- Permission claims and authorization policies.
- A dedicated one-shot migration process.
- Request fingerprinting in addition to the idempotency key.
- Conditional SQL stock updates to protect against concurrent outbound operations.

## 7. AI Proposals or Initial Designs Modified

### 7.1 Separate stores -> transactional movement store

An early design separated balance persistence and idempotency persistence. Although individually reasonable, this made the transaction boundary unclear. It was replaced with `IInventoryMovementStore`, allowing stock update, movement insert and idempotency behavior to execute through the same connection and transaction.

The change was made to preserve the specification's atomicity guarantee rather than preserving an abstraction that no longer fit the use case.

### 7.2 Insufficient stock -> ProductNotFound distinction

The conditional balance UPDATE returns no row both when stock is insufficient and when no balance exists. Initially these cases were indistinguishable and a nonexistent product could become a 409 stock conflict.

The store was changed to resolve a failed balance update by checking whether the non-deleted product exists inside the same transaction. A missing product now produces `ProductNotFound` and maps to 404; an existing product whose update failed remains `InsufficientStock` and maps to 409.

### 7.3 CreatedAtAction routing

Manual Swagger testing exposed a runtime `No route matches the supplied values` error after category creation. The database operation succeeded, but response formatting failed while generating the Location URI.

The route was corrected using a named GET route and `CreatedAtRoute`. This was an example where successful build/tests were insufficient and an HTTP smoke test found the defect.

### 7.4 Error responses -> global ProblemDetails

Controllers originally returned mixed plain-text `BadRequest`, `Conflict`, and `NotFound` responses and contained local exception mapping. This was evolved to a global exception handler and structured ProblemDetails responses so the HTTP error contract became consistent without moving HTTP concerns into Application.

### 7.5 Keycloak localhost -> shared issuer hostname

OAuth initially worked while the API ran on the host, but `localhost:8080` cannot refer to the Keycloak container from a containerized API. Rather than disabling issuer validation or introducing separate issuer identities, the deployment was changed to use `keycloak.localhost` as both the external Keycloak hostname and Docker network alias.

This keeps discovery metadata, token issuer, and API Authority consistent.

### 7.6 Pagination invalid-input behavior

The original specification proposed `400 Bad Request` for invalid pagination values. The implemented controllers normalize `page` and `pageSize` to safe bounds instead. This was retained as a small usability decision for the MVP and is explicitly documented as a specification evolution.

## 8. Proposals Deliberately Rejected or Deferred

### MediatR

CQRS did not require a mediator library for a solution of this size. Explicit handlers make dependencies and execution flow easy to inspect, so MediatR was excluded.

### Generic repository

EF Core and Dapper have different responsibilities in this solution. A generic repository would hide useful persistence semantics and add abstraction without solving a concrete problem.

### AutoMapper

The number and complexity of mappings did not justify another dependency. Explicit mapping keeps the API/application contracts visible.

### API-driven migrations

Calling `Database.Migrate()` during API startup was rejected because schema evolution is a deployment responsibility. `Inventory.Migrator` performs this operation before the API starts.

### PATCH endpoints

Typed partial updates were specified but deferred. PUT satisfies the CRUD requirement and implementing PATCH was not necessary to demonstrate the requested architecture or business behavior.

### Movement-history and dedicated balance query endpoints

These were useful extensions in the broader specification but were deferred from the final MVP to prioritize the required CRUD, inbound/outbound movement behavior, persistence split, security, Docker reproducibility, and tests.

### Configurable sorting

The specification explored whitelisted dynamic sorting, but it was not required for the core challenge and was deferred rather than adding query-contract complexity late in the implementation.

## 9. Human Review and Verification

AI output was not treated as complete merely because it compiled. Verification included:

- inspecting generated files and dependency direction;
- running the full solution build repeatedly;
- running unit tests after application/domain changes;
- running integration tests against the Dockerized SQL Server after persistence changes;
- inspecting generated EF migrations before applying them;
- exercising CRUD endpoints through Swagger;
- manually verifying idempotency behavior;
- verifying 401/403/authorized OAuth scenarios;
- validating the Keycloak issuer/discovery document;
- starting the complete stack through Docker Compose;
- verifying the one-shot migrator completes before API startup.

At the final infrastructure checkpoint, 7 unit tests and 4 integration tests passed (11 total), and the Dockerized OAuth smoke tests produced the expected 401, 403, 200 and 201 behaviors.

## 10. Examples Where AI Output Required Correction

The development process produced several useful examples of why generated code still required engineering review:

1. A category POST successfully persisted data but returned 500 because the generated `CreatedAtAction` could not resolve the target route.
2. The first inventory persistence abstraction split state that needed one SQL transaction and was refactored into one movement store.
3. A failed conditional stock UPDATE initially conflated nonexistent products with insufficient stock.
4. OAuth worked with an API running on the host but required a shared issuer hostname once the API itself was containerized.
5. A SQL Server integration-test failure was traced to a stale environment-variable password rather than application logic.

These corrections were made based on runtime behavior, database semantics, and the specification rather than accepting generated output unchanged.

## 11. Final Note

AI materially accelerated repetitive implementation and provided useful alternative designs, but the final solution reflects iterative human review. Generated proposals were accepted only when they fit the specification and project constraints; otherwise they were simplified, corrected, or deferred.
