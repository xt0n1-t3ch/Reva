# Packaging

Reva 1.3.0 packages the product as one self-contained Windows executable. The package contains no Node.js runtime and does not require a separate web server.

## Build the Windows package

```powershell
./scripts/package-windows.ps1 -Version 1.3.0
```

The script performs the release build in this order:

1. `pnpm install --frozen-lockfile` in `web/`.
2. `pnpm run build` for the Next.js static export (`output: 'export'`).
3. Clear and stage `web/out/*` into `src/Reva.Web/wwwroot`.
4. `dotnet publish src/Reva.Web/Reva.Web.csproj` for `win-x64`, self-contained, single file.
5. Add the launcher and package readme.
6. Zip the release artifacts.

Expected output:

```text
artifacts/releases/Reva-v1.3.0-win-x64.zip
```

## Package contents

| File | Purpose |
|:---|:---|
| `Reva.exe` | Single self-contained .NET 10 application hosting the static UI, REST API, OCR, workflow, export, and assistant chat. |
| `Start-Reva.cmd` | Double-click launcher that sets `ASPNETCORE_URLS=http://localhost:5187` and starts `Reva.exe`. |
| `README-RUN.txt` | Package-local run notes and optional Ollama/Docling guidance. |

Native libraries are included in the single-file publish and self-extract as needed by the .NET host.

## Run the package

```powershell
Expand-Archive artifacts/releases/Reva-v1.3.0-win-x64.zip -DestinationPath artifacts/run/Reva
./artifacts/run/Reva/Reva.exe
```

Then open:

```text
http://localhost:5187
```

`Start-Reva.cmd` performs the same start path for double-click usage.

## Optional assistant setup

The package is fully useful without a local model. For assistant chat, install Ollama and pull the default model:

```powershell
winget install Ollama.Ollama
ollama pull qwen3-vl:8b
```

Reva best-effort starts `ollama serve` when Ollama is installed. If the model is absent, `/api/agent/status` reports that state and `POST /api/agent` returns a clear local-model-unavailable stream while the rest of the app continues to work.

## Smoke-test the package

```powershell
./tests/package-smoke.ps1
```

The smoke test builds a package, extracts it to a temp directory, starts `Reva.exe` on an alternate localhost port, and asserts:

- `GET /health` returns `status = ok` and `service = Reva`;
- `GET /api/documents/` returns an empty document list for a clean package run;
- `GET /` serves the bundled static UI and includes `Reve Intelligence`;
- `GET /api/agent/status` responds;
- `POST /api/agent` returns `text/event-stream` content.

The script stops the package process and restores environment variables after the run.

## Runtime notes

- Node.js is build-time only.
- Python is optional and only enables the richer Docling parser path.
- SQLite is the default persistence store; SQL Server can be selected by local configuration.
- The release listens on localhost and defaults to `http://localhost:5187`.
- `REVA_NO_OPEN=1` is available for headless runs.
- `--seed-demo` or `REVA_SEED_DEMO=1` loads the bundled demo corpus when needed.
