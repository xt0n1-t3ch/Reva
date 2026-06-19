# Packaging and run shape

Reva packages as a web frontend plus an ASP.NET Core API. In production, the frontend is exported statically and served from the API host.

## Development

Run the API:

```powershell
dotnet run --project src/Reva.Web/Reva.Web.csproj -- --no-open
```

Run the web app:

```powershell
cd web
$env:NEXT_PUBLIC_API_BASE_URL = "http://localhost:5158"
pnpm install
pnpm dev
```

Open `http://localhost:3000`.

## Production shape

1. Build the frontend static export from `web/`.
2. Copy the exported assets into the API host's `wwwroot`.
3. Publish `src/Reva.Web` with the normal .NET publish flow.
4. Run the API host; it serves both HTTP endpoints and the static app.

## Runtime files

| File or folder | Purpose |
|:---|:---|
| SQLite database | Documents, fields, source spans, settings, learned mappings, Knowledge Hub records. |
| Upload storage | Original files and generated derivatives. |
| Export folder | CSV, Excel, and JSON outputs selected by the user. |
| Provider settings | Optional model endpoint and active model metadata. |

## Validation before handing off

```powershell
dotnet build Reva.slnx -warnaserror
dotnet test
```

For UI changes, also run the API and web dev server, then verify the browser flow that changed.
