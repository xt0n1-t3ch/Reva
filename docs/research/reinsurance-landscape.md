# Reinsurance document-intelligence landscape

Reva is grounded in a specific operational problem: turning reinsurance bordereaux, technical accounts, statements of account, and related submission files into structured, reconciled, reviewable data that analysts can trust.

This page summarizes the domain, document types, canonical fields, reconciliation breaks, standards, and competitive UX patterns that shape the product.

## 1. Domain model

Reinsurance transfers risk from an insurer or cedent to one or more reinsurers. A broker, managing agent, MGA, coverholder, or service company may sit in the reporting path. In delegated authority and bordereaux-heavy operations, data frequently arrives as spreadsheets, PDFs, emails, scans, or semi-structured statements that must be normalized before downstream booking, reporting, audit, or analytics.

Key concepts:

| Term | Meaning for document intelligence |
|:---|:---|
| Cedent / reinsured | The party ceding risk and premium. Often appears as company name, cedant code, insurer, or reinsured. |
| Reinsurer | The accepting carrier or market participant. May be shown by share, layer, participant, or reinsurer reference. |
| Broker | Intermediary that can send cover notes, accounts, bordereaux, or settlement instructions. |
| Treaty | A contract covering a book or class of business. Reporting is periodic and often table-heavy. |
| Facultative | Individually underwritten risk. Documents are more deal-specific and can include slip-like terms. |
| Proportional | Premium, losses, and commission follow a share such as quota share or surplus. |
| Non-proportional / XL | Recoveries depend on attachment, limit, and loss layer terms. |
| Bordereau | A periodic row-level report of policies, risks, premiums, claims, or movements. |
| Technical account | A statement of premium, commission, claims, taxes, brokerage, and balance due. |

Lloyd's describes Delegated Authority as a major channel where managing agents delegate authority to coverholders and service companies; this is exactly the environment where recurring bordereaux and reporting schedules create document-normalization work.

## 2. Core document types

| Document type | What it carries | Reva implications |
|:---|:---|:---|
| Premium bordereau | Policy/risk rows, gross written premium, ceded premium, commission, taxes, cession share, effective dates, line of business, territory, currency. | Needs header mapping, money/date/currency normalization, and table totals for reconciliation. |
| Claims / loss bordereau | Claim rows, paid/outstanding/incurred amounts, claim status, date of loss, reported date, reserves, recoveries, linked policy or UMR. | Needs status-code normalization and links back to premium/risk references. |
| Risk bordereau | Underlying risk details for delegated authority reporting: insured, location, limits, class, period, coverholder references. | Needs canonical field mapping and downstream template export. |
| Statement of account / technical account | Period summary of premium, commission, claims, brokerage, taxes, balance due, settlement currency. | Must reconcile stated totals and explain balance deltas. |
| Broker cover note | Narrative or semi-structured headline figures attached to a bordereau or account. | A common source of **Detected** stated figures to compare against computed table totals. |
| Slip / placing slip | Coverage terms, parties, limits, shares, dates, conditions, and wording references. | Useful context but less row-oriented than bordereaux. |
| Treaty wording | Legal contract terms, definitions, limits, exclusions, clauses, and obligations. | Longer unstructured prose; important for future rule extraction, not the core 1.3.0 path. |
| Claim notice / cash call / debit-credit note | Event-driven loss or settlement instructions with urgent financial impact. | Needs fast money extraction and explicit provenance. |

## 3. Canonical fields

Reva maps sender-specific headers into canonical reinsurance concepts. Common aliases are expected: `GWP`, `Gross Written Premium`, `Premium_Gross`, and `Written Premium` may all target premium; `Cedant Co`, `Cedent`, and `Reinsured` may target the same party concept.

### Premium and risk fields

| Canonical concept | Typical aliases and source shapes |
|:---|:---|
| Policy / certificate reference | Policy No, Cert No, Certificate, Contract Ref. |
| UMR / contract reference | UMR, Unique Market Reference, Treaty No, Contract ID. |
| Insured / cedent / reinsured | Insured, Cedent, Cedant Co, Reinsured, Client. |
| Line of business | LOB, Class, Risk Class, Product, Section. |
| Territory / location | Country, State, Risk Location, Territory. |
| Inception and expiry | Effective Date, Start Date, End Date, Expiry. |
| Gross written premium | GWP, Gross Premium, Written Premium. |
| Commission / brokerage | Commission, Ceding Commission, Brokerage, Acquisition Cost. |
| Taxes and fees | Tax, Levy, Fee, Stamp, IPT. |
| Cession percentage | Share, Ceded %, Participation, Quota Share. |
| Ceded / net premium | Ceded Premium, Net Premium, Reinsurance Premium. |
| Currency | ISO 4217 code or symbol; normalization should preserve the original. |

### Claims fields

| Canonical concept | Typical aliases and source shapes |
|:---|:---|
| Claim reference | Claim No, Loss Ref, Claim ID. |
| Linked policy / UMR | Policy Ref, Contract Ref, UMR. |
| Date of loss / reported date | DOL, Loss Date, Reported, Notification Date. |
| Status | Open, Closed, Reopened, Settled, Outstanding. |
| Paid | Paid Loss, Loss Paid, Indemnity Paid, Expense Paid. |
| Outstanding / reserve | Reserve, Outstanding Loss, Case Reserve. |
| Incurred | Paid + Outstanding, Total Incurred. |
| Recoveries | Recovery, Salvage, Subrogation. |
| Reinsurance share | RI Share, Reinsurer Share, Ceded Loss. |

### Account fields

| Canonical concept | Typical aliases and source shapes |
|:---|:---|
| Account period | Month, Quarter, Accounting Period, Statement Period. |
| Premium | Premium Due, Written Premium, Ceded Premium. |
| Claims | Claims Paid, Losses, Paid Losses. |
| Commission | Ceding Commission, Brokerage, Commission. |
| Balance due | Net Due, Amount Due, Settlement Balance. |
| Currency | Settlement Currency, Original Currency, ISO code. |

## 4. Common reconciliation breaks

Reconciliation is where document intelligence becomes operationally useful. Reva models these breaks as field-level exceptions with **Detected** stated value, **Expected** computed value, and an agreement score.

| Break | Example | Product behavior |
|:---|:---|:---|
| Stated premium differs from line-item total | Cover note states `USD 4,400,000`; attached bordereau sums to another amount. | Compute the line-item total and raise a premium exception if outside tolerance. |
| Commission basis mismatch | Commission is calculated on gross premium in one file and ceded premium in another. | Preserve both detected and expected values with citations so the analyst can decide. |
| Cession-rate mismatch | Header states `40%`; rows carry `37.5%` or mixed shares. | Raise a rate exception and point to the conflicting source rows/fields. |
| Currency conflict | Account summary says USD while rows contain mixed USD/EUR/GBP. | Flag currency inconsistency before export. |
| Period misalignment | Statement period is Q1 but line items include dates outside Q1. | Surface a review exception; do not silently drop rows. |
| Claims status normalization | `Re-open`, `Reopened`, and `R` appear in the same sender file. | Map to canonical status while preserving source value. |
| Missing references | Claims bordereau has claim refs without matching policy/UMR. | Keep the row reviewable and mark the missing join key. |
| Duplicate rows | Same risk or claim appears twice after workbook/email attachment merges. | Mark as suspicious rather than auto-approve. |

Configurable money tolerance matters because production bordereaux can include rounding, FX, tax, or allocation differences. Exact-match-only reconciliation creates noise.

## 5. Standards and reporting references

| Reference | Why it matters to Reva |
|:---|:---|
| Lloyd's Coverholder Reporting Standards (CRS) | Defines common delegated authority reporting shapes for risk, premium, and claims bordereaux. Reva's Lloyd's CRS template is an export target, not the only internal model. |
| Lloyd's Delegated Authority resources | Ground the coverholder/service-company environment where recurring bordereaux and reporting schedules appear. See [Lloyd's Delegated Authority](https://www.lloyds.com/market-resources/delegated-authorities). |
| Lloyd's Delegated Data Manager / market reporting tools | Important downstream context: data quality and validation failures affect reporting acceptance. |
| ACORD standards | ACORD provides insurance data standards and is relevant to reinsurance and London Market exchange patterns. See [ACORD](https://www.acord.org/). |
| ACORD Solutions Group ADEPT | Relevant as a data-exchange and translation pattern for standardized insurance messaging. |
| Market Reform Contract / iMRC | Relevant to placing and contract data that may later feed extraction or validation. |
| ISO 4217 | Currency normalization should use standard currency codes while preserving source evidence. |

## 6. Competitive and UX landscape

Reva borrows the best interaction pattern from intelligent document processing: a source document beside extracted fields, with human correction and traceability. The domain-specific differentiator is reinsurance reconciliation and sender-learned schema mapping.

| Product / category | Observable pattern | Reva positioning |
|:---|:---|:---|
| Rossum | AI document processing emphasizes ingest, capture, validation, transformation, user feedback, and audit trails for transactional workflows. See [Rossum](https://rossum.ai/). | Reva applies the same trust loop to reinsurance bordereaux with local-first runtime and field-level source citations. |
| Send | Send includes bordereaux ingestion, validation against contract rules, standardization, exceptions, and delegated/reinsurance underwriting workflows. See [Send Bordereaux Ingestion](https://send.technology/platform/bordereaux-ingestion/) and [Send Reinsurance Underwriting](https://send.technology/products/reinsurance-underwriting/). | Reva focuses on offline document ingestion/reconciliation rather than a broader underwriting platform. |
| Sapiens | Insurance platform vendor with reinsurance administration and core-system context. See [Sapiens](https://sapiens.com/). | Reva is not a core administration suite; it prepares trusted data for review/export. |
| Quantiphi / Dociphi-style document AI | Insurance-oriented document AI and extraction workflows. See [Quantiphi](https://www.quantiphi.com/). | Reva keeps the workflow local, transparent, and source-cited. |
| Generic IDP / OCR tools | Extract fields and tables from documents, often with confidence and review queues. | Reva adds reinsurance canonical fields, sender-specific mapping memory, and computed reconciliation exceptions. |

The practical UX pattern is consistent: ingest any-format files, map fields into a canonical model, validate with rules, route exceptions to humans, show source evidence, and export clean data. Reva implements that loop in a single self-contained native desktop application that runs fully on-device.

## 7. Product implications

Reva 1.3.0 should prioritize:

1. **Schema mapping transparency** — analysts must see how each source header became a canonical field.
2. **Correction learning** — a correction should reduce future work for the same sender/domain.
3. **Source-cited review** — every field should carry provenance, and geometry-backed citations should highlight the original source region.
4. **Computed reconciliation** — exceptions should be based on actual document values, not static demo data.
5. **Export flexibility** — downstream consumers need CSV, Excel, JSON, and saved templates.
6. **Local deployability** — one executable should run the full product without cloud setup.

## 8. What Reva intentionally does not claim

- No live mailbox sync is shipped; inbound email support is file-based `.eml` and `.msg`.
- No cloud OCR or cloud LLM is required for the default workflow.
- No Python runtime is required unless the optional Docling path is enabled.
- No straight-through-processing percentage is claimed without live operational measurement.
