# 3. Separate accounting and identity persistence

Date: 2026-11-18

## Status

Accepted

## Context

User authentication and accounting aggregates have different lifecycles and schemas. Mixing ASP.NET Identity entities with accounting aggregates in one model would couple unrelated persistence concerns.

## Decision

Authentication and accounting use separate EF Core contexts:

- `ICFAccountingDbContext` stores accounting entities in the `public` schema.
- `AuthenticationDbContext` extends the ASP.NET Identity base context and stores identity data in the `auth` schema.
- Identity migration history is stored in `auth.__IdentityMigrationsHistory`.
- Accounting migration history is stored in `public.__EFMigrationsHistory`.

Both migration sets must be applied explicitly.

## Consequences

- Identity provisioning can evolve independently from accounting data.
- Database operations must account for two contexts and two migration histories.
- Development, deployment, and documentation must clearly distinguish the two migration sets.
