# Tech stack

Use this page to explain why each technology exists in Reva.

## Next.js 16 + React 19

**What it is.** The product frontend in `web/`.

**Why it is here.** Reva needs a fast analyst workspace: upload, work queue, review canvas, field panels, mappings, export, Knowledge Hub, settings, and chat.

**How to explain it.** "The frontend is a modern React app. It gives me a polished review workflow and keeps API calls centralized in one client contract."

## Tailwind v4 + Geist-style design system

**What it is.** The styling layer and visual language.

**Why it is here.** The product needs dense financial review screens without looking noisy. The Geist look gives high contrast, hairline borders, clear typography, and disciplined spacing.

**How to explain it.** "The UI is monochrome, grid-based, and uses semantic tokens. Numbers and IDs use a mono font so financial data scans cleanly."

## Vercel AI SDK

**What it is.** The frontend chat and streaming UI layer.

**Why it is here.** Reva's copilot needs streaming responses, tool-call rendering, and provider flexibility.

**How to explain it.** "The agent UI uses the Vercel AI SDK with an OpenAI-compatible stream. That gives the same class of agentic chat primitives used by leading AI products without tying Reva to a single provider."

## ASP.NET Core .NET 10

**What it is.** The API host and backend runtime.

**Why it is here.** File processing, endpoint groups, streaming responses, dependency injection, EF Core, and strong typing fit the backend workload.

**How to explain it.** "The backend is a .NET minimal API. Endpoint groups expose documents, review, settings, Knowledge Hub, processing streams, exports, and agent chat."

## EF Core + SQLite

**What it is.** Persistence for documents, fields, citations, settings, learned mappings, Knowledge Hub records, and exports.

**Why it is here.** SQLite gives zero-setup local persistence. EF Core keeps the schema versioned and lets the same model move to a server database later.

**How to explain it.** "SQLite is the default because the demo runs anywhere. EF Core gives migrations, typed queries, and a clean path to a shared database."

## PaddleOCR

**What it is.** Local OCR for scanned images and image-only pages.

**Why it is here.** Reinsurance submissions include scans and photos. OCR must return text plus geometry for source citations.

**How to explain it.** "PaddleOCR lets the pipeline read scans locally and return bounding boxes, which the review screen uses for source highlights."

## Parser libraries

| Library | Format |
|:---|:---|
| PdfPig | Digital PDF text and layout |
| DocumentFormat.OpenXml | DOCX and PPTX |
| ClosedXML | XLSX |
| ExcelDataReader | XLS |
| MimeKit | EML email and attachments |
| MSGReader | Outlook MSG files |

**How to explain it.** "The router picks by content and parser capability, not extension alone. Unknown files become low-confidence visible text instead of hard failures."

## Optional providers

**What they are.** Local Ollama, compatible hosted model endpoints, HuggingFace-backed paths, and Docling for layout parsing.

**Why they are here.** They improve extraction and chat when available, but they are not required.

**How to explain it.** "Models are additive. The deterministic pipeline is the source of truth; providers can propose, explain, or summarize through controlled seams."

## One sentence stack summary

Next.js renders the analyst workflow, ASP.NET Core runs the document API, EF Core stores source-cited state, PaddleOCR reads scans, deterministic rules reconcile financial values, and the AI SDK powers a provider-neutral agentic copilot.
