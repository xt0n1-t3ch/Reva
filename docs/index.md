# Reva documentation

Reva is a local-first document-intelligence workspace for reinsurance operations. It turns inbound documents into source-cited, reconciled, reviewable, export-ready data.

This documentation is written for two audiences:

- **Non-programmers** who need to understand what the product does and why it matters.
- **Interviewers and engineers** who need to understand the architecture, tradeoffs, and proof points quickly.

## Start here

| Page | Best for | What you will learn |
|:---|:---|:---|
| [README](../README.md) | First impression | Product value, screenshots, quick start, and repo map. |
| [Product guide](product-guide.md) | Non-technical overview | The end-to-end workflow in plain language. |
| [Demo script](demo-script.md) | Interviews and showcases | A short, reliable walkthrough with talk track. |
| [Architecture](architecture.md) | Technical review | Boundaries, data flow, runtime shape, and extension points. |
| [AI and pipeline](ai-pipeline.md) | Workflow explanation | Deterministic stages, optional model assist, and agent tools. |
| [Packaging](packaging.md) | Release handoff | Development, static export, Windows packaging, and validation. |

## Learning track

| Page | Use it when |
|:---|:---|
| [Interview cheatsheet](learn/interview-cheatsheet.md) | You need short answers to likely questions. |
| [Code tour](learn/code-tour.md) | You want to find the right folder or class quickly. |
| [Tech stack](learn/tech-stack.md) | You need to explain why each technology was chosen. |
| [Model landscape](learn/model-landscape.md) | You need to discuss local and hosted model options. |

## Domain track

| Page | Use it when |
|:---|:---|
| [Reinsurance landscape](research/reinsurance-landscape.md) | You need the business context: documents, roles, breaks, and why source citations matter. |

## Product constraints

These rules keep Reva trustworthy:

- The deterministic path works without keys, hosted models, or network-only services.
- AI is optional and settings-driven.
- Every extracted value carries provenance.
- Citation boxes use normalized coordinates when geometry is available.
- Reconciliation compares stated totals to computed totals under a configured tolerance.
- The frontend talks to the backend through one centralized API client contract.
- The web app is the product surface; docs should describe the current `web/` experience.

## Short product summary

Reva receives the files a reinsurance analyst already handles, reads them, extracts the canonical fields, cites where each value came from, flags financial mismatches, lets the analyst approve or correct the result, and exports clean data.
