# Reinsurance document-intelligence landscape

Reva is grounded in a specific operational problem: turning reinsurance bordereaux, technical accounts, statements of account, and related submission files into structured, reconciled, reviewable data analysts can trust.

## 1. Domain model

Reinsurance transfers risk from an insurer or cedent to one or more reinsurers. A broker, managing agent, MGA, coverholder, or service company may sit in the reporting path. In delegated authority and bordereaux-heavy operations, data arrives as spreadsheets, PDFs, emails, scans, or semi-structured statements that must be normalized before booking, reporting, audit, or analytics.

| Term | Meaning for document intelligence |
|:---|:---|
| Cedent / reinsured | Party ceding risk and premium. Often appears as company name, cedant code, insurer, or reinsured. |
| Reinsurer | Accepting carrier or market participant. May be shown by share, layer, participant, or reference. |
| Broker | Intermediary sending cover notes, accounts, bordereaux, or settlement instructions. |
| Treaty | Contract covering a book or class of business. Reporting is periodic and table-heavy. |
| Facultative | Individually underwritten risk. Documents are more deal-specific and slip-like. |
| Bordereau | Periodic row-level report of policies, risks, premiums, claims, or movements. |
| Technical account | Statement of premium, commission, claims, taxes, brokerage, and balance due. |

## 2. Core document types

| Document type | What it carries | Reva implication |
|:---|:---|:---|
| Premium bordereau | Policy/risk rows, gross written premium, ceded premium, commission, taxes, share, dates, line, territory, currency. | Header mapping, money/date/currency normalization, table totals. |
| Claims / loss bordereau | Claim rows, paid/outstanding/incurred amounts, status, dates, reserves, recoveries, policy or UMR. | Status normalization and links back to premium/risk references. |
| Risk bordereau | Insured, location, limits, class, period, coverholder references. | Canonical mapping and downstream template export. |
| Statement of account / technical account | Premium, commission, claims, brokerage, taxes, balance due, settlement currency. | Reconcile stated totals and explain balance deltas. |
| Broker cover note | Narrative or semi-structured headline figures attached to a bordereau or account. | Common source of Detected values. |
| Claim notice / cash call / debit-credit note | Event-driven loss or settlement instruction. | Fast money extraction and explicit provenance. |

## 3. Canonical fields

Reva maps sender-specific headers into canonical reinsurance concepts. `GWP`, `Gross Written Premium`, and `Written Premium` may all target Premium. `Cedant Co`, `Cedent`, and `Reinsured` may target the same party concept.

| Canonical concept | Typical aliases and source shapes |
|:---|:---|
| Contract reference | UMR, Treaty No, Contract ID, Policy No. |
| Cedent / reinsured | Cedent, Cedant Co, Reinsured, Client. |
| Broker | Broker, Intermediary, Producing Broker. |
| Reinsurer | Reinsurer, Market, Participant. |
| Line of business | LOB, Class, Risk Class, Product. |
| Period | Month, Quarter, Accounting Period, Start/End. |
| Premium | GWP, Gross Premium, Written Premium, Ceded Premium. |
| Claims | Paid Loss, Losses, Incurred, Outstanding. |
| Commission | Ceding Commission, Brokerage, Acquisition Cost. |
| Cession % | Share, Ceded %, Participation, Quota Share. |
| Retention | Retained %, Net Share, Retention. |
| Limit | Limit, Sum Insured, Cover Limit. |
| Currency | ISO code, symbol, settlement currency. |

## 4. Common reconciliation breaks

| Break | Example | Product behavior |
|:---|:---|:---|
| Stated premium differs from rows | Cover note states one value; attached bordereau sums to another. | Raise a Premium exception with Detected vs Expected. |
| Commission basis mismatch | Commission is calculated on gross premium in one file and ceded premium in another. | Preserve both values with citations. |
| Cession-rate mismatch | Header states 40%; rows carry mixed shares. | Flag the rate break and point to source rows. |
| Currency conflict | Account summary says USD while rows contain mixed currencies. | Flag before export. |
| Period misalignment | Statement period is Q1 but rows include dates outside Q1. | Surface a review exception. |
| Duplicate rows | Same risk or claim appears twice after attachment merges. | Mark as suspicious, not auto-approved. |

## 5. Standards and reporting references

| Reference | Why it matters |
|:---|:---|
| Lloyd's Coverholder Reporting Standards | Common delegated authority reporting shapes for risk, premium, and claims bordereaux. |
| Lloyd's Delegated Authority resources | Context for coverholder and service-company reporting workflows. |
| ACORD standards | Insurance data standards relevant to exchange patterns. |
| ISO 4217 | Currency normalization should use standard codes while preserving source evidence. |

## 6. Competitive and UX landscape

The consistent intelligent-document-processing pattern is: ingest files, map fields into a canonical model, validate with rules, route exceptions to humans, show source evidence, and export clean data. Reva applies that pattern to reinsurance documents with local-first defaults, source citations, learned sender mappings, and reconciliation.

| Product / category | Observable pattern | Reva positioning |
|:---|:---|:---|
| Rossum | Capture, validation, user feedback, audit trails. | Reva applies the trust loop to reinsurance fields and totals. |
| Send | Bordereaux ingestion, validation, standardization, exceptions. | Reva focuses on document review and export rather than a broader underwriting suite. |
| Sapiens | Reinsurance administration and core-system context. | Reva prepares trusted data for downstream systems. |
| Generic IDP / OCR tools | Extract fields and tables with confidence and queues. | Reva adds canonical reinsurance fields, mapping memory, and computed exceptions. |

## 7. Product implications

1. Schema mapping must be visible.
2. Corrections should reduce future work for the same sender or domain.
3. Every field needs provenance.
4. Reconciliation must use actual document values.
5. Export needs CSV, Excel, JSON, and saved templates.
6. The web app should demo without cloud setup.

## 8. What Reva intentionally does not claim

- No live mailbox sync is shipped; inbound email support is file-based `.eml` and `.msg`.
- No cloud OCR or cloud LLM is required for the default workflow.
- No Python runtime is required unless the optional Docling path is enabled.
- No straight-through-processing percentage is claimed without live operational measurement.
