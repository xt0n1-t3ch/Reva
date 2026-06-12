# Demo Script

## 1. Set context

Reva is a local-first reinsurance document intelligence cockpit for technical accounting and bordereaux review. It mirrors the kind of AI-assisted document workflow Active Re describes in its technical operations and AI processing updates.

## 2. Run the app

```powershell
dotnet run --project src/Reva.Web/Reva.Web.csproj
```

## 3. Upload a sample

Use `samples/technical-account-statement.txt` or `samples/bordereau.csv`. The app stores the upload, hashes it, parses it locally, classifies the document type, extracts canonical fields, and flags missing or suspicious values.

## 4. Review the extraction

Open the document review screen. Show the split preview, extracted fields, confidence badges, issue panel, and editable corrections.

## 5. Approve and export

Approve the reviewed record and export JSON or CSV. Explain that SQLite is the demo default and SQL Server is a configuration switch.

## 6. Architecture talking points

- Backend-first contract-driven design.
- Local parser worker with Docling and optional VLM/OCR adapters.
- EF Core persistence with SQL Server path.
- Human-in-the-loop review for auditability.
- Synthetic data only, no private documents committed.
