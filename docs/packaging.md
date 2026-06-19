# Packaging and release shape

Reva runs as a web frontend plus an ASP.NET Core API. In development they run as two processes. In release packaging, the frontend is exported and served by the API host.

## Development mode

Run the backend API:

```powershell
dotnet run --project src/Reva.Web/Reva.Web.csproj -- --no-open
```

Run the frontend:

```powershell
cd web
$env:NEXT_PUBLIC_API_BASE_URL = "http://localhost:5158"
pnpm dev
```

Open `http://localhost:3000`.

This mode is best for UI work because Next.js gives fast refresh while the API keeps real document state on `http://localhost:5158`.

## Packaged mode

The Windows package is built as one API-hosted application:

1. Restore and build the .NET solution.
2. Install web dependencies.
3. Build the Next.js static export from `web/`.
4. Publish `src/Reva.Web` for Windows.
5. Copy `web/out` into the published API `wwwroot`.
6. Run a smoke test against the packaged executable.

The result is a Windows executable that serves:

- the static Reva web app
- document API routes
- health checks
- export routes
- settings routes
- assistant/agent routes

## Runtime data

| Data | Purpose |
|:---|:---|
| SQLite database | Documents, fields, citations, exceptions, settings, mappings, Knowledge Hub records, and export records. |
| File storage | Original uploads and generated derivatives. |
| Export storage | CSV, Excel, and JSON outputs. |
| Local settings | Optional provider configuration and UI defaults. |

Secrets must come from local configuration or environment variables. They should never be committed.

## Validation checklist

Before a handoff or release:

```powershell
dotnet build Reva.slnx -warnaserror
dotnet test
```

For the packaged executable, verify:

- `/health` returns OK
- `/api/documents` returns document data
- `/` serves the frontend
- default SQLite data storage exists
- no browser opens automatically during package smoke tests

For UI-facing work, also verify the changed screen in a real browser.
