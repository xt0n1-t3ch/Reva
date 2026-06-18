# Interview cheatsheet

Crisp answers to the questions Reva is likely to attract, the elevator pitch, and a demo script you can run live. Read the [code tour](code-tour.md) and [tech stack](tech-stack.md) first; this turns that knowledge into spoken answers.

---

## The 30-second elevator pitch

"Reva is a native desktop app that turns messy reinsurance documents — emails, spreadsheets, scanned PDFs — into structured, reviewable data. Every extracted value carries a citation back to the exact spot on the page it came from, and stated totals are reconciled against the line items that should add up to them. It runs fully offline by default: native parsers, on-device OCR, and a local database, all in one self-contained executable. On top of that, an optional local vision model can read page images and propose extra fields, and an AI copilot can answer questions about your documents and even drive the app for you. The model is chosen from a menu, not hardcoded, so the AI improves the product without ever being a hard dependency."

---

## The architecture in one breath

"Four projects in one process. `Reva.Core` is the domain vocabulary — the contracts and the canonical field set. `Reva.Infrastructure` is the machinery — EF Core, the parsers, PaddleOCR, the extraction and reconciliation engines, and the agent. `Reva.Ai` is the swappable model layer — a model registry and a vision extractor. `Reva.App` is the Avalonia desktop window. Dependencies point inward, and everything talks through in-process calls. There's no HTTP anywhere in the product."

---

## Likely questions and answers

**Why a native desktop app instead of a web app?**
"The data is sensitive and the requirement is local-first. A native window means no web server, no port, no Node build, and nothing leaving the machine. The previous version served a browser UI over localhost; 2.0 removes that whole layer. Avalonia gives a real desktop window with compile-checked bindings, and it reuses the exact backend the web host used through dependency injection."

**How does the AI not corrupt the data?**
"Two safeguards. First, the model is additive — the deterministic tier always runs and produces the baseline; the model only proposes extras. Second, the merge is conservative: a proposal is accepted only if it carries a citation and clears a confidence threshold, and it will never overwrite a money total the deterministic extractor already found. The vision extractor also fails closed — any malformed reply is dropped to null. So a bad model response degrades to 'no help,' never to 'wrong data.'"

**Why is the model configurable, and how?**
"Because the local-model landscape moves fast and the best model depends on the user's hardware and what they've pulled. The model is a setting backed by a registry. The registry ships a curated menu — Qwen 3.5, Qwen3-VL, granite-docling, and others — probes Ollama's `/api/tags` to mark which are installed, and appends anything else it finds. The choice is persisted and used for both the copilot and VLM extraction. Because Reva talks to Ollama over the OpenAI-compatible API through `Microsoft.Extensions.AI`, the same client code works against any of them."

**How does the copilot move the UI without hacking it?**
"Through a small in-process message bus. The agent's action tools — open document, correct a field, navigate, export — publish a typed `AppAction` onto an `IAppActionBus`. The app's `NavigationService` subscribes to that bus and performs the real navigation on the UI thread. Chat and UI share one channel, so they stay in lockstep. The agent doesn't reach into views; it sends intents and the UI reacts."

**What happens to a file Reva doesn't recognize?**
"It never hard-fails. The parser router picks a parser by sniffed content, and if none matches — or if a parser throws — it falls back to a visible-text reader and records a warning. The file still becomes a low-confidence reviewable record. 'Never quarantine, never crash' is a deliberate intake policy."

**How do citations work?**
"OCR and the PDF renderer capture per-line geometry — page, bounding box, polygon, OCR confidence — normalized to the rendered page size. That geometry is stored per source span and assembled into the review payload. When you hover a field, the review view highlights the exact source region, and it scales correctly with zoom because the coordinates are normalized."

**What's the difference between confidence and 'reviewed'?**
"Machine confidence is computed from how a value was located; it's honest, never a flattering constant. When an analyst corrects a field, it's marked Reviewed — a separate, human signal. Reva never inflates confidence to make an edit look better. That keeps the audit trail truthful."

**How does it learn from corrections?**
"Schema mapping has a precedence order: a learned per-sender rule wins, then a static alias, then a bounded fuzzy match, otherwise the header is left unmapped. When an analyst corrects a mapping, Reva persists a learned rule keyed to that sender or email domain. The next document from that sender maps itself. It's adaptive without any training step."

**Why SQLite, and can it scale to a team?**
"SQLite is zero-setup — the whole database is one file in the user's local app data, which fits a local-first desktop product. The provider is chosen from configuration, so the identical EF model runs on SQL Server when a team needs shared storage. No code changes, just config."

**How is this testable?**
"View models hold no UI types and depend on one `IRevaClient` interface, so they're plain classes under test. The domain logic — extraction, the merge rules, reconciliation, schema mapping — lives in infrastructure behind interfaces and is unit-tested directly. The compiled bindings catch view/view-model drift at build time, which removes a whole class of UI bugs from the test burden."

**Why MVVM with the CommunityToolkit?**
"MVVM separates the screen from its logic so the logic is testable and the screen is declarative. The CommunityToolkit source generators remove the boilerplate — `[ObservableProperty]` and `[RelayCommand]` generate the notification and command code — so the view models stay small and readable."

**What's the role of `IRevaClient`?**
"It's the single seam between the UI and the backend. Every view model takes that one façade instead of a dozen services. It opens a DI scope per call and forwards to the workflow, exporter, settings store, and model registry. It keeps the view models ignorant of infrastructure details and gives one place to mock for tests."

---

## The three-tier model, stated cleanly

"There are three independent tiers. Tier one is deterministic and always on — native parsing, offline OCR, rule-based extraction, reconciliation — and it needs nothing external. Tier two is a local vision model that proposes extra fields, enabled by a toggle and a chosen model. Tier three is an optional Docling parser for hard layouts. Each tier degrades gracefully: if tier two or three is absent, tier one still delivers a complete result."

---

## Demo script

Run the app:

```powershell
dotnet run --project src/Reva.App/Reva.App.csproj
```

A native window opens. Walk it in this order:

1. **Dashboard.** "This is the work queue — status, confidence, exception count, and real page thumbnails. I can filter to items that need review." Reseed the demo corpus from Settings if the queue is empty.
2. **Upload.** Drag a seeded `.eml` onto the upload area. "Reva stores and hashes it, parses the email body and its attachment, classifies it, extracts canonical fields, maps the sender's headers, reconciles the totals, and opens it." All in-process, no network.
3. **Review split view.** Hover a field. "The page on the left highlights the exact region this value was read from. On the right, each canonical field shows its value, confidence, status, citation, and any exception. The highlight is geometry-backed, so it's correct at any zoom."
4. **Reconciliation.** Open an exception card. "Detected is what the document stated; Expected is what I computed by summing the line items; the score is the agreement. Money checks honor a configurable tolerance."
5. **Mappings.** "Here's how this sender's column headers mapped to canonical fields, and why — learned, alias, or fuzzy. I'll correct one mapping; that saves a rule for this sender, and the next document from them maps itself."
6. **Copilot.** Open the panel. Ask: "Which documents have reconciliation exceptions?" Then: "Open the first one and highlight Premium." "Notice the copilot didn't just answer — it navigated the app and highlighted the field. Its action tools publish messages onto an in-process bus that the navigation service follows."
7. **Settings — the model menu.** "The vision model is chosen here from a menu, not hardcoded. It lists curated models and marks which I've pulled into Ollama. I can turn on LLM assist to let a vision model propose extra fields during extraction." Toggle the theme to show light/dark.
8. **Export.** Pick a template, show the live preview, download CSV. "Templates are reusable layouts; the Lloyd's CRS template shows a downstream reporting shape."

Close on the pitch: "One file, one window, fully offline by default, with optional local AI that helps but is never trusted to overwrite validated data."

---

## Three things to always mention

- **Local-first is a hard constraint, not a preference.** OCR, the database, and the model all run on-device. That decision shaped every technology choice.
- **The AI is additive and fails closed.** It improves results when present and disappears cleanly when absent, and it never overrides a validated figure.
- **Trust is built in.** Every value has a citation, confidence is honest, and human edits are tracked as Reviewed rather than disguised as machine certainty.
