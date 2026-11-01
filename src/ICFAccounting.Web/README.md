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
   `http://localhost:5101`.
2. In this directory, install the frontend dependencies:

   ```sh
   npm install
   ```

3. Optionally copy `.env.example` to `.env.local` and set
   `VITE_API_BASE_URL` to the API's base URL. The default is
   `https://localhost:7206`; use `http://localhost:5101` if you prefer HTTP.
   The HTTPS development certificate must be trusted by the browser.
4. Start Vite:

   ```sh
   npm run dev
   ```

   Open `http://localhost:5173`. The API allows this origin in the ASP.NET
   Core Development environment only. Additional local development origins can
   be set in the API's `Cors:DevelopmentOrigins` configuration; production
   builds do not enable CORS.

The UI displays an actionable connection error when the API is unavailable.
It does not use sample data. Pledges are browsed through a selected pledger or
category, and payments through a selected pledge.

## Checks

```sh
npm test
npm run lint
npm run build
```
