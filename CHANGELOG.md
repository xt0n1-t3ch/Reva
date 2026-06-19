# Changelog

All notable changes to this project are documented here. The format is based on Keep a Changelog 1.1.0, and this project follows Semantic Versioning.

## [Unreleased]

### Changed
- Refocus Reva on the web app: Next.js 16 + React 19 + Tailwind v4 frontend, ASP.NET Core .NET 10 API, SQLite EF Core persistence, and the existing reinsurance domain core.
- Remove the retired non-web shell from product docs and onboarding material.
- Present the agentic copilot as a modern Vercel AI SDK and OpenAI-compatible streaming implementation, vendor-neutral and provider-configurable.

### Added
- Multi-provider model story covering local Ollama, OpenAI-compatible endpoints, and HuggingFace cloud paths when configured.
- Knowledge Hub positioning for searchable analyst reference material.
- Agentic copilot positioning with real backend tools for document lookup, review, correction, export, and knowledge search.
- Real-time processing stream positioning for stage, line, field, reconciliation, and completion events.

## [1.3.0] - 2026-06-16

### Added
- Source-cited reinsurance document pipeline with parsing, local OCR, deterministic extraction, schema mapping, reconciliation, review, and export.
- Next.js operations workspace with upload, queue, review, mappings, export, settings, and assistant chat surfaces.
- SQLite EF Core persistence for documents, fields, source spans, settings, audit events, and learned mappings.
- Optional local model path through Ollama for extraction assist and grounded chat.

### Changed
- Kept the keyless deterministic path as the default. Model features stay additive and configurable.
- Made source provenance part of every extracted field. Geometry-backed citations highlight source regions when available.
