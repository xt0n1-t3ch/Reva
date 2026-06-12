# Reva

> Reinsurance document intelligence cockpit for technical accounting, bordereaux, statements of account, and analyst review workflows.

Reva is a local-first .NET and document-AI MVP built for a Programmer Analyst interview scenario. It demonstrates how an internal reinsurance operations team can upload documents, parse text and tables, classify the document type, extract canonical financial fields, review exceptions, and export approved records.

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
