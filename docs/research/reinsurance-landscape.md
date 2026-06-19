# Reinsurance landscape

This page explains the business context behind Reva in practical terms.

## The roles

| Role | Plain-English description |
|:---|:---|
| Cedent | The insurer or company transferring part of its risk. |
| Broker | The intermediary that structures, places, or administers the transaction. |
| Reinsurer | The party accepting part of the risk in exchange for premium. |
| Analyst | The person who reviews submissions, checks totals, resolves exceptions, and prepares data for downstream systems. |

## The documents

Reinsurance operations receive many document types. Reva focuses on the ones that carry operational and financial values.

| Document | What it usually contains |
|:---|:---|
| Bordereau | Line-item reporting for premium, claims, risks, periods, and exposure. |
| Statement of Account | Accounting totals, balances, commissions, and settlement values. |
| Treaty | Contract structure and reinsurance terms. |
| Facultative slip | A placement document for a specific risk. |
| Loss run | Claims history and loss detail. |
| Endorsement | A contract change or amendment. |
| Claim notice | Notification and details of a claim event. |

## The data problem

The same business meaning appears under many names. For example, premium may arrive as:

- `Premium`
- `Gross Premium`
- `Written Premium`
- `Premium Amount`
- `GWP`

A simple parser can read the text. A useful reinsurance workflow must normalize it, cite it, and know whether it reconciles.

## Why reconciliation matters

Bordereaux and statements often include control totals. The line items should add up to those totals within an agreed tolerance.

When the detected total and expected total do not match, the analyst needs to know:

- which value was stated
- which value was computed
- how large the break is
- which document or line contributed to it
- whether it is severe enough to block approval

Reva surfaces those breaks as review exceptions.

## Why citations matter

Financial review needs evidence. If a system extracts `1,250,000` as premium, the reviewer must be able to ask where it came from.

Source citations make the answer concrete. They reduce blind trust, speed up correction, and create a better audit trail.

## Why local-first matters

Reinsurance files can contain sensitive commercial data. A local-first default lets the core workflow run without relying on a hosted model provider. That is important for demos, privacy-conscious teams, and environments where external services need explicit approval.

## Competitive angle

Many tools can OCR or parse documents. Reva's stronger positioning is the combined workflow:

1. accept real file variety
2. read scans and digital documents
3. normalize reinsurance fields
4. cite extracted values
5. reconcile financial totals
6. learn sender-specific mappings
7. let analysts review decisions
8. export clean operational data
9. answer questions through grounded backend tools

That full loop is what makes it interview-ready as a product, not just as a technical experiment.
