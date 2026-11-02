# ICFAccounting.Web

React and TypeScript administration frontend for the ICF Accounting API. The
application manages pledgers, pledge categories, pledges, and pledge payments
using the API in `../ICFAccounting.Api`.

## Requirements

- Node.js and npm
- The .NET 10 SDK
- SQL Server for the API, configured as described in
  [Local SQL Server](../../docs/guides/local-sql-server.md)

## Configure and run

1. Start SQL Server, apply the API database migrations, trust the local HTTPS
   development certificate, and run `ICFAccounting.Api` with its `https`
   launch profile. The API listens on `https://localhost:7206` and
   `http://localhost:5101`. Follow the
   [authentication guide](../../docs/guides/authentication.md) to apply the
   Identity migration, export the Vite HTTPS certificate, and provision a user.
2. In this directory, install the frontend dependencies:

   ```sh
   npm install
   ```

3. Optionally copy `.env.example` to `.env.local` and set
   `VITE_API_BASE_URL` if necessary. The default `/api` uses Vite's HTTPS proxy
   to `https://localhost:7206`; remove old HTTP overrides. Secure authentication
   cookies require HTTPS.
4. Start Vite:

   ```sh
   npm run dev
   ```

   Open `https://localhost:5173` and sign in with administrator-provisioned
   credentials. Production likewise serves React and `/api` on one HTTPS origin.
   There is no public registration.

The UI displays an actionable connection error when the API is unavailable.
It does not use sample data. Pledges are browsed through a selected pledger or
category, and payments through a selected pledge.
Sessions are restored before protected content is rendered; URL hash navigation
preserves the intended section/selection through login and expiration.

## Checks

```sh
npm test
npm run lint
npm run build
```
