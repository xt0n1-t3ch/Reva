<p align="center">
  <img src="docs/assets/reva-banner.png" alt="Reva AI document intelligence cockpit banner">
</p>

<h1 align="center">Reva</h1>

<p align="center">
  <strong>AI-powered document review for reinsurance workflows.</strong>
</p>

<p align="center">
  <a href="https://github.com/xt0n1-t3ch/Reva/actions/workflows/ci.yml"><img src="https://github.com/xt0n1-t3ch/Reva/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 10">
  <img src="https://img.shields.io/badge/Blazor-Web%20App-512BD4?logo=blazor&logoColor=white" alt="Blazor Web App">
  <img src="https://img.shields.io/badge/Document%20AI-OCR%20%2B%20Extraction-00B3A4" alt="Document AI">
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-green.svg" alt="MIT License"></a>
</p>

Reva is a local-first document intelligence cockpit that turns messy reinsurance documents into structured, reviewable, export-ready data. It combines a Blazor analyst UI, an ASP.NET Core API, SQLite-backed workflow state, and a Python parser worker so teams can ingest technical accounts, bordereaux, statements of account, and scanned or semi-structured files; extract financial fields and tables; flag exceptions; capture review decisions; and export approved records.

## Quick start

```powershell
dotnet restore Reva.slnx
dotnet test Reva.slnx
dotnet run --project src/Reva.Web/Reva.Web.csproj
```

Open the URL printed by ASP.NET Core, upload one of the files in `samples/`, review the extracted fields, then export JSON or CSV.

## Stack

- .NET 10 Blazor Web App for the analyst cockpit and API.
- EF Core with SQLite by default and a SQL Server-ready provider switch.
- Local Python parser worker with a Docling adapter path and deterministic text/CSV fallback.
- Contract-first payloads in `contracts/`.

## Documentation

- [Documentation index](docs/index.md)
- [Architecture](docs/architecture.md)
- [AI pipeline](docs/ai-pipeline.md)
- [Demo script](docs/demo-script.md)
- [Visual reference](docs/visual-references/reva-intelligence-cockpit-reference.png)

## License

MIT — see [LICENSE](LICENSE).
