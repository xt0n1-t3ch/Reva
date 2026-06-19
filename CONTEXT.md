# Context and glossary

The shared language of Reva. Each term is defined once here so names stay consistent across code, docs, and conversation.

---

## Reinsurance domain

**Reinsurance.** Insurance bought by an insurance company. The original insurer, the cedent, transfers part of its risk to a reinsurer, usually arranged by a broker.

**Cedent.** The insurer that cedes risk to a reinsurer. One of Reva's canonical fields.

**Reinsurer.** The party assuming the ceded risk. A canonical field.

**Broker.** The intermediary that places the reinsurance between cedent and reinsurer. A canonical field.

**Cession.** The act of transferring risk from the cedent to the reinsurer, and the share transferred. Reva extracts **Cession %**.

**Retention.** The share of risk the cedent keeps. The complement of cession. A canonical field.

**Bordereau** (plural **bordereaux**). A periodic, line-by-line schedule sent from cedent to reinsurer listing risks, premiums, and claims under a treaty. A *premium bordereau* lists premiums; a *claims bordereau* lists losses.

**Technical account.** A statement summarizing premiums, commissions, claims, and balance under a reinsurance contract for a period.

**Statement of account.** A document stating the net balance owed between parties for a period.

**Line of business.** The class of insurance a risk belongs to, such as property, casualty, or marine. A canonical field.

**Premium / Claims / Commission.** The money fields. Premium is paid for cover, claims are losses paid, and commission is the intermediary fee. Each is reconciled against line-item columns when present.

**Contract reference.** The treaty or contract identifier. A canonical field.

**Period.** The time span the document covers. A canonical field.

**Currency / Limit.** Currency is the money denomination. Limit is the maximum cover under the contract. Both are canonical fields.

**Treaty.** A reinsurance contract covering a defined book of business, rather than a single risk.

**Lloyd's CRS.** A downstream reporting shape used in the Lloyd's market. Reva ships an export template oriented to it.

---

## Product architecture

**Reva web app.** The single product surface: `web/` for the Next.js frontend and `src/Reva.Web` for the ASP.NET Core API.

**Next.js frontend.** The React 19 client in `web/`. It owns the app shell, upload, review, mappings, export, Knowledge Hub, settings, and copilot screens.

**API boundary.** The contract between frontend and backend. The canonical client is `web/lib/api/client.ts`; backend endpoints live in `src/Reva.Web`.

**Minimal API endpoint.** A small ASP.NET Core endpoint mapped by feature, such as documents, settings, Knowledge Hub, agent chat, or processing streams.

**Vercel AI SDK chat.** The frontend chat loop. It sends turns, renders streaming parts, and handles tool-call state through the same class of protocol used by modern agentic products.

**OpenAI-compatible streaming.** Provider-neutral streaming over the common chat-completions shape. Reva can point that seam at local Ollama, compatible hosted endpoints, or HuggingFace-backed providers when configured.

**Canonical field.** One of Reva's normalized reinsurance fields: Cedent, Broker, Reinsurer, Contract Reference, Line of Business, Period, Currency, Premium, Claims, Commission, Cession %, Retention, Limit.

**Schema mapping.** Translating a sender's column header into a canonical field. Precedence: learned per-sender rule, static alias, bounded fuzzy match, otherwise unmapped.

**Learned mapping.** A per-sender or per-email-domain rule saved when an analyst corrects a mapping. It takes precedence on the next document from that sender.

**Citation.** The link from an extracted value back to where it was read: page plus normalized region when geometry is available.

**Source span.** The stored citation unit: page, normalized box, optional polygon, OCR confidence, and text.

**Provenance.** The method and source evidence behind a field value. Always present; citations can be empty only when geometry is unavailable.

**Confidence.** A computed score in `[0,1]` reflecting how a value was located. It is never inflated to hide uncertainty.

**Reviewed.** The human signal set when an analyst accepts or corrects a field. Kept separate from machine confidence.

**Reconciliation.** Comparing a stated headline figure against the value computed from line items within a configured tolerance.

**Detected vs Expected.** In a reconciliation finding, *Detected* is the value the document stated and *Expected* is the value Reva computed.

**Exception.** A missing field, classification miss, or reconciliation break that needs review.

**Document workflow.** The pipeline spine: ingest, parse, OCR when needed, classify, extract, map, reconcile, persist, review, export.

**Deterministic extraction.** Rule-based extraction that runs with no model and no network. This is the source of truth.

**Model assist.** Optional provider-backed help for extraction or chat. It can propose and explain; deterministic results and analyst review remain authoritative.

**VLM.** A vision-language model that can read rendered page images and propose fields during model-assisted extraction.

**Conservative merge.** The rule for combining model proposals with deterministic values: require provenance, respect confidence, and avoid overwriting validated money fields.

**Provider registry.** The settings-driven list of reachable model providers and model names.

**Active provider.** The provider/model selected in Settings for agent chat or optional extraction assist.

**Agentic copilot.** The chat assistant that can answer questions and call tools against real backend workflows.

**Action tool.** A tool the agent can call, such as list documents, open a record, correct a field, set review state, export, or query Knowledge Hub.

**Tool loop.** The request cycle where the model proposes tool calls, backend code executes them, and the stream returns tool results plus final text.

**Processing stream.** A server-sent event stream that reports document stages and scanned lines as the pipeline runs.

**Knowledge Hub.** Searchable reference material connected to the analyst workspace and agent tools.

**Workspace.** The local data area containing the SQLite database, uploaded files, generated exports, and cached provider state.

**Ollama.** Optional local model server exposed through an OpenAI-compatible endpoint.

**Docling.** Optional external document-layout parser, disabled unless configured.
