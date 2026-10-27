# ICFAccounting

An accounting API for pledgers, pledge categories, pledges, and payments.
Built with .NET 10, ASP.NET Core Minimal APIs, EF Core, and SQL Server.

## Local Setup

SQL Server runs in Docker Compose; the API runs on the local .NET SDK.
The current container setup targets an Intel Mac with Docker Desktop.

1. Install the .NET 10 SDK and start Docker Desktop.
2. From the repository root, run `dotnet restore ICFAccounting.slnx` and
   `dotnet tool restore`.
3. Follow the [local SQL Server guide](docs/guides/local-sql-server.md) to start
   SQL Server, supply credentials, apply migrations to `ICFAccounting`, and
   run the API.

The HTTPS launch profile serves the API at `https://localhost:7206`.
Credentials are supplied through environment variables or local user-secrets,
never committed configuration. Database migrations are explicit; startup does
not create or update the schema. No sample data is inserted.

## Verification

```zsh
dotnet build ICFAccounting.slnx
dotnet test ICFAccounting.slnx
```

The full suite requires Docker for SQL Server integration tests. Unit tests
can run without Docker:

```zsh
dotnet test tests/ICFAccounting.UnitTests/ICFAccounting.UnitTests.csproj
```

## Documentation

- [Local SQL Server, credentials, migrations, tests, and troubleshooting](docs/guides/local-sql-server.md)
- [Pledgers API](docs/api/pledgers.md)
- [Pledge Categories API](docs/api/pledge-categories.md)
- [Pledges API](docs/api/pledges.md)
- [Pledge Payments API](docs/api/pledge-payments.md)

## Solution Layout

- `src/ICFAccounting.Domain`: aggregates, invariants, and repository interfaces.
- `src/ICFAccounting.Application`: application services and orchestration.
- `src/ICFAccounting.Data`: SQL repositories, EF mappings, and migrations.
- `src/ICFAccounting.Api`: HTTP endpoints and response contracts.
- `tests/ICFAccounting.UnitTests`: Domain and Application tests.
- `tests/ICFAccounting.IntegrationTests`: SQL persistence and API tests.