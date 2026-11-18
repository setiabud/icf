# 6. Use React as a same-origin administration frontend

Date: 2026-11-18

## Status

Accepted

## Context

The accounting application needs an interactive administration interface that can share secure authentication with the ASP.NET Core API. A separate frontend origin would complicate cookies, CSRF, CORS, and deployment.

## Decision

The frontend is a React and TypeScript application under `src/ICFAccounting.Web`.

- Vite serves the frontend in local development and proxies `/api` to the HTTPS API.
- Production serves React and the API on one HTTPS origin.
- Protected content is rendered only after session restoration.
- Browser navigation preserves selections and hash-based routes across login and session expiration.
- The frontend consumes API ProblemDetails and displays actionable connection and authentication states.

## Consequences

- Local development requires HTTPS and matching CORS or proxy configuration.
- Production deployment is simplified by eliminating cross-origin cookies.
- Frontend behavior is covered by Vitest and Testing Library tests.
