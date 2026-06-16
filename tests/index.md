# Test suite

| Suite | Covers | Run |
|:---|:---|:---|
| [Reva.Unit](Reva.Unit/) | Reinsurance classifier, extractor, reconciliation, and schema-mapping behavior. | `dotnet test tests/Reva.Unit/Reva.Unit.csproj` |
| [Reva.Integration](Reva.Integration/) | Upload, parse, classify, schema-map, review, settings, and export through the real API and SQLite. | `dotnet test tests/Reva.Integration/Reva.Integration.csproj` |
| [Reva.E2E](Reva.E2E/) | API host smoke coverage for `/health`, `/api/documents`, and review payload contracts. | `dotnet test tests/Reva.E2E/Reva.E2E.csproj --no-build` |
| [web/e2e](../web/tests/e2e/) | Next.js workspace, review citation hover, export, settings, onboarding, and axe checks against the live stack. | `cd web && npx playwright test` |
| [docling-worker](../tools/docling-worker/tests/) | Python worker text and CSV parsing. | `python -m unittest discover tools/docling-worker/tests` |
