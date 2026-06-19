# Tech stack

Reva combines a polished browser interface with a strongly typed document-processing backend.

## Next.js 16 and React 19

**Role:** product frontend in `web/`.

Reva needs a fast analyst workspace: upload, queue, review, mappings, export, Knowledge Hub, settings, and assistant. React gives composable UI. Next.js gives routing, build tooling, and static export for the packaged app.

**Interview line:** "The frontend is a modern React workspace for financial review, not just a form around an API."

## Tailwind v4 and Geist-style design

**Role:** visual system.

The UI is designed for dense data review: strong typography, monochrome surfaces, blue accents, 1px borders, small radii, and generous whitespace. Semantic tokens live in `web/app/globals.css` so components do not hardcode theme colors.

**Interview line:** "The design is intentionally quiet because the financial evidence should be louder than the chrome."

## ASP.NET Core .NET 10

**Role:** API host and production runtime.

The backend handles uploads, streaming progress, review payloads, settings, exports, Knowledge Hub, assistant tools, and static frontend hosting. .NET gives strong typing, dependency injection, streaming, EF Core, and reliable file-processing ergonomics.

**Interview line:** "The backend is a typed API around a real document workflow, not a thin mock server."

## EF Core and SQLite

**Role:** default persistence.

SQLite keeps the demo local and easy to run. EF Core keeps the data model typed and migration-ready, with a path to SQL Server or PostgreSQL later.

**Interview line:** "SQLite makes the product portable; EF Core keeps the schema ready to scale."

## Parser and OCR stack

| Capability | Implementation role |
|:---|:---|
| PDF text | Reads digital PDF text and layout where available. |
| Office documents | Reads DOCX and PPTX content. |
| Spreadsheets | Reads XLSX, XLS, CSV, TSV, ODS-style tabular data paths. |
| Email | Reads `.eml` and `.msg` messages and attachments. |
| Images and scans | Uses local OCR for text and geometry. |

**Interview line:** "The router is format-aware. Reva tries to read the file the way it actually exists, not the way the extension promises."

## Vercel AI SDK and OpenAI-compatible streaming

**Role:** assistant chat surface.

The assistant uses streaming UI primitives and backend tools. The provider seam is compatible with local or hosted model endpoints, but the product does not depend on one vendor identity.

**Interview line:** "The AI layer is provider-neutral and tool-backed; backend code owns real actions."

## Optional model providers

Reva can use local Ollama, OpenAI-compatible hosted endpoints, HuggingFace-backed inference paths, and optional layout workers when configured.

**Interview line:** "Models assist the workflow; they do not replace provenance, reconciliation, or analyst review."

## One-sentence stack summary

Next.js renders the analyst workflow, ASP.NET Core runs the document API, EF Core stores source-cited state, local OCR reads scans, deterministic rules reconcile financial values, and the AI SDK powers a provider-neutral assistant.
