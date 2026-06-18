# Context and glossary

The shared language of Reva. Each term is defined once here so names stay consistent across the code, the docs, and conversation. Two sections: the reinsurance domain, then the architecture.

---

## Reinsurance domain

**Reinsurance.** Insurance bought by an insurance company. The original insurer (the cedent) transfers part of its risk to a reinsurer, usually arranged by a broker.

**Cedent.** The insurer that cedes risk to a reinsurer. One of Reva's canonical fields.

**Reinsurer.** The party assuming the ceded risk. A canonical field.

**Broker.** The intermediary that places the reinsurance between cedent and reinsurer. A canonical field.

**Cession.** The act of transferring risk from the cedent to the reinsurer, and the share that is transferred. Reva extracts the **Cession %** — the proportion ceded.

**Retention.** The share of risk the cedent keeps rather than ceding. The complement of cession. A canonical field.

**Bordereau** (plural **bordereaux**). A periodic, line-by-line schedule sent from cedent to reinsurer listing the individual risks, premiums, and claims under a treaty. The core document Reva ingests. A *premium bordereau* lists premiums; a *claims bordereau* lists losses.

**Technical account.** A statement summarizing the financial movements (premiums, commissions, claims, balance) under a reinsurance contract for a period. One of the document types Reva classifies.

**Statement of account.** A document stating the net balance owed between the parties for a period. Another classified document type.

**Line of business.** The class of insurance a risk belongs to (property, casualty, marine, and so on). A canonical field, compared during reconciliation by token agreement.

**Premium / Claims / Commission.** The money fields. Premium is what is paid for cover; claims are losses paid; commission is the intermediary's fee. Each is a canonical field and is reconciled against the sum of its line-item column.

**Contract reference.** The identifier of the treaty or contract the document relates to. A canonical field.

**Period.** The time span the document covers (for example a quarter). A canonical field.

**Currency / Limit.** Currency is the money's denomination; Limit is the maximum cover under the contract. Both are canonical fields.

**Treaty.** A reinsurance contract covering a defined book of business, as opposed to a single risk.

**Lloyd's CRS.** A downstream reporting shape (Core Reporting Standard) used in the Lloyd's market. Reva ships an export template oriented to it.

---

## Architecture and Reva-specific terms

**Canonical field.** One of Reva's normalized reinsurance fields, defined once in `ReinsuranceFieldNames.Canonical`: Cedent, Broker, Reinsurer, Contract Reference, Line of Business, Period, Currency, Premium, Claims, Commission, Cession %, Retention, Limit. Schema mapping turns each sender's headers into these.

**Schema mapping.** Translating a sender's column header into a canonical field. Precedence: a *learned* per-sender rule, then a static *alias*, then a bounded *fuzzy* match, otherwise *unmapped*.

**Learned mapping.** A per-sender (or per-email-domain) rule saved when an analyst corrects a mapping. It takes precedence on the next document from that sender, so the system adapts without retraining.

**Citation.** The link from an extracted value back to where it was read — a page plus a normalized region (bounding box and polygon), captured by OCR or the PDF renderer. Citations drive the source highlight in the review view.

**Source span.** The stored unit of citation geometry: a page, a normalized bounding box, an optional polygon, OCR confidence, and the text. Persisted as `DocumentSourceSpanRecord`.

**Provenance.** The record of how a field value was obtained — the method and its citations. Always present, even when geometry is unavailable.

**Confidence.** A computed score in `[0,1]` reflecting how a value was located. Honest, never a flattering constant. Rendered as Low / Medium / High by the two configured thresholds.

**Reviewed.** The human signal set when an analyst corrects or accepts a field. Kept separate from machine confidence — Reva never inflates confidence to disguise an edit.

**Reconciliation.** Comparing a stated headline figure (**Detected**) against the value computed from the line items (**Expected**), within a configurable money tolerance. A disagreement becomes a field-level exception with an agreement score.

**Detected vs Expected.** In a reconciliation finding, *Detected* is the value the document stated and *Expected* is the value Reva computed from the line items.

**Exception.** A finding raised during extraction or reconciliation — a missing field, an unclassified document, or a reconciliation break. Modeled as `ExtractionIssue`.

**Three-tier processing.** Reva's layered model: tier one is deterministic and always on (parsers, offline OCR, rule extraction, reconciliation); tier two is the optional local VLM that proposes extra fields; tier three is the optional Docling parser for hard layouts. Each tier degrades gracefully.

**Deterministic extraction.** Rule-based field extraction that runs with no model and no network — the always-on baseline.

**VLM (vision-language model).** A model that reads page images and reasons about them. In Reva it proposes additional fields during tier-two extraction; proposals are merged conservatively.

**Merge (conservative).** The rule that combines a VLM proposal with the deterministic result. A proposal is accepted only with a citation and sufficient confidence, and it never overwrites an already-populated money field.

**Model registry.** The component that lists the curated model menu, marks which are installed in Ollama, appends extras, and persists the active choice. The reason the model is configurable, not hardcoded.

**Active model.** The model the user selected in Settings, used for both VLM extraction and the copilot. Persisted under `%LOCALAPPDATA%\Reva`.

**Copilot / agent.** The chat assistant. It answers questions over the real workflow and can *act* on the app through action tools.

**Action tool.** A copilot tool that changes app state or moves the UI — open a document, correct a field, navigate, export. It does the work and publishes an `AppAction`.

**AppAction / action bus.** The typed message (`AppAction`) and the in-process channel (`IAppActionBus`) the copilot uses to drive the UI. The navigation service subscribes and performs the real action, so chat and UI stay in lockstep.

**RevaClient.** The single façade (`IRevaClient`) view models use to reach the backend. It opens a DI scope per call and forwards to the workflow, exporter, settings store, and model registry.

**Document workflow.** The pipeline spine (`IDocumentWorkflow`): ingest, parse, extract, merge, map, reconcile, persist, review, export.

**MVVM / view model / view.** The UI pattern. A *view model* holds bindable state and commands and no UI types; a *view* is the Avalonia XAML bound to it. Resolved by convention through the `ViewLocator`.

**Compiled bindings.** Avalonia bindings checked against a view's `x:DataType` at compile time, so binding errors break the build instead of failing silently.

**Self-contained executable.** A .NET publish that bundles the runtime, so `Reva.exe` runs with nothing pre-installed.

**Workspace.** The per-user data location, `%LOCALAPPDATA%\Reva`: the SQLite database and the uploads folder.

**Ollama.** The local model server Reva talks to over its OpenAI-compatible endpoint. Optional — when absent, the deterministic tier and all non-AI features still work.

**Docling.** The optional external document-layout parser (tier three), off by default, enabled by configuration.
