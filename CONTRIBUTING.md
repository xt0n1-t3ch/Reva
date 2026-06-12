# Contributing

Use one branch per concern. Keep contracts, backend, frontend, tests, and docs synchronized.

## Commit conventions

Use Conventional Commits: `<type>(<scope>): <imperative subject>`.

## Validation

Run the full gate before opening a pull request:

```powershell
dotnet format Reva.slnx --verify-no-changes
dotnet build Reva.slnx
dotnet test Reva.slnx
python -m pytest tools/docling-worker/tests
```

## Tests

Tests live under `tests/{Reva.Unit,Reva.Integration}` and are indexed in [tests/index.md](tests/index.md).
