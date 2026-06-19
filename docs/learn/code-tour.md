# Code tour

This tour helps you find the right part of the repository quickly.

## Repository map

| Path | What lives there | When to open it |
|:---|:---|:---|
| `web/` | Next.js product frontend. | You are changing the user interface, app shell, assistant panel, review screen, export screen, or API client. |
| `src/Reva.Web/` | ASP.NET Core host and HTTP endpoint groups. | You are adding API routes, streaming endpoints, settings endpoints, export endpoints, or static hosting behavior. |
| `src/Reva.Core/` | Domain contracts and vocabulary. | You are changing document states, canonical fields, review contracts, or shared value rules. |
| `src/Reva.Infrastructure/` | Document-processing implementation. | You are changing storage, parsing, OCR, extraction, mapping, reconciliation, export, Knowledge Hub, settings, or assistant tools. |
| `contracts/` | JSON schemas. | You are changing payload shapes that external tools or the frontend rely on. |
| `tests/` | Unit and integration tests. | You need proof for domain, infrastructure, or API behavior. |
| `docs/` | Product and technical documentation. | You are preparing a demo, interview, handoff, or release explanation. |

## One document through the system

1. `web/` posts the upload through the shared API client.
2. `src/Reva.Web` accepts the request and starts the workflow.
3. Infrastructure stores the file and computes its hash.
4. Parser routing extracts text, tables, email attachments, and/or OCR text.
5. The workflow classifies the document type.
6. Extraction creates canonical field candidates.
7. Schema mapping resolves sender-specific headers.
8. Reconciliation compares stated totals to computed totals.
9. Results are persisted with provenance and citations.
10. The review API returns a payload to the frontend.
11. The analyst approves, corrects, rejects, asks the assistant, or exports.

## Frontend notes

`web/lib/api/client.ts` is the central client contract. Keep frontend API calls there instead of scattering fetch calls across components.

Main screens:

- `web/app/page.tsx` for Workspace
- `web/app/review/page.tsx` for Review
- `web/app/export/page.tsx` for Export
- `web/app/mappings/page.tsx` for Mappings
- `web/app/knowledge/page.tsx` for Knowledge
- `web/app/settings/page.tsx` for Settings

Shared shell pieces live under `web/components/shell/`.

## Backend notes

Endpoint groups should be mapped once in `src/Reva.Web`. Duplicate registration can create ambiguous routes.

Infrastructure is intentionally broad because it owns the operational workflow. Keep behavior behind focused services so the API layer stays thin.

## Contract notes

Review payloads must include provenance. Citations can be empty only when geometry is unavailable. Bounding boxes are normalized to `0..1` against the final rendered page size.

## Test strategy

Use unit tests for deterministic rules and integration tests for API flows. For UI work, run the API and frontend, then verify the screen in a browser.
