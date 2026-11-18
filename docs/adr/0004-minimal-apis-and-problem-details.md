# 4. Use ASP.NET Core Minimal APIs and ProblemDetails

Date: 2026-11-18

## Status

Accepted

## Context

The API provides CRUD-style administration operations and paginated queries. It needs consistent routing, validation responses, OpenAPI metadata, and a clear separation from domain behavior.

## Decision

The HTTP API uses ASP.NET Core Minimal APIs.

- Endpoint extension methods group routes by feature.
- Request types belong in the API project.
- Application services perform orchestration.
- Domain entities enforce business invariants.
- Invalid requests and domain validation failures produce RFC 7807 `ProblemDetails`.
- Unexpected server failures are logged and returned as safe 500 responses.
- Relationship conflicts are translated into conflict responses.

## Consequences

- The API remains lightweight and does not require MVC controllers.
- HTTP response behavior is consistent across features.
- Transport mapping stays out of the domain.
- Endpoint changes should include API documentation and integration coverage.
