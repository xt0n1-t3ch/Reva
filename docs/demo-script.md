# Demo script

> This page describes the legacy browser host (`src/Reva.Web`). For the 2.0 native desktop app, use the demo script in the [interview cheatsheet](learn/interview-cheatsheet.md#demo-script), which walks the same flow inside the Avalonia window. The product walkthrough below still maps one-to-one onto the native screens (Dashboard, Review, Mappings, Export, Settings, Copilot).

This script shows Reva as a product: one app that ingests messy reinsurance documents, extracts and maps canonical fields, reconciles headline figures against line items, supports source-cited human review, and exports approved data.

## Run the native app

```powershell
dotnet run --project src/Reva.App/Reva.App.csproj
```

A native window opens. The packaged executable follows the same flow after launching `Reva.exe`.

## Run the legacy browser host

```powershell
dotnet run --project src/Reva.Web/Reva.Web.csproj -- --seed-demo
```

Open `http://localhost:5187`. This path is retained for reference and is not the 2.0 product.

## Demo corpus

| Document | Type | What it demonstrates |
|:---|:---|:---|
| `orion-property-cat-xl-jan-2025.eml` | Premium bordereau email with attachment | Email intake, attachment parsing, extraction, learned schema mapping, source citations, and reconciliation breaks where stated cover-note totals differ from summed line items. |
| `meridian-property-cat-xl-bordereau-2025-q1.png` | Scanned bordereau image | Offline PaddleOCR on a scanned page, field extraction with real page coordinates, and geometry-backed citation overlays in the split view. |
| `technical-account-statement.txt` | Statement of account | Clean technical-account extraction, confidence, approval, and export. |
| `operations-note.txt` | Unknown operational note | Never-hard-reject behavior: the file still becomes a low-confidence reviewable record. |

Reva can also process Excel/CSV, digital PDFs, scanned PDFs and images, Word, PowerPoint, plain text, email bodies, email attachments, and best-effort visible text from unknown files.

## Five-minute walkthrough

1. **Workspace** — Start at the work queue. Point out status, confidence, exceptions, and real page thumbnails for image/PDF entries. Filter to items that need review.
2. **Import** — Drop a document or use the seeded `.eml`. Reva stores it, hashes it, parses the email body and attachment, classifies it, extracts fields, maps headers, reconciles values, and opens review.
3. **Review split view** — On the left, document pages render with source highlights. On the right, canonical fields show values, confidence, status, citations, and exceptions. Hover or focus a field to highlight the exact source region.
4. **Schema mapping** — Show the source header → canonical target mapping evidence. Correct a mapping once; the sender-specific EF rule is learned and takes precedence for the next document from that sender/domain.
5. **Reconciliation** — Open the exception cards. Explain **Detected** as the stated value, **Expected** as the computed line-item total, and the agreement score as a value in `[0,1]`. Money checks honor the configured tolerance.
6. **Assistant** — Open the assistant. Ask for the document summary or why a field failed reconciliation. If Ollama/model is not installed, show the graceful local-model-unavailable message and continue the core flow.
7. **Export** — Pick a saved template, preview the output, and download CSV, Excel, or JSON. The Lloyd's CRS template demonstrates downstream reporting shape.
8. **Settings** — Show theme/accent/branding, reconciliation tolerance, optional LLM-assisted extraction, default export template, reseed, and clear-data controls.

## Talking points

- **Local-first:** native parsers, bundled OCR, SQLite, deterministic extraction, and exports all run without external services.
- **Trustworthy:** every extracted field carries provenance; geometry-backed citations highlight source regions; analyst edits become **Reviewed** instead of artificial confidence.
- **Adaptive:** schema mapping starts with aliases and fuzzy matching, then learns sender/domain rules from corrections.
- **Operational:** reconciliation turns mismatches into actionable field-level exceptions instead of burying them in tables.
- **Simple to deploy:** the release is one `Reva.exe` serving the UI, API, OCR, and assistant endpoint from one localhost origin.

## Proof commands

```powershell
dotnet test Reva.slnx
cd web
npx playwright test
```

Package proof:

```powershell
./scripts/package-windows.ps1 -Version 1.3.0
./tests/package-smoke.ps1
```
