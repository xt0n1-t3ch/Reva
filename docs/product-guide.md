# Product guide

Reva is a document-intelligence application for reinsurance operations.

It is designed around a practical problem: reinsurance teams receive important financial information in many different formats. One sender may send a CSV. Another may send an Excel workbook. Another may send a PDF, a scan, an email attachment, or a photographed table. The analyst still has to turn all of that into reliable, reviewable, exportable data.

Reva makes that workflow visible and controlled.

## The workflow in plain language

### 1. Ingest the document

An analyst uploads a bordereau, statement, slip, loss run, claim notice, endorsement, spreadsheet, email, PDF, scan, image, or text file.

Reva stores the file, fingerprints it with SHA-256, and records the processing state so duplicate and follow-up work can be tracked.

### 2. Read the content

Reva chooses a parser based on the document type and content. Digital files are read directly. Images and scans go through local OCR so the text can be searched, cited, and reviewed.

Unknown or imperfect formats do not become hard failures. They degrade into visible text with lower confidence, which keeps the analyst in control.

### 3. Classify the document

The system identifies the business document type, such as:

- bordereau
- statement of account
- facultative slip
- treaty document
- loss run
- endorsement
- claim notice

Classification helps the extractor know which values matter.

### 4. Extract canonical fields

Reva normalizes the fields a reinsurer expects to see. The core set is:

| Canonical field | Meaning |
|:---|:---|
| Cedent | The party transferring risk. |
| Broker | The intermediary placing or managing the business. |
| Reinsurer | The party accepting risk. |
| Contract Reference | The policy, treaty, or contract identifier. |
| Line of Business | The class of risk, such as property, casualty, marine, or aviation. |
| Period | The accounting or coverage period. |
| Currency | The money unit used for reported values. |
| Premium | Reported premium amount. |
| Claims | Reported claim amount. |
| Commission | Commission value or rate. |
| Cession % | Share of risk ceded. |
| Retention | Amount retained before reinsurance responds. |
| Limit | Maximum covered amount. |

### 5. Cite the source

A value without evidence is not enough for financial operations. Reva attaches provenance to extracted values and uses page/geometry citations when available.

That means a reviewer can ask: "Where did this premium number come from?" and get an answer tied back to the original document.

### 6. Reconcile totals

Reva compares stated totals against computed line-item totals. If the numbers do not match within the configured tolerance, it creates an exception with detected and expected values.

The goal is not to hide uncertainty. The goal is to show it early, clearly, and with enough context to resolve it.

### 7. Review and correct

The review workspace shows document state, extracted fields, confidence, citations, exceptions, and actions. Analysts can approve, request changes, reject, or correct values.

When a sender-specific column header is corrected, the mapping layer can remember that decision for the next file from the same sender.

### 8. Ask the assistant

The assistant answers grounded questions about processed documents, exceptions, classifications, field provenance, exports, and Knowledge Hub notes. It uses backend tools over Reva's stored state instead of guessing from an isolated prompt.

### 9. Export clean data

Reviewed data can be exported as CSV, Excel, or JSON. Templates keep the output aligned with downstream market and reporting needs.

## Why local-first matters

Reinsurance documents can contain sensitive commercial and financial information. Reva's default path runs without a hosted model key and without making AI a dependency for the core workflow.

Optional model providers can help with chat, summaries, and extraction proposals when enabled. The deterministic workflow remains the source of truth.

## One-sentence demo pitch

Reva turns messy reinsurance submissions into source-cited, reconciled, analyst-approved data that can be exported with confidence.
