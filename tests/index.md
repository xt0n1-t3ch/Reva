# Test suite

| Suite | Covers | Run |
|:---|:---|:---|
| [Reva.Unit](Reva.Unit/) | Reinsurance classifier, extractor, reconciliation, and schema-mapping behavior. | `dotnet test tests/Reva.Unit/Reva.Unit.csproj` |
| [Reva.Integration](Reva.Integration/) | Upload, parse, classify, schema-map, review, and export through the real API and SQLite. | `dotnet test tests/Reva.Integration/Reva.Integration.csproj` |
| [Reva.E2E](Reva.E2E/) | Browser walkthrough for workspace, review citations, schema mapping, exports, and never-reject import. | `dotnet test tests/Reva.E2E/Reva.E2E.csproj --no-build` |
| [docling-worker](../tools/docling-worker/tests/) | Python worker text and CSV parsing. | `python -m unittest discover tools/docling-worker/tests` |
