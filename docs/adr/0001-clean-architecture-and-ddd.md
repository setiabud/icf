# 1. Use Clean Architecture and domain-driven design

Date: 2026-11-18

## Status

Accepted

## Context

ICF Accounting manages related accounting records with invariants and relationships that should not depend on HTTP or database details. The solution also needs clear boundaries so domain behavior can be tested independently of infrastructure.

## Decision

The solution is organized into Domain, Application, Data, and API projects:

- `ICFAccounting.Domain` contains aggregates, strongly typed IDs, repository interfaces, and domain rules.
- `ICFAccounting.Application` contains use-case services and orchestrates domain operations.
- `ICFAccounting.Data` implements repository interfaces and owns EF Core mappings and migrations.
- `ICFAccounting.Api` owns HTTP transport concerns, request and response models, and authentication.
- `ICFAccounting.Web` is an independent React/TypeScript administrative frontend.

Domain entities use private construction and factory methods or update methods that enforce invariants. Repository interfaces define typed persistence operations for each aggregate, alongside feature-specific queries.

## Consequences

- Domain and application behavior can be unit tested without HTTP or PostgreSQL.
- Persistence and transport details do not leak into the domain model.
- API endpoints remain thin adapters over application services.
- Changes to external technologies require only the relevant boundary to change.
