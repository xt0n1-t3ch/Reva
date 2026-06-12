# Test suite

| Suite | Covers | Run |
|:---|:---|:---|
| [Reva.Unit](Reva.Unit/) | Reinsurance classifier and extractor behavior. | `dotnet test tests/Reva.Unit/Reva.Unit.csproj` |
| [Reva.Integration](Reva.Integration/) | Upload, parse, classify, review, and export through the real API and SQLite. | `dotnet test tests/Reva.Integration/Reva.Integration.csproj` |
| [docling-worker](../tools/docling-worker/tests/) | Python worker text and CSV parsing. | `python -m unittest discover tools/docling-worker/tests` |
