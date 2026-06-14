# Demo script

The interview walkthrough. The automated end-to-end tests (`tests/Reva.E2E`) drive this exact
flow in a real browser, so "it works" is provable on screen.

## Run it

```powershell
dotnet run --project src/Reva.Web/Reva.Web.csproj -- --seed-demo
```

Open the workspace. (Local runs don't auto-open a tab; the packaged build does.)

## The demo corpus

Seeded on first run — one document per kind, chosen to show range:

| Document | Type | Shows |
|---|---|---|
| `orion-property-cat-xl-jan-2025.eml` | Premium bordereau (broker cover-note email + CSV attachment) | The hero: extraction + **reconciliation** (the stated cover-note totals deliberately disagree with the attached line items) + source citations |
| `technical-account-statement.txt` | Statement of account | Clean extraction, zero exceptions, ready to approve |
| `operations-note.txt` | Unknown | **Never hard-rejected** — a non-reinsurance note still becomes a low-confidence reviewable record |

What the engine processes today: Excel/CSV, PDF (digital + scanned via OCR), Word, PowerPoint,
emails with attachments, plain text, and anything else best-effort. Document types it
understands: premium & claims bordereaux, statements of account, slips, loss runs, claim
notices. See `docs/research/reinsurance-landscape.md` for the domain grounding.

## Walkthrough (about 5 minutes)

1. **Workspace** — the dense operations view: KPI strip, a work queue, and the segmented
   filter (All / Needs review / Clean). Filter to *Needs review* to show triage.
2. **Import anything** — drop a file (even a random one). It is never rejected; it becomes a
   reviewable record and opens straight into review.
3. **Review & adjust** — the split view: the document on the left, extracted fields on the
   right. **Hover a field and its value lights up in the document** (the source citation).
   Edit a field; it shows as *Reviewed* once approved.
4. **Checks** — the Detected-vs-Expected reconciliation cards, computed from the data, ranked
   by how badly they disagree.
5. **Export** — JSON or CSV in one click. (Customizable export templates + Lloyd's CRS 5.2 are
   on the roadmap, issues #28/#34.)

## Why it is built well (talking points)

- **Modular**: parsing, OCR, classification, extraction, and reconciliation are separate,
  swappable pieces behind interfaces — add a parser or an LLM extractor without touching the UI.
- **Extensible**: never hard-rejects; new document types and export templates slot in.
- **Trustworthy**: confidence is computed (not faked), every value is traceable to its source,
  and corrections are audited.
- **Proven**: real Playwright end-to-end tests run this whole demo on every change.

## Architecture

- Backend-first, contract-driven; the UI injects the workflow directly.
- Native .NET parsers + offline PaddleOCR; EF Core (SQLite default, SQL Server by config).
- Human-in-the-loop review for auditability. Synthetic data only.
