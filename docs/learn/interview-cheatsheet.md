# Interview cheatsheet

## Elevator pitch

"Reva is a reinsurance document-intelligence web app. It ingests the files brokers and cedents send every day, extracts canonical fields with source citations, reconciles stated totals against line items, and gives analysts a review workspace with export and an agentic copilot. The default pipeline is deterministic and keyless; model providers are optional."

## Architecture answer

"The product surface is `web/`: Next.js 16, React 19, Tailwind v4, and the Vercel AI SDK. The backend is `src/Reva.Web`, an ASP.NET Core .NET 10 minimal API. `Reva.Core` owns the reinsurance vocabulary and contracts. `Reva.Infrastructure` owns parsing, OCR, extraction, mapping, reconciliation, EF Core persistence, export, Knowledge Hub, and agent tools. The frontend talks to the API through one client contract, and SQLite is the default store."

## Why this stack?

"The UI needs fast iteration and a polished analyst workflow, so I used the modern React stack. The backend needs strong typing, file processing, EF Core, and reliable long-running services, so .NET fits. The agent layer uses the Vercel AI SDK and OpenAI-compatible streaming because that gives provider flexibility without tying the product to one hosted assistant."

## What makes it more than a parser?

"It closes the trust loop. A parser gives fields. Reva gives fields with provenance, source highlights, confidence, reconciliation exceptions, learned sender mappings, and export templates. An analyst can see why a value exists and correct it. The next similar file gets easier."

## Why deterministic first?

"Reinsurance documents carry financial values. The baseline has to run without a key, without a network call, and without trusting a model. Rules, tables, OCR, and reconciliation produce the source of truth. Models can propose or explain, but they don't silently override reviewed data."

## How does the copilot work?

"The chat UI streams through the Vercel AI SDK. The backend exposes a tool loop over real product actions: list documents, explain a field, correct a value, set review state, export, and search Knowledge Hub. Tool calls execute in backend code, so mutations stay typed and auditable."

## Demo script

1. Upload a bordereau or statement.
2. Show the live processing stream.
3. Open Review and point to citations.
4. Explain Detected vs Expected on Premium or Claims.
5. Correct a sender mapping.
6. Ask the copilot what needs review.
7. Search Knowledge Hub.
8. Export CSV, Excel, or JSON.

## Sharp Q&A

**What are the canonical fields?**
Cedent, Broker, Reinsurer, Contract Reference, Line of Business, Period, Currency, Premium, Claims, Commission, Cession %, Retention, Limit.

**How do citations work?**
OCR and PDF parsing create source spans with page and normalized geometry. Fields link to those spans, so the browser can highlight the source region.

**How do learned mappings work?**
When an analyst corrects a sender header, Reva stores a per-sender rule. Next time, mapping precedence is learned rule, static alias, bounded fuzzy match, then unmapped.

**What happens if no model is configured?**
Upload, extraction, reconciliation, review, and export still work. Optional chat or model-assisted extraction reports that no provider is available.

**Where would you scale it next?**
Move storage to shared SQL Server or Postgres, add background workers for high-volume ingestion, add mailbox/API ingestion, and make provider routing policy-based per tenant.
