# 5. Use secure cookie authentication with CSRF protection

Date: 2026-11-18

## Status

Accepted

## Context

The React frontend requires authenticated access to accounting administration. Registration is intentionally not public, and the application must prevent cross-site request forgery and insecure session cookies.

## Decision

The API uses ASP.NET Core Identity cookies for authentication:

- Public registration is disabled; accounts are created or updated through administrator-provisioned commands.
- Passwords require at least 12 characters, 210,000 password-hasher iterations, and lockout after five failed attempts.
- Sessions are eight hours long, are not sliding, and have a `SameSite=Strict` host cookie.
- Session and antiforgery cookies are HTTP-only and secure.
- Authentication and authorization failures use `401` and `403` ProblemDetails responses.
- Login attempts are rate limited.
- Mutating HTTP requests require a valid antiforgery token.
- Data Protection keys can be persisted to a configured key-ring path.

## Consequences

- The frontend must participate in the same HTTPS origin and include the antiforgery flow for mutations.
- Administrator account provisioning is an operational requirement.
- Session handling is more secure but requires HTTPS and correct reverse-proxy or origin configuration.
