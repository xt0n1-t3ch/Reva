# AI Pipeline

Reve Intelligence treats parsing, OCR, extraction, and reconciliation as core backend
capabilities, not UI add-ons. The default path is keyless and offline.

## Runtime path

1. Store the upload with a SHA-256 hash (safe random name, no-execute).
2. Route it through `ParserRouter` by sniffed content — never by extension alone.
3. Get back text, Markdown, tables, a parser profile, and any warnings.
4. Classify the reinsurance document type (bordereau-aware).
5. Map sender-specific headers to canonical fields with confidence.
6. Extract canonical fields with computed, explainable confidence.
7. Reconcile stated figures against the line-item totals (see below).
8. Surface field-level exceptions for human review.
9. Export approved JSON or CSV.

## Parsers (native .NET, no Python required)

| Input | Parser |
| --- | --- |
| txt / md / csv | built-in (encoding-detected) |
| docx / pptx | DocumentFormat.OpenXml |
| xlsx | ClosedXML |
| eml | MimeKit (body + recursive attachments) |
| msg | MSGReader |
| digital PDF | PdfPig |
| images / scanned PDF | PaddleOCR (PP-OCR V5, bundled, offline) |
| unknown / binary | best-effort visible-text fallback (low confidence, never an error) |

## OCR

`Sdcb.PaddleOCR` with the local PP-OCR V5 models runs entirely on the machine — no Python, no
cloud, no API key. Per-line confidence comes from the engine, so the scores are real. Models
are loaded lazily and provisioned on first use; a missing model degrades gracefully instead of
crashing.

## Schema mapping

The mapper combines static reinsurance aliases with learned EF overrides keyed by sender or
email domain. It records every source header, canonical target, normalized value, confidence,
and source (`alias`, `fuzzy`, `learned`, or `unmapped`) so Review can show why a column landed
where it did. Analyst corrections persist as sender-specific rules and take precedence on the
next document from that sender.

## Reconciliation

A real, common reinsurance break: a broker cover note states headline totals that do not match
the attached bordereau line items. The extractor computes the line-item totals and compares
them to the stated values, emitting a field-level exception with the Detected value, the
Expected (computed) value, and an agreement score. The seeded hero document demonstrates this
with genuine, varied discrepancies — see `docs/demo-script.md`.

## Optional LLM (deferred)

`Microsoft.Extensions.AI` can supply an optional `IChatClient` that proposes field candidates;
the deterministic validators still decide. It stays off by default (`Reve:Llm:Provider = None`)
so the app runs keyless. Tracked separately as an issue.

## Why local-first

Cloud OCR (e.g. Azure Document Intelligence) is a strong production adapter, but local-first by
default avoids secrets, removes setup friction, and keeps the architecture portable. Production
adapters plug in behind the existing parser contract — they are not a rewrite.
