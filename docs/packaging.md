# Packaging

Reva ships as source code and as a Windows self-contained package.

## Build the Windows package

```powershell
./scripts/package-windows.ps1 -Version 0.1.1
```

The script writes:

```text
artifacts/releases/Reva-v0.1.1-win-x64.zip
```

The archive contains:

- `Reva.exe` — self-contained ASP.NET Core/Blazor application.
- `Start-Reva.cmd` — double-click launcher that opens `http://localhost:5187`.
- `README-RUN.txt` — package-local run notes.
- Optional Docling worker files under `tools/docling-worker/`.

## Run the package

```powershell
Expand-Archive artifacts/releases/Reva-v0.1.1-win-x64.zip -DestinationPath artifacts/run/Reva
./artifacts/run/Reva/Start-Reva.cmd
```

Or run the executable directly:

```powershell
./artifacts/run/Reva/Reva.exe --urls http://localhost:5187
```

## Smoke-test the package

```powershell
./tests/package-smoke.ps1
```

The smoke test builds the package, extracts it to a temp folder, starts `Reva.exe`, verifies `/health`, verifies `/api/documents/`, and shuts the process down.

## Runtime notes

The package bundles .NET for `win-x64`. Python is optional. TXT, Markdown, CSV, PDF, and image intake have an in-process fallback path; installing Python and Docling enables richer document parsing through the optional worker.