# Architecture

Reva is a web application backed by a typed .NET document-intelligence API.

The architecture is intentionally split by responsibility: the browser owns the analyst experience, the API owns product boundaries, the domain core owns vocabulary, and infrastructure owns the document workflow.

## System map

| Area | Path | Responsibility |
|:---|:---|:---|
| Product frontend | `web/` | Workspace, review, export, mappings, settings, Knowledge Hub, assistant, and the shared API client. |
| API host | `src/Reva.Web/` | HTTP endpoints, server-sent events, agent streaming, settings, exports, and static frontend hosting. |
| Domain core | `src/Reva.Core/` | Reinsurance document types, canonical fields, document states, contracts, and shared value rules. |
| Infrastructure | `src/Reva.Infrastructure/` | Storage, EF Core, parser routing, OCR, extraction, schema mapping, reconciliation, export, Knowledge Hub, and agent tools. |
| Contracts | `contracts/` | JSON schemas and normalized geometry contracts used by the review payload. |
| Tests | `tests/` | Unit and integration coverage for core behavior and API flows. |

## Runtime flow

1. The analyst uses the browser application.
2. The frontend calls the backend through `web/lib/api/client.ts`.
3. `src/Reva.Web` receives uploads, review requests, export requests, settings updates, and chat streams.
4. `DocumentWorkflow` in infrastructure processes documents through ingest, parse, OCR, classify, extract, map, reconcile, and persist stages.
5. Results are stored in SQLite by default.
6. Review payloads return fields, confidence, provenance, citations, exceptions, and available actions.
7. The assistant uses backend tools over the same persisted state.
8. Exports are produced from templates after data is available for review.

## Boundary rules

### Frontend boundary

The frontend should not duplicate backend contracts. API calls are centralized in `web/lib/api/client.ts`, so request and response shapes have one client-side owner.

### Domain boundary

Canonical reinsurance concepts live in `src/Reva.Core`. UI and persistence details should not leak into the domain vocabulary.

### Infrastructure boundary

Document processing details live in `src/Reva.Infrastructure`. This includes file storage, OCR, parsers, extraction heuristics, learned mappings, reconciliation rules, export templates, settings, and optional provider adapters.

### Provider boundary

Model providers are optional. Reva can connect to local Ollama, OpenAI-compatible endpoints, HuggingFace-backed inference, or no provider at all. Missing providers must not break the deterministic workflow.

## Data contracts

Review payloads must preserve:

- document identity and state
- canonical fields
- confidence levels
- provenance
- citations
- normalized bounding boxes when geometry exists
- reconciliation exceptions
- available review actions

Normalized geometry is important because the browser may render pages at different sizes. Coordinates are stored in `0..1` space against the rendered page.

## Scaling path

The current shape is optimized for a local-first demo and analyst workstation. The clean scale-out path is:

1. Move SQLite to shared SQL Server or PostgreSQL.
2. Move document processing into background workers.
3. Add mailbox/API ingestion for production intake.
4. Add tenant-aware storage and provider settings.
5. Add queue-based export and audit logging for high-volume operations.

The current boundaries already separate the pieces that would need to move.
