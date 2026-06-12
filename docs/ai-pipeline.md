# AI Pipeline

Reva treats AI/OCR as a core backend capability, not a UI add-on.

## Runtime path

1. Store upload with a SHA-256 hash.
2. Invoke the local parser worker.
3. Return Markdown, text, tables, parser profile, and warnings.
4. Classify the reinsurance document type.
5. Extract canonical fields and confidence scores.
6. Surface issues for human review.
7. Export approved JSON or CSV.

## Default local profile

The worker uses Docling when it is available locally. Docling is a practical default because it focuses on document conversion, tables, OCR-capable parsing, Markdown, and JSON-style downstream use. The fallback parser keeps the demo runnable without GPU, cloud credentials, or large model downloads.

## Optional high-accuracy profile

PaddleOCR-VL is the recommended optional adapter for scanned or visually complex documents. It is a lightweight vision-language OCR family aimed at document parsing tasks such as text, tables, charts, and complex layout. It should be added behind the existing `IDocumentParser` contract rather than wired directly into the UI.

## Why not cloud-first

Azure Document Intelligence is a strong production adapter for Microsoft-heavy enterprises, but this interview MVP is local-first to avoid secrets and prove architecture. The production path is an additional parser implementation, not a rewrite.

## Roadmap

- Add a `PaddleOcrVlDocumentParser` worker mode.
- Add table reconciliation checks for totals and currency consistency.
- Add confidence calibration from reviewed corrections.
- Add SQL Server migrations and Power BI export views.
