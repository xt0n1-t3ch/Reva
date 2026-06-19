# AI and document pipeline

Reva's pipeline is deterministic first. Model features are optional assists layered on top of a source-cited extraction and reconciliation workflow.

## Stage order

1. **Ingest.** Store upload, compute SHA-256, deduplicate, and record metadata.
2. **Parse.** Route by content and parser capability: PDF, Office, spreadsheets, text, email, images.
3. **OCR.** Use local PaddleOCR for scanned images and image-only pages.
4. **Classify.** Pick the reinsurance document type.
5. **Extract.** Read canonical fields with deterministic rules and table/header signals.
6. **Map.** Convert sender-specific headers to canonical fields using learned rules, aliases, and bounded fuzzy matching.
7. **Assist.** If enabled, ask a configured model for proposals with provenance.
8. **Reconcile.** Compare stated totals with line-item totals under tolerance.
9. **Review.** Return fields, confidence, citations, issues, and suggested actions.
10. **Export.** Write CSV, Excel, or JSON from templates.

## Deterministic path

The default path uses no keys and no model provider. It owns the source of truth:

- format parsers for PDF, Office, spreadsheets, email, text, and images
- PaddleOCR for local OCR
- label and table extraction
- canonical reinsurance fields
- sender-learned schema mapping
- reconciliation rules
- provenance and normalized citation geometry

## Optional model assist

When enabled in Settings, a provider can assist extraction or chat. Supported provider classes:

- local Ollama through an OpenAI-compatible endpoint
- OpenAI-compatible hosted endpoints
- HuggingFace-backed cloud inference paths

Model output is treated as a proposal. It must carry enough evidence to be useful, and it does not replace reviewed deterministic values.

## Agentic copilot

The copilot is a Vercel AI SDK chat surface backed by an OpenAI-compatible streaming protocol. Backend tools expose real product actions:

| Tool class | Examples |
|:---|:---|
| Document lookup | list documents, open a document, summarize status |
| Review action | explain a field, correct a value, set review state |
| Reconciliation | explain Detected vs Expected, list exceptions |
| Export | create CSV, Excel, or JSON output |
| Knowledge Hub | search reference notes and cite the result |

The agent can help only through registered tools. The workflow remains auditable because the backend owns all mutations.

## Real-time processing stream

The processing stream reports stages, scanned lines, extracted fields, reconciliation figures, completion, and safe errors over server-sent events. The UI uses it to show what the system is reading while a document runs.
