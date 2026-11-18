# 2. Use PostgreSQL and Entity Framework Core with explicit migrations

Date: 2026-11-18

## Status

Accepted

## Context

The application needs reliable relational persistence, decimal monetary values, and database constraints for relationships between pledgers, categories, pledges, and payments. Development and test environments need predictable PostgreSQL behavior.

## Decision

The solution uses PostgreSQL through Entity Framework Core and the Npgsql provider.

- `ICFAccountingDbContext` owns the public accounting schema.
- EF Core configurations map domain aggregate identities and relationships.
- Monetary amounts use PostgreSQL `numeric` columns.
- Timestamps are normalized to PostgreSQL microsecond precision where needed.
- EF migrations are applied explicitly; application startup does not create or update the schema.
- The connection string is supplied through configuration or environment variables and is required for data services.

Database migration history for accounting is stored in `public.__EFMigrationsHistory`.

## Consequences

- Schema changes are reviewable and deployable through migrations.
- The database can enforce relationship integrity.
- Local and deployed environments must apply migrations deliberately.
- Tests can use PostgreSQL test containers and require Docker for the full integration suite.
