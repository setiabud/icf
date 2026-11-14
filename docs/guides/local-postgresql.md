# Local PostgreSQL

PostgreSQL 17 runs in Docker Compose; the .NET API and React frontend run
locally. PostgreSQL's native ARM64 image supports Apple Silicon. The first
startup creates an empty `icfaccounting` database; EF migrations create its
schema. This setup does not copy data from the former SQL Server database.

## Prerequisites and startup

- Install the .NET 10 SDK and Docker Desktop with Docker Compose.
- For the frontend, install Node.js and npm.
- For HTTPS development, run `dotnet dev-certs https --trust` and approve the
  operating-system prompt.

Run commands from the repository root. Restore the solution packages and EF
tool, then start PostgreSQL with a strong local password:

```zsh
dotnet restore ICFAccounting.slnx
dotnet tool restore

read -rs 'POSTGRES_PASSWORD?PostgreSQL development password: '
export POSTGRES_PASSWORD
docker compose -f compose.json up -d --wait postgres
```

Compose binds port `5432` to `127.0.0.1`; set `POSTGRES_PORT` before starting
if that port is in use. The named `postgres-data` volume persists the database
across container restarts. Do not commit the password.

## Create the database schema

The Compose service creates the empty database `icfaccounting`. Apply the
accounting and Identity migrations explicitly; application startup does not
create a database, migrate a schema, or seed users.

Enter the connection string without placing it in shell history:

```zsh
read -rs 'ConnectionStrings__ICFAccounting?PostgreSQL connection string: '
export ConnectionStrings__ICFAccounting
dotnet ef database update --project src/ICFAccounting.Data --startup-project src/ICFAccounting.Data --context ICFAccountingDbContext
dotnet ef database update --project src/ICFAccounting.Data --startup-project src/ICFAccounting.Data --context AuthenticationDbContext
```

Use the configured port if `POSTGRES_PORT` is not `5432`. The connection
string format is:

```text
Host=localhost;Port=5432;Database=icfaccounting;Username=postgres;Password=<your local password>
```

The Data design-time factories read only `ConnectionStrings__ICFAccounting`
from the process environment; they do not read Compose variables or API
user-secrets. Accounting tables and migration history are in `public`.
Identity tables and its separate `__IdentityMigrationsHistory` are in `auth`.
Remove the environment values when finished:

```zsh
unset ConnectionStrings__ICFAccounting
unset POSTGRES_PASSWORD
```

## Run the API and frontend

With the connection string configured and both migration sets applied, run the
API:

```zsh
dotnet run --project src/ICFAccounting.Api --launch-profile https
```

The API uses `https://localhost:7206`. Alternatively, store the connection
string in local user-secrets:

```zsh
read -rs 'ConnectionStrings__ICFAccounting?PostgreSQL connection string: '
export ConnectionStrings__ICFAccounting
dotnet user-secrets set "ConnectionStrings:ICFAccounting" "$ConnectionStrings__ICFAccounting" --project src/ICFAccounting.Api
unset ConnectionStrings__ICFAccounting
```

User-secrets are outside the repository but are not encrypted; use them only
for local development. The standalone migration factories still require the
environment variable.

Follow the [authentication guide](authentication.md) to provision an
administrator account. To run the frontend, install its dependencies in
`src/ICFAccounting.Web` with `npm install`, then run `npm run dev`.

## Database and behavior notes

- IDs use PostgreSQL `uuid`; amounts use unconstrained `numeric` to preserve
  the full .NET `decimal` range. Domain validation limits amounts to two
  decimal places.
- Names use the ICU collation
  `icf_case_insensitive_accent_sensitive` for case-insensitive,
  accent-sensitive sorting.
- Audit timestamps are stored as `timestamp without time zone` and restored as
  UTC `DateTime` values. Payment dates preserve their wall-clock value and are
  also stored without a time zone. PostgreSQL timestamps have microsecond
  precision.
- Foreign keys reject deletion of referenced records; the API translates
  recognized relationship conflicts into sanitized `409 Conflict` responses.
- Equal sort values use PostgreSQL's native UUID ordering as the stable
  tie-breaker. This differs from SQL Server's native GUID ordering.

## Tests

Domain and Application unit tests do not need Docker:

```zsh
dotnet test tests/ICFAccounting.UnitTests/ICFAccounting.UnitTests.csproj
```

Integration tests start disposable PostgreSQL 17 containers, apply the
migrations, and use isolated databases. They require Docker and do not use the
Compose database:

```zsh
dotnet test tests/ICFAccounting.IntegrationTests/ICFAccounting.IntegrationTests.csproj
```

The complete suite is:

```zsh
dotnet test ICFAccounting.slnx
```

Tests fail when Docker is unavailable; they do not silently skip database
checks or substitute an in-memory provider.

## Inspect, stop, and reset

Use these commands to check the service and open a PostgreSQL shell:

```zsh
docker compose -f compose.json ps
docker compose -f compose.json logs postgres
docker compose -f compose.json exec postgres psql -U postgres -d icfaccounting
docker compose -f compose.json stop postgres
```

Stopping the container or running `docker compose -f compose.json down`
preserves the `postgres-data` volume. The following command permanently
deletes that volume and all local database data; only use it when you
intentionally want a fresh database:

```zsh
docker compose -f compose.json down --volumes
```
