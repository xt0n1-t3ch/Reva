# Reva agent guide

## Commands
- Restore/build: `dotnet build Reva.slnx -warnaserror`
- Tests: `dotnet test`
- Format before EF migration commits: `dotnet format`
- Run app without browser: `dotnet run --project src/Reva.Web/Reva.Web.csproj -- --no-open`

## Layout
- Core contracts and domain types: `src/Reva.Core`
- Backend services, parsing, extraction, OCR, persistence: `src/Reva.Infrastructure`
- Blazor host and HTTP API endpoints: `src/Reva.Web`
- Unit/integration tests: `tests/Reva.Unit`, `tests/Reva.Integration`
- Contract schemas: `contracts`

## Boundaries
- Do not edit `src/Reva.Web/Components/**`, `web/**`, `tests/Reva.E2E/**`, CI workflows, or release automation unless Tony explicitly scopes it.
- Keep the keyless/offline default path working. External AI and Docling paths stay disabled unless config enables them.
- Secrets come from environment or local config only and are never committed.

## API contracts
- Review payloads follow `contracts/bdx-review-payload.schema.json`.
- Bounding boxes are normalized to `0..1` against the final rendered page size.
- Provenance is always present; citations may be empty only when geometry is unavailable.
