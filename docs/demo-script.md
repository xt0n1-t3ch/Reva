# Demo script

Use this script for interviews, portfolio walkthroughs, and product demos.

## Setup

Run the API:

```powershell
dotnet run --project src/Reva.Web/Reva.Web.csproj -- --no-open
```

Run the web app:

```powershell
cd web
$env:NEXT_PUBLIC_API_BASE_URL = "http://localhost:5158"
pnpm dev
```

Open `http://localhost:3000`.

## Two-minute pitch

"Reva is a local-first document-intelligence workspace for reinsurance operations. It ingests the files brokers and cedents send every day, extracts the canonical fields, cites where each value came from, reconciles control totals, lets an analyst approve or correct exceptions, and exports clean data. The key design choice is trust: the default workflow works without a hosted model, and optional AI only assists through controlled seams."

## Walkthrough

### 1. Workspace

Open **Workspace**.

Say: "This is the intake cockpit. It shows document status, confidence, exceptions, processing activity, and the work queue. The analyst can upload a file or load demo scenarios."

Point out:

- document count
- pending review count
- exception indicators
- upload area
- processing stream or queue

### 2. Review

Open **Review** and select a document with exceptions.

Say: "This is where trust is earned. Reva does not just show a value; it shows confidence, provenance, and reconciliation context. If geometry exists, it can link back to the exact source region."

Point out:

- source text or document view
- extracted fields
- confidence labels
- reconciliation exceptions
- approve/request changes/reject controls

### 3. Mappings

Open **Mappings**.

Say: "Different senders name the same business fields differently. Reva can learn sender-specific overrides, then fall back to static aliases and bounded fuzzy matching."

Point out:

- sender grouping
- source header
- canonical field
- confidence and origin

### 4. Assistant

Open **Assistant**.

Ask one grounded question:

```text
Which documents have reconciliation exceptions?
```

Say: "The assistant is not a generic chatbot. It is grounded in processed documents and backend tools, so it can explain document state instead of guessing."

### 5. Knowledge

Open **Knowledge**.

Say: "The Knowledge Hub keeps domain notes inside the same analyst workspace. It gives the product and assistant shared context about reinsurance standards and edge cases."

### 6. Export

Open **Export**.

Say: "After review, data can leave in market-friendly formats. Templates keep exports consistent for downstream teams."

Point out:

- CSV, Excel, and JSON options
- reusable templates
- document-level downloads

### 7. Showcase tour

Click **Showcase**.

Say: "The built-in tour makes the product interview-ready. It walks through the capabilities with live scenarios instead of a slide deck."

## Strong closing line

"The product is not trying to be a magic parser. It is a review system for financial documents: read the file, show the evidence, flag the breaks, let a human decide, and export clean data."
