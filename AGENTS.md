# Reva agent guide

## Commands
- Restore/build: `dotnet build Reva.slnx -warnaserror`
- Tests: `dotnet test`
- Format before EF migration commits: `dotnet format`
- Run API without opening a browser: `dotnet run --project src/Reva.Web/Reva.Web.csproj -- --no-open`
- Run web app: from `web/`, set `NEXT_PUBLIC_API_BASE_URL=http://localhost:5158`, then `pnpm dev`

## Layout
- Core contracts and domain types: `src/Reva.Core`
- Backend services, parsing, extraction, OCR, persistence: `src/Reva.Infrastructure`
- ASP.NET Core host and HTTP API endpoints: `src/Reva.Web`
- Active product frontend: `web/`
- Unit/integration tests: `tests/Reva.Unit`, `tests/Reva.Integration`
- Contract schemas: `contracts`

## Boundaries
- `web/` is the active product frontend. Work there for UI only when Tony scopes UI work.
- Do not edit `tests/Reva.E2E/**`, CI workflows, or release automation unless Tony explicitly scopes it.
- Keep the keyless/offline default path working. External AI and Docling paths stay disabled unless config enables them.
- Secrets come from environment or local config only and are never committed.

## API contracts
- Review payloads follow `contracts/bdx-review-payload.schema.json`.
- Bounding boxes are normalized to `0..1` against the final rendered page size.
- Provenance is always present; citations may be empty only when geometry is unavailable.
- Frontend API calls are centralized in `web/lib/api/client.ts`; do not break that contract.

## Reinsurance domain
Reva processes the documents that flow between cedents, brokers, and reinsurers.
- Document types (`ReinsuranceDocumentType`): Treaty, FacultativeSlip, Bordereau (premium/claims), StatementOfAccount (technical account), LossRun, Endorsement, ClaimNotice.
- Canonical fields (13, `ReinsuranceFieldNames`): Cedent, Broker, Reinsurer, ContractReference, LineOfBusiness, Period, Currency, Premium, Claims, Commission, Cession %, Retention, Limit.
- Reconciliation checks stated control totals against computed line-item values with a settings-driven relative tolerance. Mismatches become exceptions carrying Detected vs Expected and severity.

## Document pipeline
Stage order (`DocumentWorkflow`): ingestion with SHA-256 dedup -> parse/route by format -> OCR for scanned images -> classification -> layout/table analysis -> deterministic field extraction -> optional model assist -> reconciliation/anomaly detection -> export.
- Formats handled: PDF, PNG/JPG, CSV/TSV, XLSX/XLS, ODS, DOCX/PPTX, EML/MSG with recursive attachment parse.
- The keyless deterministic path is the source of truth and always works. AI is optional. Every extracted value carries provenance.
- Export uses reusable templates for CSV, Excel, and JSON, including Lloyd's CRS 5.2 and bordereau line-item layouts.
- Known limitation: scanned-PDF page rasterization still needs the production renderer path; native-text PDFs and image OCR work today.

## Frontend
- Product UI: `web/` (Next.js 16 + React 19 + Tailwind v4 + Vercel AI SDK), talking to `src/Reva.Web` over HTTP/SSE/streaming responses.
- Dev: run the API on `:5158` and the web dev server on `:3000` with `NEXT_PUBLIC_API_BASE_URL` pointed at the API.
- Prod build is static export served from the API's `wwwroot`.
- Design system: Vercel Geist look, pure black/white themes, Geist Sans + Geist Mono, 1px bordered grid, generous whitespace, small radii, disciplined blue accent `#0070F3`, and semantic tokens in `web/app/globals.css`.
- Use semantic token utilities instead of hardcoded colors. Layouts must fill the viewport height. Verify UI changes live in the browser.
