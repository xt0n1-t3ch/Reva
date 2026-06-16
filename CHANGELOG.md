# Changelog

All notable changes to this project are documented here. The format is based on
Keep a Changelog 1.1.0, and this project follows Semantic Versioning.

## [Unreleased]

## [1.3.0] - 2026-06-16

First stable AI-native release. Reva is now a single self-contained desktop
application with an offline document-intelligence pipeline and a grounded
assistant.

### Added
- All-in-one Windows executable: a single self-contained `Reva.exe` serves the
  web UI, REST API, OCR engine, and assistant chat from one localhost origin —
  no Node.js, no separate web server, no extra processes.
- Grounded assistant chat: a local, keyless model (Ollama `qwen3-vl`) answers
  questions and runs tools (list documents, inspect a document, reconcile,
  explain a field) over the real document workflow. Chat degrades to a clear
  "local model unavailable" message when no model is installed; every other
  feature keeps working.
- Next.js operations workspace: a dense work-queue dashboard, drag-and-drop
  upload, and a Rossum-style split-view review with live source-citation
  overlays drawn on the rendered document page.
- Source citations end to end: OCR captures per-line bounding boxes and review
  highlights the exact region each field was read from, scaling with zoom.
- Scanned-document OCR: image and scanned-PDF intake render pages and extract
  fields with real page coordinates via the bundled offline PaddleOCR engine.
- Export template editor: create, edit, duplicate, and delete templates with
  format-aware CSV/Excel/JSON download and live preview.
- Settings depth: configurable reconciliation tolerance, an optional
  LLM-assisted extraction toggle (off by default), export defaults, and data
  management (reseed/clear).
- Schema-mappings page showing per-sender header-to-canonical mappings with
  learned and corrected provenance.
- First-run guided onboarding tour, replayable from the app.
- File-based inbound document seam for `.eml`/`.msg` ingestion.

### Changed
- Rebuilt the interface as a Next.js static frontend served by the .NET host;
  the former Blazor cockpit is retired and the host is now an API plus a
  static-UI server, same origin.
- Extraction and reconciliation remain fully deterministic and offline by
  default; the local model is optional and never overrides validated figures.

### Fixed
- Reconciliation honors a configurable money tolerance instead of exact-match
  comparison only.
- OCR bounding boxes are captured from real region geometry, so citations point
  at the correct area of the page instead of the whole page.

### Security
- The packaged application binds to localhost only, ships no secrets, and serves
  the bundled UI read-only from a single port.

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
