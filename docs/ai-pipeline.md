# AI and document pipeline

Reva is deterministic first. AI can assist, but it is not the foundation of trust.

That distinction is the core design decision. Financial document processing needs repeatable extraction, visible evidence, and explicit review. Models can help explain, summarize, and propose. They should not silently replace reviewed values.

## Pipeline stages

| Stage | What happens | Output |
|:---|:---|:---|
| Ingest | Store the file, hash it, detect duplicates, and record metadata. | A tracked document record. |
| Parse | Route by format and parser capability. | Text, tables, metadata, and attachments. |
| OCR | Read scans and images locally. | Text with normalized geometry when available. |
| Classify | Identify the reinsurance document type. | Document type and confidence. |
| Extract | Find canonical fields and line-item values. | Source-cited field candidates. |
| Map | Convert sender-specific headers to canonical fields. | Normalized field names and mapping confidence. |
| Assist | Optionally ask a configured model for proposals or explanations. | Suggestions, never unchecked truth. |
| Reconcile | Compare stated totals against computed totals. | Exceptions with detected and expected values. |
| Review | Present evidence, confidence, and actions to the analyst. | Approved, corrected, rejected, or pending state. |
| Export | Write selected output templates. | CSV, Excel, or JSON files. |

## Deterministic source of truth

The default workflow works without API keys and without remote AI. It includes:

- format-aware parsers
- local OCR
- label and table extraction
- canonical field normalization
- sender-specific schema mapping
- bounded fuzzy matching
- reconciliation rules
- provenance and citation geometry
- export templates

This makes Reva demoable, testable, and useful even when no model provider is configured.

## Learned schema mapping

Senders often use their own column names. One file may say `Gross Premium`; another may say `Premium Amount`; another may abbreviate it.

Reva resolves mappings in a controlled order:

1. learned sender override
2. static reinsurance alias
3. bounded fuzzy match
4. unmapped field

That order lets analyst corrections become durable without letting fuzzy matching override known decisions.

## Optional model assist

Model providers can be enabled in settings. Supported provider classes include:

- local Ollama through an OpenAI-compatible endpoint
- hosted OpenAI-compatible endpoints
- HuggingFace-backed cloud inference paths
- optional document-layout workers where configured

Model output is treated as assistance. It can propose, explain, summarize, or power chat. It does not erase the deterministic audit trail.

## Assistant tool loop

The assistant is useful because it talks to Reva's backend tools. It can answer questions such as:

- Which documents have reconciliation exceptions?
- Why did this premium field get flagged?
- Where did this value come from?
- Which file is ready for export?
- What does the Knowledge Hub say about bordereaux?

Tool-backed answers are better than free-form guesses because the backend still owns document state, exports, and review actions.

## Streaming progress

Processing can emit live status over server-sent events. The UI can show stages such as parse, OCR, extraction, mapping, reconciliation, completion, and safe errors.

This turns a black-box upload into an observable workflow.
