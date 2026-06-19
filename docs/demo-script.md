# Demo script

Use this when showing Reva in an interview.

## Start

```powershell
dotnet run --project src/Reva.Web/Reva.Web.csproj -- --no-open
cd web
$env:NEXT_PUBLIC_API_BASE_URL = "http://localhost:5158"
pnpm dev
```

Open `http://localhost:3000`.

## Walkthrough

1. **Upload.** Drop a bordereau, statement, PDF, spreadsheet, email, or image into the workspace.
2. **Watch processing.** Point out the live stage stream: parse, OCR when needed, extract, map, reconcile.
3. **Review fields.** Open the review view. Show canonical fields, confidence, provenance, source citations, and exceptions.
4. **Explain reconciliation.** Pick Premium or Claims and compare Detected vs Expected.
5. **Correct a mapping.** Change a sender header mapping and explain that learned mappings take precedence for the next document from the same sender.
6. **Ask the copilot.** Ask which document needs attention, why a field was flagged, or what to export.
7. **Knowledge Hub.** Search a reference note and show how the agent can use product knowledge without guessing.
8. **Export.** Export CSV, Excel, or JSON and explain the template boundary.

## Talk track

"Reva is a reinsurance document-intelligence web app. It turns files operations teams already receive into structured, source-cited data. The default path is deterministic and keyless. Optional model providers improve chat and extraction, but the workflow never depends on them. The interesting part is the trust loop: every value has provenance, every control total is reconciled, and analyst corrections teach the mapping layer for future files."

## Proof points to mention

- Next.js frontend with a Geist-style analyst workspace.
- ASP.NET Core API with feature endpoint groups and streaming surfaces.
- EF Core persistence over SQLite by default.
- Local PaddleOCR for scanned files.
- Vercel AI SDK chat with OpenAI-compatible streaming and backend tools.
- Optional local or hosted model providers.
