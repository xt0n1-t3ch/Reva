# Reinsurance document intelligence — domain & market research

Research that grounds the product and UI. Our app sits in the **bordereaux ingestion &
reconciliation** camp (alongside Brisc, Vellum, Mi Analyst, Quantiphi, Nomad), not the
placement/pricing camp (Supercede, Whitespace, hyperexponential).

## 1. Domain in plain English

Reinsurance is insurance for insurers. A primary insurer (the **cedent**) offloads part of
its risk to a **reinsurer**, usually through a **broker**. The slice handed over — risk plus
the matching premium — is a **cession**.

- **Treaty** vs **facultative**: a treaty covers a whole class of business automatically;
  facultative is negotiated one risk at a time.
- **Proportional** (quota share / surplus): reinsurer shares the same % of premium and losses
  and pays the cedent a ceding commission. **Non-proportional** (excess of loss / XL): the
  reinsurer only pays above an attachment point, up to a limit.
- **Delegated authority (DA)**: a carrier lets an MGA/coverholder write business and report
  back via **bordereaux** — today the highest-volume source of document processing.

## 2. Document types (and where the pain is)

| Document | What it is | Pain |
|---|---|---|
| Premium bordereau | Spreadsheet of every policy ceded in the period (premium, commission, ceded amount) | Highest volume; every sender's own format |
| Claims / loss bordereau | Claims paid/outstanding in the period + reinsurance share | Highest pain; inconsistent status codes, links to premium BDX |
| Risk bordereau | List of underlying risks bound (Lloyd's DA reporting) | High volume in DA |
| Statement of account (technical account) | Periodic reconciliation of premium/claims/commission; balance owed | Must tie to BDX |
| Slip / placing slip | Summary of terms used to arrange cover | Semi-structured |
| Treaty wording | The full legal contract | Long unstructured prose |
| Loss run | Historical claims listing for an account | Format-variable |
| Claim notice / cash call / debit-credit note | Loss notification / urgent collection / accounting note | Time-critical money flows |

**The acute pain is bordereaux** (premium + claims): many senders, each with their own
columns and code sets, all needing to be forced into one schema before anything can be
reconciled or booked.

## 3. Key fields to extract

- **Premium bordereau**: policy/cert ref, UMR, insured, territory, line of business, inception
  & expiry, sum insured/limit, gross written premium, commission/brokerage, taxes, cession %,
  ceded/net premium, currency (ISO 4217), period, coverholder ref.
- **Claims bordereau**: claim ref + linked policy/UMR, date of loss, date reported, peril/cat
  code, status (Open/Closed/Re-open), paid/outstanding/incurred (ITD vs period), reserves,
  reinsurance share, recoveries, currency.
- **Statement of account**: account period, premium, commission, claims, profit commission,
  brokerage, balance due, currency, offset.

## 4. Must-have features (ranked)

1. Ingest any format (Excel/CSV/PDF native+scanned/email) — table stakes.
2. OCR + field extraction.
3. **Schema mapping to a canonical layout** — translate each sender's headers ("GWP" /
   "Gross Written Premium" / "Premium_Gross") to one canonical field, normalize dates,
   currencies, codes. *Rivals admit this step usually stays manual — automating it well is the
   differentiator.*
4. Validation & reconciliation — stated vs computed totals; premium↔claims via shared UMR.
5. Exception / error queue — auto-pass most rows, humans handle only flagged ones.
6. Audit trail + **page/cell-level citations** — every value traceable to its source.
7. Multi-currency normalization (ISO 4217).
8. Customizable export templates — to downstream systems and **Lloyd's CRS 5.2**.
9. Role-based review/approval + bulk processing + late-file alerts.

## 5. What real products do

Dominant shape across the field: **drag-and-drop any-format ingest → auto header/column
mapping to a canonical model → rule-based validation in a filterable exceptions grid → human
reviews exceptions only → page/cell-level citations → one-click export to downstream systems
and Lloyd's 5.2.**

- **Brisc / Mi Analyst / Quantiphi Dociphi / Vellum / AI Insurance** — bordereaux ingestion:
  any format in, auto-map + normalize, validate, exceptions with audit trail, export to
  IMS/Insurity/SICS/Sequel Re; Vellum supports Lloyd's 5.2 in/out.
- **Nomad Data Doc Chat** — captures your mapping/reconciliation rules, answers with page-level
  citations.
- **DataFlowMapper** — focuses on the mapping layer; validation errors in a filterable grid.
- **Supercede / Whitespace / hyperexponential / Cytora / Send / Artificial** — placement,
  e-placing, pricing, submission intake. Adjacent; not our lane.

## 6. Standards worth knowing

- **Lloyd's Coverholder Reporting Standards v5.2** — the core risk/premium/claims fields
  coverholders must report.
- **DDM (Delegated Data Manager) / DA SATS** — Lloyd's central bordereaux collection &
  validation; rejects submissions that fail the standards.
- **LIMOSS** — hosts the standards, market glossary, and premium/claims bordereaux SOPs.
- **Market Reform Contract (MRC) / iMRC** — London standard placing document, going
  machine-readable.
- **ACORD** — global insurance data standard; GRLC CRP / Core Data Record / ADEPT for treaty
  data exchange.

## 7. Our feature set (demo-first)

**Wow path:** drag-drop any-format ingest · extraction with the document shown side-by-side
and **source citations** · **auto schema-mapping to canonical fields** with confidence and a
one-click correction that learns per sender · validation rules + filterable exceptions grid ·
premium↔claims reconciliation via UMR · one-click export to **Lloyd's CRS 5.2** + canonical
CSV/JSON · multi-currency · audit trail.

**Later:** role-based approval, bulk processing, late-file alerts, ACORD/ADEPT ingest-at-source,
treaty-wording extraction, learned per-sender templates, downstream connectors, agentic
auto-resolution of common exceptions.

**Where we out-class the field in the demo:** (a) the mapping step rivals leave manual, and
(b) visible page/cell-level citations that make the data feel trustworthy at a glance.
