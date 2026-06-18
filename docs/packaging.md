# Packaging

Reva 2.0 ships as one self-contained native executable, `Reva.exe`, published from `src/Reva.App`. The package contains no Node.js, no web server, and no separate processes. Double-clicking it opens a native desktop window — there is no URL to visit.

## Build the Windows package

```powershell
./scripts/package-windows.ps1 -Version 2.0.0
```

The script publishes the Avalonia app as a single self-contained file and zips the result:

1. `dotnet publish src/Reva.App/Reva.App.csproj` for `win-x64`, self-contained.
2. `PublishSingleFile=true` with `IncludeNativeLibrariesForSelfExtract=true`, so native dependencies (PaddleOCR, Skia, the PDF renderer) are bundled and self-extracted by the .NET host.
3. `EnableCompressionInSingleFile=true` to keep the download small.
4. Add the double-click launcher and package readme.
5. Zip the artifacts.

Expected output:

```text
artifacts/releases/Reva-v2.0.0-win-x64.zip
```

## Package contents

| File | Purpose |
|:---|:---|
| `Reva.exe` | The single self-contained .NET 10 / Avalonia desktop application. Opens a native window hosting the full pipeline, database, OCR, and copilot. |
| `Start-Reva.cmd` | Optional double-click launcher that starts `Reva.exe` from its own folder. |
| `README-RUN.txt` | Package-local run notes and optional Ollama guidance. |

Native libraries are included in the single-file publish and self-extract as needed by the .NET host on first launch.

## Run the package

```powershell
Expand-Archive artifacts/releases/Reva-v2.0.0-win-x64.zip -DestinationPath artifacts/run/Reva
./artifacts/run/Reva/Reva.exe
```

A native window opens. On first run, Reva creates its workspace under `%LOCALAPPDATA%\Reva` (the SQLite database and the uploads folder).

## Run from source during development

```powershell
dotnet run --project src/Reva.App/Reva.App.csproj
```

This launches the same native window directly, against the same per-user workspace.

## Optional assistant and VLM setup

The package is fully useful without any model — the deterministic tier ingests, extracts, reconciles, reviews, and exports offline. For the copilot and VLM-assisted extraction, install Ollama and pull a model:

```powershell
winget install Ollama.Ollama
ollama pull qwen3-vl:8b
```

Then open **Settings**, choose a model from the menu, and enable **LLM assist** for VLM extraction. The model is configurable per machine; see [model landscape](learn/model-landscape.md). When Ollama is not running or no model is installed, the copilot shows a clear local-model-unavailable message and the rest of the app continues to work.

## Runtime notes

- The app is a native window — there is no port and no localhost server.
- SQLite is the default store; SQL Server can be selected by local configuration.
- Python and Docling are optional and only enable the richer tier-three parsing path.
- The workspace lives under `%LOCALAPPDATA%\Reva`; deleting it resets the app to a clean state.
- The chosen model is persisted in the workspace and reused across launches.
