# Changelog

All notable changes to this project are documented here. The format is based on
Keep a Changelog 1.1.0, and this project follows Semantic Versioning.

## [Unreleased]

## [0.1.2] - 2026-06-12

### Added
- One-click Windows launch: double-clicking `Reva.exe` now starts the local server and opens the browser automatically.

### Fixed
- Unsupported uploads are quarantined as unsupported documents instead of being presented as extracted reinsurance records.
- Export now blocks non-extracted or unknown documents with a clear conflict response.
- Existing low-confidence unknown records are normalized at startup so old local demo data stops polluting the queue.

## [0.1.1] - 2026-06-12

### Added
- Windows `win-x64` self-contained package script and package smoke test.
- `/health` endpoint for packaged-app and deployment verification.

### Fixed
- Rebranded the in-app rail from ReActive Intelligence to Reva Document AI.
- Fixed SQLite document listing failures caused by ordering `DateTimeOffset` values in SQL.
- Added in-process parser fallback so TXT, Markdown, CSV, PDF, and image intake can run without Python.

## [0.1.0] - 2026-06-12

### Added
- Blazor analyst cockpit for upload, extraction review, exception triage, and export.
- ASP.NET Core API with SQLite-backed document workflow state.
- Local Python parser worker with Docling adapter path and deterministic fallback parsing.
- Reinsurance field extraction for technical accounts, bordereaux, and statements of account.
- Contract schemas, sample documents, architecture docs, AI pipeline notes, and CI validation.
