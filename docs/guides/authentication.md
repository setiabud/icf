# Email/password authentication

## Approved scope and account lifecycle

The React app uses ASP.NET Core Identity with SQL Server and HttpOnly cookie
sessions, not mock login. Email/password is the only login method. Google,
Microsoft, consent screens, provider-console setup, client secrets, OAuth callback
URLs, and provider logout URLs do not apply. There is no public registration or
password-reset endpoint.

Identity is an open-source, self-hosted library with no per-user license fee.
Database, hosting, backups, key protection, account administration, and secure
credential delivery remain the operator's responsibility. Email delivery and
self-service recovery are not included.

An administrator first provisions credentials in `auth.AspNetUsers`. After the
first successful password verification, a single application user is created in
`auth.UserProfiles`, including its name, optional image, and creation time.
Subsequent sign-ins reuse that profile. Email is the login identifier; the
display name falls back to email. All signed-in users can perform every existing
accounting operation; there are no roles or per-record restrictions.

Passwords use Identity's salted, versioned PBKDF2-HMAC-SHA512 hashes with 210,000
iterations. New passwords require at least 12 characters, uppercase/lowercase,
a digit, and a symbol. Five failed password attempts lock an account for 15
minutes. Login is also limited to 20 requests per minute per API-visible source
IP, per process. Generic failed-login messages do not disclose registered emails.

## Database setup

Follow [local SQL Server setup](local-sql-server.md) and supply the connection
string securely. Both contexts use `ConnectionStrings__ICFAccounting`.
Apply both migration sets explicitly from the repository root:

```sh
dotnet ef database update --project src/ICFAccounting.Data --startup-project src/ICFAccounting.Data --context ICFAccountingDbContext
dotnet ef database update --project src/ICFAccounting.Data --startup-project src/ICFAccounting.Data --context AuthenticationDbContext
```

Identity has a separate schema and migration history,
`auth.__IdentityMigrationsHistory`; accounting keeps `dbo.__EFMigrationsHistory`.
Startup never creates databases, applies migrations, or seeds users.
The deployment identity needs schema-change permissions; the runtime identity
needs only the data permissions used by the app. Do not use SQL `sa` in production.

## Administrator provisioning and recovery

With the same connection string available to the API, use an interactive terminal:

```sh
dotnet run --project src/ICFAccounting.Api --no-launch-profile -- users create
dotnet run --project src/ICFAccounting.Api --no-launch-profile -- users set-password
```

Creation prompts for email, optional display name and HTTPS image URL, then
password and confirmation. Password entry is hidden; Escape cancels. Passwords
must never be arguments, environment variables, committed files, or logs. Piped
input is rejected. Deliver initial credentials through a secure out-of-band
channel. These commands do not start an HTTP server or migrate the schema.
They require the connection string in the process environment or another
server configuration source. If using local user-secrets instead, prefix the
command with `ASPNETCORE_ENVIRONMENT=Development`; `--no-launch-profile` does not
select Development automatically. Keep production commands in Production.

`set-password` is administrator-operated recovery. It updates the security stamp,
invalidating existing sessions on their next API request. It does not expose a
public password-reset workflow or bypass an existing lockout. Protect database
and command execution access: anyone able to provision users can grant full
accounting access.

## Local HTTPS development

Secure host-only cookies require HTTPS in development as well as production.
Export and trust the local .NET development certificate **on your machine**:

```sh
dotnet dev-certs https --trust
mkdir -p "$HOME/.aspnet/https"
umask 077
dotnet dev-certs https --export-path "$HOME/.aspnet/https/icfaccounting-web.pem" --format Pem --no-password
```

This exports the certificate and its private key to the `.pem` and `.key` paths
used by Vite. Keep both outside the repository, restrict permissions to your
account, and never share or commit the key. Run the API with its HTTPS profile:

```sh
dotnet run --project src/ICFAccounting.Api --launch-profile https
```

Then run `npm run dev` in `src/ICFAccounting.Web` and open
`https://localhost:5173`. Vite proxies `/api/*` to `https://localhost:7206/*`
using the exported certificate for upstream verification; it does not disable
TLS validation. Both processes must use the same local development certificate.
Tests and production builds do not need these certificate files.

The default `VITE_API_BASE_URL=/api` keeps requests on one browser origin. Remove
old overrides pointing to `http://localhost:5101`. Do not send passwords over HTTP.
No browser-exposed environment variable contains a secret.

## Session and HTTP contract

The API paths below are upstream paths; the browser adds the `/api` proxy prefix.
Authentication and accounting responses are marked `Cache-Control: no-store`.

| Method and path | Authentication | Result |
| --- | --- | --- |
| `GET /auth/csrf` | Anonymous allowed | `{ "requestToken": "..." }` and HttpOnly antiforgery cookie |
| `POST /auth/login` | Anonymous allowed; CSRF required | `{ "email": "...", "password": "..." }` creates a session |
| `GET /auth/session` | Required | User profile and absolute `expiresAt`; otherwise `401` |
| `POST /auth/logout` | Required; CSRF required | `204` and cleared browser session cookie |

Successful login/session responses contain `id`, `name`, `email`,
`profileImageUrl` (nullable), and `expiresAt`. No password hash or bearer token is
returned. For every POST/PUT/DELETE, get a current CSRF token with the same cookie
jar and send it as `X-CSRF-TOKEN`. Tokens are bound to the current identity; get
a new one after login/logout. The React HTTP client handles this automatically
and sends cookies with requests. API clients must handle cookies and CSRF too.
Missing or invalid verification tokens return `400 ProblemDetails`.

`__Host-ICFAccounting.Session` is Secure, HttpOnly, SameSite=Strict, host-only,
and nonpersistent. Sessions have an absolute eight-hour deadline with no sliding
renewal; closing the browser can end them earlier, depending on browser session
restoration settings. Security-stamp validation runs on every authenticated
request, so sessions for reset/deleted accounts are rejected without waiting
for cookie expiration. Normal logout clears this browser's cookie; it does not remotely
sign out other browsers or revoke a separately copied cookie.

React restores sessions before rendering protected views and checks again on
window focus. Deadline expiration or an accounting API `401` returns to login
without losing the URL's intended page/selection. Hash routes include
`#/pledgers`, `#/categories`, `#/pledges?kind=category&parentId=...`, and
`#/payments?pledgerId=...&pledgeId=...`. Only known sections are navigable;
external return URLs are never followed. Browser back/forward works.

Loading, network failures, invalid credentials, lockout, rate limits, logout
failures, and expiration are visible. Login can be cancelled, clearing the
password and aborting the browser request. Cancellation cannot guarantee rollback
of a request the server already received. There are no consent-denial states
because no external providers are enabled. Passwords and tokens are never put
in localStorage/sessionStorage.

## Production configuration

Serve the built React files and proxy `/api/*` to the API under **one HTTPS
origin**, for example `https://accounting.example.test`. Strip `/api` before
forwarding; preserve the host, cookie headers and `Set-Cookie` responses.
Do not cache API responses or fall back to the SPA for `/api` failures. Use a
verified HTTPS upstream so API HTTPS redirection and Secure cookies work without
trusting arbitrary forwarded headers. HTTP requests to the public site must
redirect to HTTPS at the edge before accepting credentials.

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings__ICFAccounting` | Required server-side SQL connection string; use a deployment secret store |
| `VITE_API_BASE_URL` | Build-time public API prefix; normally `/api` |
| `ASPNETCORE_ENVIRONMENT` | `Production` outside local development |
| `DataProtection__KeyRingPath` | Optional persistent/shared server-side key-ring directory |

Persist Data Protection keys across restarts and share the key ring across API
instances. The application name is fixed to `ICFAccounting`. If setting
`DataProtection__KeyRingPath`, use a restricted, encrypted volume: file persistence
does not itself encrypt keys at rest. Back up/protect keys along with database
backups. Without stable keys, deployment restarts can invalidate sessions.

Production does not enable CORS; use the same-origin proxy. A restrictive
Development-only CORS policy remains for explicitly configured HTTPS development
origins and allows credentials plus the CSRF header.

The limiter uses the actual peer IP and does not trust incoming forwarded-IP
headers. Behind a reverse proxy it can therefore group requests by proxy IP.
Configure suitable edge rate limiting for the deployment; multiple instances
do not share the in-process rate counter. Never enable request-body logging,
EF sensitive-data logging, or proxy logging of cookies/passwords/tokens.

## Verification and rollout

```sh
dotnet build ICFAccounting.slnx
dotnet test ICFAccounting.slnx
cd src/ICFAccounting.Web
npm test
npm run lint
npm run build
```

SQL Testcontainers tests use real Identity password verification, profile
creation/reuse, cookies, CSRF, logout, expiry, and password-reset invalidation.
Existing accounting endpoint tests use an explicit test-only identity, not a
production bypass. React tests use test doubles to exercise UI lifecycle and
navigation. No external provider credentials are needed.

Before rollout, apply both migration sets, provision accounts, configure HTTPS,
proxying and persistent keys, and verify login/logout in the deployed browser.
Automated tests do not certify a particular production proxy, key-volume setup,
or administrator credential-delivery process.
