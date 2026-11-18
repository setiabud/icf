# VS Code Dev Container

The development container runs .NET 10, Node.js 24, the API, frontend, and
tests. A companion PostgreSQL 17 service holds the development database.
Both images support native Apple Silicon. Docker Desktop and VS Code remain
on the Mac; you do not need host Node.js or .NET for daily development.
The initial HTTPS certificate export uses the host .NET SDK.

## Host setup

1. Install and start Docker Desktop. Install VS Code and its **Dev Containers**
   extension. Docker Compose must support the `!reset` tag (version 2.24.4 or
   later).
2. Create an ignored `.env` at the repository root:

   ```sh
   cp .devcontainer/.env.example .env
   chmod 600 .env
   openssl rand -hex 32
   ```

   Paste the generated value after `POSTGRES_PASSWORD=` in `.env`. Keep it
   local; never commit or share it. Use a hexadecimal password so it can be
   embedded safely in the database connection string. Changing this value
   does not change the password in an already initialized PostgreSQL volume.
3. With the host .NET SDK, trust and export its development certificate:

   ```sh
   dotnet dev-certs https --trust
   mkdir -p "$HOME/.aspnet/https"
   umask 077
   dotnet dev-certs https --export-path "$HOME/.aspnet/https/icfaccounting-web.pem" --format Pem --no-password
   ```

   Approve macOS's trust prompt. The certificate and private key stay outside
   the repository and are mounted read-only into the container. The API and
   Vite use these same files, preserving upstream TLS verification and browser
   trust. Do not generate a separate certificate inside the container.
4. Open the repository in VS Code and choose **Dev Containers: Reopen in
   Container**. The first build installs tools and restores .NET, EF, and
   frontend dependencies. PostgreSQL must pass its health check before the
   development service starts.

The default Compose project is separate from the standalone local setup, with
its own database volume. Existing host-development data is not copied.
PostgreSQL is reachable as `postgres:5432` inside the Compose network; it has
no published host port in this setup.

## Explicit database setup

Run these commands in the VS Code container terminal, at `/workspaces/icf`:

```sh
dotnet ef database update --project src/ICFAccounting.Data --startup-project src/ICFAccounting.Data --context ICFAccountingDbContext
dotnet ef database update --project src/ICFAccounting.Data --startup-project src/ICFAccounting.Data --context AuthenticationDbContext
dotnet run --project src/ICFAccounting.Api --no-launch-profile -- users create
```

The connection string is already supplied to the container environment.
Migrations and account provisioning are never run automatically; no sample
data is inserted. Follow the [authentication guide](authentication.md) for
account lifecycle and recovery.

## Run and debug

In one container terminal:

```sh
dotnet watch --project src/ICFAccounting.Api --launch-profile https
```

In another:

```sh
cd src/ICFAccounting.Web
npm run dev
```

Open `https://localhost:5173` in the Mac browser. VS Code forwards frontend
port `5173` and API port `7206` with HTTPS. Keep these host ports free.
Vite's `/api` proxy reaches the API on `https://localhost:7206` inside the
development container. Secure session cookies continue to work on one origin.
Use C# Dev Kit inside VS Code for API debugging.

The workspace is bind-mounted, so source edits persist on the host. Linux
.NET build outputs and intermediate files use a separate named volume via
`.devcontainer/dotnet.props`; host `bin` and `obj` files are excluded.
NuGet packages and Linux `node_modules` also have named volumes, keeping
platform-specific dependencies out of host builds.
PostgreSQL timestamps are normalized to microsecond precision before saving,
including when the Linux clock supplies finer-grained audit timestamps.

## Verification

In the container terminal:

```sh
dotnet build ICFAccounting.slnx
dotnet test ICFAccounting.slnx
npm --prefix src/ICFAccounting.Web test
npm --prefix src/ICFAccounting.Web run lint
npm --prefix src/ICFAccounting.Web run build
```

Integration tests still create disposable PostgreSQL containers and isolated
databases, not the companion development database. The Docker-outside-of-Docker
feature connects the development container to Docker Desktop.
`TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal` routes published test ports
back to Docker Desktop; the socket override lets the cleanup container use the
host socket. Do not disable Testcontainers cleanup.

**Docker socket access grants control of your Docker Desktop engine.** Only
open trusted repository code in this container. This setup does not start a
nested Docker daemon and does not isolate untrusted code from other Docker
Desktop resources.

## Persistence and troubleshooting

- Closing the remote session stops its Compose services. Reopening or
  rebuilding preserves the database, NuGet cache, build outputs, and frontend
  dependencies. The host certificate files are unaffected.
- To inspect the development database, run `docker ps` in the container, then
  `docker exec -it <postgres-container-name> psql -U postgres -d icfaccounting`.
  Choose the companion service, not a disposable test container.
- If Testcontainers cannot connect, check `docker info` inside the container
  and that Docker Desktop is running. The `host.docker.internal` setting is
  intended for Docker Desktop; native Linux Docker hosts need a reachable host
  gateway configured separately.
- If HTTPS fails, re-export a valid, host-trusted certificate and restart the
  API and Vite. Do not bypass certificate verification.
- If dependencies are stale, run `dotnet restore`, `dotnet tool restore`, and
  `npm ci --prefix src/ICFAccounting.Web` inside the container.
- To reset data intentionally, stop the devcontainer and remove only its
  PostgreSQL volume using Docker Desktop. This permanently deletes that
  database; do not remove standalone or test resources. Reopen the container
  and explicitly apply both migration sets again.
