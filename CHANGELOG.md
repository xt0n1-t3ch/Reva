# Changelog

All notable changes to this project are documented here. The format is based on Keep a Changelog 1.1.0, and this project follows Semantic Versioning.

## [Unreleased]

## [1.4.0] - 2026-06-19

### Added
- Real-time document processing stream: a live view that shows each source line as it is read, then each extracted field, schema mapping, and reconciliation, on a stage-by-stage timeline. Documents without optical character recognition stream their parsed source text.
- Guided Showcase launched from the header: a step-by-step tour of every capability, with a one-click action to load the bundled demonstration dataset so a presentation needs no external files.
- Review template view: a Source and Template toggle that renders each record in a clean, standards-ordered layout for presentation.
- Assistant date and time tool so the copilot answers questions about the current date.
- Windows application icon set to the Reva brand mark.

### Changed
- Refocused Reva on the web app: Next.js 16 + React 19 + Tailwind v4 frontend, ASP.NET Core .NET 10 API, SQLite EF Core persistence (SQL Server configurable), and the reinsurance domain core. The retired non-web shell was removed.
- Reasoning controls enable model thinking at Medium and above on capable providers, and the streaming layer forwards reasoning when a provider emits it.
- Professional-language pass across the interface and documentation.

### Fixed
- The assistant no longer renders an empty message bubble; a live working indicator shows elapsed time until the answer begins to stream.
- The work queue requests a page image only for formats that render one, keeping the browser console clean.

## [1.3.0] - 2026-06-16

### Added
- Source-cited reinsurance document pipeline with parsing, local OCR, deterministic extraction, schema mapping, reconciliation, review, and export.
- Next.js operations workspace with upload, queue, review, mappings, export, settings, and assistant chat surfaces.
- SQLite EF Core persistence for documents, fields, source spans, settings, audit events, and learned mappings.
- Optional local model path through Ollama for extraction assist and grounded chat.

### Changed
- Kept the keyless deterministic path as the default. Model features stay additive and configurable.
- Made source provenance part of every extracted field. Geometry-backed citations highlight source regions when available.
