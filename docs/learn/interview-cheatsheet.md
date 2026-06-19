# Interview cheatsheet

## Elevator pitch

Reva is a local-first document-intelligence workspace for reinsurance operations. It reads the files brokers and cedents send every day, extracts canonical fields with citations, reconciles control totals, gives analysts a review workflow, and exports clean data.

## The one-minute architecture answer

The product surface is `web/`, a Next.js 16 and React 19 app with Tailwind v4 and a Geist-style design system. The backend is `src/Reva.Web`, an ASP.NET Core .NET 10 API. `src/Reva.Core` owns the reinsurance vocabulary and contracts. `src/Reva.Infrastructure` owns storage, parsing, OCR, extraction, mapping, reconciliation, export, Knowledge Hub, settings, and assistant tools. SQLite is the default local store.

## What makes Reva credible

- It works without a hosted model key.
- It handles real operational file variety.
- It preserves provenance for every extracted value.
- It checks stated totals against computed totals.
- It lets analysts approve, correct, reject, and export.
- It keeps optional AI behind explicit provider settings.
- It uses backend tools so assistant actions stay typed and auditable.

## Strong answers

### Why deterministic first?

Reinsurance documents contain financial values. A system that silently trusts a model is not enough. Reva's baseline path uses parsers, OCR, rules, mappings, and reconciliation so it can run locally and produce reviewable evidence. Models can help, but reviewed deterministic state wins.

### How does source citation work?

Parsers and OCR create source spans. When geometry is available, coordinates are normalized from `0..1` against the rendered page. That lets the frontend highlight the right region regardless of zoom or screen size.

### What are the canonical fields?

Cedent, Broker, Reinsurer, Contract Reference, Line of Business, Period, Currency, Premium, Claims, Commission, Cession %, Retention, and Limit.

### How do learned mappings work?

Sender headers are resolved in a controlled order: learned sender override, static alias, bounded fuzzy match, then unmapped. Analyst corrections can improve the next document from the same sender without making fuzzy matching too aggressive.

### What does the assistant actually do?

The assistant streams through an OpenAI-compatible protocol and uses backend tools. It can list documents, explain fields, inspect exceptions, search Knowledge Hub, open records, update review state, and create exports through Reva's API boundary.

### What happens with no AI provider?

Upload, parse, OCR, extraction, mapping, reconciliation, review, and export still work. Optional chat or model-assist surfaces can report that no provider is configured, but the core workflow remains usable.

### Where would you scale it next?

Move persistence from local SQLite to shared SQL Server or PostgreSQL, run document processing in background workers, add mailbox/API ingestion, add tenant-aware settings, and route provider policy per tenant or workload.

## Demo checklist

1. Open Workspace and show intake status.
2. Open Review and show confidence, source citations, and reconciliation exceptions.
3. Open Mappings and explain sender-specific learning.
4. Ask Assistant which documents need review.
5. Open Knowledge and show domain context.
6. Open Export and download CSV or JSON.
7. Open Showcase for the guided tour.

## Closing line

Reva is not just extracting text. It is building a trust loop around reinsurance documents: read, cite, reconcile, review, learn, and export.
