# Code tour

A file-by-file walk through Reva, written so you can explain any part of it from memory. Read it top to bottom once; after that, jump to the section you need. Every path is real and every responsibility is described in plain terms.

The mental model first: Reva is four projects stacked inside one process. `Reva.Core` is the vocabulary. `Reva.Infrastructure` is the machinery that does the work. `Reva.Ai` is the swappable model layer. `Reva.App` is the desktop window you see. Dependencies point inward — the app knows about infrastructure and AI, infrastructure and AI know about core, and core knows about nobody.

```
Reva.App  ─────►  Reva.Ai  ─────►  Reva.Core
   │                                  ▲
   └──────────►  Reva.Infrastructure ─┘
```

---

## Reva.Core — the domain vocabulary

This project has no dependencies on UI, EF, or any framework. It is the set of names and shapes the rest of the system agrees on. If you can explain `Reva.Core`, you can explain what Reva is *about* before touching how it works.

**`Contracts.cs`** is the heart of it. Every data transfer object lives here as a C# `record`:

- `DocumentUploadResult`, `DocumentSummary`, `DocumentDetail` — what you get back when you upload, list, or open a document.
- `ExtractedField(Name, Value, Confidence, Source, IsCorrected)` — one canonical field with its provenance. `Source` is the citation string; `IsCorrected` is set when an analyst fixed it.
- `ExtractedTable` — a parsed table: headers plus rows.
- `SchemaMapping` — how one sender's column header (`SourceHeader`) maps to a canonical field (`CanonicalField`), with the source of that decision (`learned` / `alias` / `fuzzy` / `unmapped`).
- `ExtractionIssue` — a finding. A plain finding carries severity and message; a *reconciliation* finding also carries the field, the `Detected` (stated) value, the `Expected` (computed) value, and an agreement score. The `IsReconciliation` property is true only when all three are present.
- `ReviewDecision`, `FieldCorrection`, `SchemaMappingCorrection` — what an analyst sends back when they review.
- `ExportTemplate`, `ExportColumn`, `ExportTemplateDraft`, `ExportPreview`, `ExportFile`, `ExportRecord` — the export model. A template is a reusable layout; a column maps an output header to a canonical source; built-in templates can't be deleted.

**`Contracts/BdxReviewPayload.cs`** is the assembled, review-ready shape: fields with provenance and citations, pages, and reconciliation checks, ready for the split-view review screen. It is what `IBdxReviewPayloadAssembler` produces.

**`Reinsurance/ReinsuranceFieldNames.cs`** is the single source of truth for the canonical field set: `Cedent`, `Broker`, `Reinsurer`, `Contract Reference`, `Line of Business`, `Period`, `Currency`, `Premium`, `Claims`, `Commission`, `Cession %`, `Retention`, `Limit`. The `Canonical` array is referenced everywhere — by the extractor, the VLM prompt, and the merger — so there is exactly one list of fields in the whole system.

**`Reinsurance/MoneyFormatter.cs`** is the one place money is formatted and parsed, so totals compare consistently. **`Reinsurance/ExceptionSeverity.cs`** and **`ReinsuranceDocumentType.cs`** are the enums for finding severity and document class (technical account, bordereau, statement of account).

**`Documents/`** holds the state machine: `DocumentStatus` (where the document is in the pipeline), `ReviewState` (analyst verdict), `DocumentIntakePolicy` and `AcceptedDocumentExtensions` (what is accepted and how unknown files are handled).

**`Settings/AppSettings.cs`** is the one persisted settings row: theme, accent color, product name, the two confidence thresholds (low/medium boundaries), default export template, reconciliation tolerance, and the `UseLlmAssist` toggle. `RuntimeSettings.cs` is the in-memory mirror loaded at startup and on every save, so the whole app reflects a change without re-reading the database.

---

## Reva.Infrastructure — the machinery

This is the largest project and where the real work happens. It depends on `Reva.Core` and runtime libraries. One static entry point wires it all together.

**`RevaInfrastructureRegistration.cs`** is the DI manifest. `AddRevaInfrastructure(services, configuration)` registers everything: the EF `DbContext` (SQLite by default, SQL Server when configured), the hasher, storage, OCR engine, PDF renderer, the parser router, the classifier and extractor, the extraction merger, the schema-mapping service, the document workflow, the export stack, settings, data maintenance, the action bus, and the agent chat service. Read this file first when you need to know "what is available to inject."

**`RevaConfiguration.cs`** centralizes every configuration key as a constant (`RevaConfigurationKeys`) and the provider names (`RevaDatabaseProviders`). No magic strings live anywhere else.

### Persistence — `Persistence/`

**`RevaDbContext.cs`** is the EF Core context. The `*Record` classes are the database rows: `DocumentRecord` (the root, with collections of the rest), `DocumentFieldRecord`, `DocumentTableRecord`, `DocumentIssueRecord`, `DocumentPageRecord`, `DocumentSourceSpanRecord` (the citation geometry — page, bbox, polygon, OCR confidence), `DocumentSchemaMappingRecord`, `LearnedSchemaMappingRecord` (the per-sender rules learned from corrections), `ExportTemplateRecord`, `AppSettingsRecord`, and `ReviewEventRecord` (the audit trail of who changed what).

**`Migrations/`** is the EF migration history. **`RevaDbContextFactory.cs`** is the design-time factory the `dotnet ef` tooling uses. **`DemoData/DemoDocumentSeeder.cs`** loads the sample corpus so you can demo against real-looking data.

### Parsing — `Parsing/`

**`ParserRouter.cs`** is the dispatcher. It owns an ordered list of `IFileParser` implementations, picks the first one whose `CanParse(extension)` returns true, and runs it. If a parser throws, it falls back to the binary visible-text parser and records a warning — ingestion never crashes on a bad file. The typed parsers around it:

- `PlainTextParsers.cs` — text, Markdown, CSV with encoding detection, and the binary fallback.
- `PdfFileParser.cs` — digital PDFs via `PdfPig`; routes scanned PDFs to the renderer + OCR.
- `OfficeFileParsers.cs` — Word and PowerPoint via OpenXml; `ExcelFileParser` for XLSX.
- `LegacyExcelFileParser.cs`, `OpenDocumentSpreadsheetParser.cs`, `GoogleSheetsStubParser.cs` — the long tail of spreadsheet formats.
- `ImageFileParser.cs` — images straight to OCR.
- `EmailFileParsers.cs` — EML via `MimeKit`, MSG via `MSGReader`, each recursing into attachments by handing them back to the router.

**`ParsedDocument.cs`** is the output shape every parser produces: text, markdown, raw JSON, pages (with image paths), tables, and source spans (the geometry). **`IDocumentParser.cs`** / **`IFileParser.cs`** are the interfaces. **`DoclingParserOptions.cs`** configures the optional external Docling worker.

### OCR and rendering — `Ocr/`, `Rendering/`

**`PaddleOcrEngine.cs`** wraps `Sdcb.PaddleOCR` with bundled PP-OCR V5 models — fully offline, no Python, no cloud. It returns per-line text, confidence, and normalized bounding boxes and polygons, which become the citation overlays. **`PdfiumPageImageRenderer.cs`** rasterizes PDF pages to images so scanned PDFs can be OCR'd and so the review view has real page pictures.

### Extraction — `Extraction/`

This is tier one and tier two of the processing model.

- **`ReinsuranceClassifier.cs`** decides the document type.
- **`ReinsuranceFieldExtractor.cs`** is the deterministic extractor: it locates canonical field values by rule, computes honest confidence from *how* it found each value, and never assigns flattering constants.
- **`LlmFieldExtraction.cs`** holds the VLM/LLM seam: the `ILlmFieldExtractor` interface, the `DisabledLlmFieldExtractor` no-op (used when assist is off), and the text-only `OllamaLlmFieldExtractor`. It also holds **`ExtractionMerger`** — the conservative merge. A model proposal is only accepted if it has a value, confidence ≥ 0.6, and a citation; and it never overwrites an already-populated money field. This is why the model can help without corrupting a validated total.
- **`LlmExtractionOptions.cs`** carries the prompts and limits.

### Schema mapping — `SchemaMapping/`

**`SchemaMappingService.cs`** turns a sender's headers into canonical fields in a strict order of precedence: a learned per-sender rule wins; then a static alias; then a bounded fuzzy match; otherwise the header is left unmapped as a low-confidence review row. When an analyst corrects a mapping, it persists a `LearnedSchemaMappingRecord` keyed to that sender or email domain, so the next document from them maps itself. This is the adaptive part of Reva.

### Reconciliation, review, export

- **Reconciliation** is computed inside the review assembler and the workflow: stated control totals (Detected) are compared against the value summed from the line items (Expected), with the configured money tolerance. Each disagreement becomes a field-level `ExtractionIssue`.
- **`Review/BdxReviewPayloadAssembler.cs`** assembles a `DocumentRecord` into the `BdxReviewPayload` the review screen needs — fields with citations, pages, and reconciliation checks.
- **`Export/`** — `ExportTemplateStore` (template CRUD with built-ins), `DocumentExporter` (renders CSV / Excel / JSON and live previews), `ExportTemplateDefaults` (the shipped templates, including the Lloyd's CRS shape).

### Settings, storage, hashing, ingestion

- **`Settings/SettingsStore.cs`** reads and writes the single settings row and refreshes `RuntimeSettings`. **`DataMaintenance.cs`** reseeds or clears the workspace.
- **`Storage/LocalDocumentStorage.cs`** stores uploads under safe names. **`Hashing/Sha256DocumentHasher.cs`** hashes every upload so duplicates and tampering are detectable.
- **`Ingestion/`** holds the inbound seams: the file-based `.eml`/`.msg` source and the disabled OAuth mailbox stubs, plus the parser adapter layer.

### The workflow — `DocumentWorkflow.cs`

This is the spine. `IngestAsync` stores and hashes the upload, then `ParseAndExtractAsync` runs the whole pipeline: parse → classify → deterministic extract → *optionally* ask the VLM for a proposal (only when `UseLlmAssist` is on) → merge → schema-map → reconcile → persist. `ListAsync`, `GetAsync`, `ReviewAsync`, and `ExportAsync` are the read and write operations the app and the copilot both call. Read the merge section (around the `extractionMerger.Merge` call) to see the three tiers meet in one place.

### The agent — `Agent/`

This is the brain and hands of the copilot, and the one piece of infrastructure the UI most directly leans on.

- **`AppAction.cs`** defines the whole copilot-to-UI contract in one file: the `AppActionKind` enum (Navigate, OpenDocument, GotoPage, Highlight, Refresh, SetFilter, Toast, Progress), the `AppAction` record, the `IAppActionBus` interface, and the `AppActionBus` implementation (a small thread-safe observable). This is the seam that lets the copilot move the real UI without touching it directly.
- **`AgentChatService.cs`** is the tool loop. `BuildTools(...)` constructs the function tools the model can call. Read tools (`list_documents`, `get_document`, `reconcile`, `explain_field`) return JSON over the real workflow. Action tools (`goto`, `open_document`, `process_documents`, `correct_field`, `set_review_state`, `export_document`, `filter_queue`, `reseed`, `clear`) do the work *and* publish `AppAction`s to the bus so the UI follows. `StreamAsync` streams the model's response. Every tool is wrapped in `CatchAsync` so a failure returns a clean error instead of breaking the turn.
- **`AgentChatRequestParser.cs`** parses the chat history into model messages. **`AiSdkUiMessageStreamMapper.cs`** maps streamed updates to UI message parts. **`AgentChatOptions.cs`** / **`RevaAgentConfiguration.cs`** / **`AgentActionConstants.cs`** / **`AgentStreamConstants.cs`** centralize the model id, temperature, context size, max tool iterations, route names, tool names, and all the user-facing strings.
- **`OllamaProcessManager.cs`** best-effort starts `ollama serve` when Ollama is installed.

---

## Reva.Ai — the swappable model layer

Small, focused, and the answer to "how is the model not hardcoded." Depends on `Reva.Core` and `Reva.Infrastructure` (it implements the infrastructure's `ILlmFieldExtractor`).

**`AddRevaAiExtensions.cs`** is the DI entry point: `AddRevaAi(services, configuration)`. It reads the AI options, registers the model registry, and — only when vision extraction is enabled — registers the `VlmFieldExtractor` as the infrastructure's `ILlmFieldExtractor`, wired to an OpenAI-compatible `ChatClient` pointed at the local Ollama endpoint with a placeholder key.

**`AiConstants.cs`** centralizes the Ollama paths (`/api/tags`, the `/v1` OpenAI-compatible suffix, the placeholder key), the persisted model-state file path under `%LOCALAPPDATA%\Reva`, and the VLM extraction defaults (max pages, max characters, the PNG media type, the citation token, and the system prompt).

**`AiProcessingOptions.cs`** is the bound options object: base URL, OpenAI base URL, active model, the vision-extraction flag, and the timeout — each with a default constant and a `Reva:Ai:*` configuration key.

**`Models/ModelDescriptor.cs`** defines a model entry (`Id`, `DisplayName`, `Kind`, `Notes`, `Installed`) and the curated June-2026 menu in `CuratedModels.Menu` — Qwen 3.5, Qwen3-VL, Qwen3-VL 8B, Granite Docling, Llama 4, Gemma 4. This is the list the Settings screen shows.

**`ModelRegistry.cs`** (behind **`IModelRegistry.cs`**) is the configurable-model engine. `ListAsync` probes Ollama's `/api/tags`, marks which curated models are installed, and appends any other installed models it finds. `GetActiveModelAsync` / `SetActiveModelAsync` read and persist the chosen model to the state file (with an in-memory cache and a semaphore so it is safe under concurrency). `IsOllamaAvailableAsync` is the online check the shell status dot uses. Nothing in here hardcodes a model — it reflects what you picked and what you have.

**`VlmFieldExtractor.cs`** is tier two in action. Given a parsed document and the deterministic result, it collects up to eight page images, builds a multimodal prompt (the system prompt, the instruction to return strict JSON with a citation token, the canonical field list, and the already-known deterministic fields), sends them to the chosen vision model with temperature 0, and parses the reply. It validates every proposed field against the canonical names, clamps confidence, and enforces the citation token. Anything malformed is dropped; the whole call fails closed to `null` so a model hiccup can never corrupt extraction. If there are no page images, it falls back to the parsed text.

---

## Reva.App — the desktop application

This is the shipped `Reva.exe`. Avalonia 12, MVVM with CommunityToolkit, compiled bindings on. Depends on Core, Infrastructure, and Ai.

### Startup and composition

**`Program.cs`** is the Avalonia entry point — it builds the `AppBuilder`, detects the platform, loads the Inter font, and starts the classic desktop lifetime. **`App.axaml`** / **`App.axaml.cs`** set up application resources and create the main window.

**`Composition/AppServices.cs`** is the app's DI container. `Build()` ensures the data directory exists, builds configuration (forcing SQLite at the per-user path and the per-user upload root), then calls `AddRevaInfrastructure`, `AddRevaAi`, registers `IRevaClient`, `INavigationService`, and every view model. This is the file that connects the desktop app to the same domain services the old web host used.

**`Composition/AppDataPaths.cs`** defines the per-user workspace: `%LOCALAPPDATA%\Reva`, the `reva.db` SQLite file, and the `uploads` folder, plus the connection string. `Ensure()` creates the folders on first run.

**`appsettings.json`** carries the `Reva:*` configuration defaults that ship with the app.

### Services

- **`Services/IRevaClient.cs`** / **`RevaClient.cs`** are the app's façade over infrastructure. Instead of view models taking a dozen services each, they take one `IRevaClient`. It opens a DI scope per call and forwards to the workflow, template store, exporter, settings store, and model registry. This is the single boundary between the UI and the backend.
- **`Services/INavigationService.cs`** / **`NavigationService.cs`** own routing *and* subscribe to the action bus. `NavigateTo` swaps the current view model; `OpenDocument` navigates to Review and tells the target which document to open. The nested `ActionObserver` listens to `IAppActionBus` and dispatches `Navigate` / `OpenDocument` actions onto the UI thread — this is the receiving half of the copilot-drives-the-UI loop.
- **`Services/DocumentContentTypes.cs`** maps file extensions to content types for upload.
- **`Navigation/AppRoutes.cs`** is the route constant set: dashboard, review, mappings, export, settings.

### View models — `ViewModels/`

All use `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit and are partial classes.

- **`ViewModelBase.cs`** is the shared base.
- **`ShellViewModel.cs`** is the frame: the left-nav active route, the online status and active model shown in the header, the theme toggle, the copilot open/close toggle, and it owns the `CopilotViewModel`. It wires `NavigationService.CurrentChanged` to its `Current` property so the content area follows navigation.
- **`DashboardViewModel.cs`** is the work queue — list, filter, status, confidence, exceptions.
- **`UploadViewModel.cs`** handles drag-drop and browse upload.
- **`ReviewViewModel.cs`** is the split-view review: pages with citation highlights on one side, canonical fields with confidence, status, and exceptions on the other. It implements the document-navigation target so the copilot can open and highlight.
- **`MappingsViewModel.cs`** shows per-sender header-to-canonical mappings and their provenance.
- **`ExportViewModel.cs`** is template selection, live preview, and download.
- **`SettingsViewModel.cs`** is theme/accent/branding, confidence thresholds, reconciliation tolerance, the LLM-assist toggle, the default template, the **model menu**, and data management.
- **`CopilotViewModel.cs`** is the chat. It builds the request JSON from the message history, opens a scope, builds the tools via `IAgentChatService.BuildTools`, streams the turn, and renders text deltas plus per-tool "step" cards (running → done) on the UI thread. `RefreshStatusAsync` updates the online indicator. Read this with `AgentChatService` open beside it to see the full loop.

### Views — `Views/`

Every `.axaml` view sets `x:DataType` to its view model and `x:CompileBindings="True"`, so bindings are checked at compile time. The view namespace is flat (`Reva.App.Views`) and the view-model namespace is flat (`Reva.App.ViewModels`) so **`ViewLocator.cs`** can resolve a view from a view model by name convention.

- **`MainWindow.axaml`** hosts the shell.
- **`ShellView.axaml`** is the chrome: a 248px left navigation rail (Dashboard, Review, Mappings, Export, Settings) with active-state styling, a top bar showing the title, the active model chip, a Tour button, the theme toggle, and the Copilot button, the main content `ContentControl` bound to `Current`, and a 380px right-side copilot panel that shows when `IsCopilotOpen` is true.
- **`DashboardView`, `ReviewView`, `MappingsView`, `ExportView`, `SettingsView`, `CopilotView`, `UploadControl`** are the screens, each bound to its view model.

### Theming and helpers

- **`Themes/Tokens.axaml`** holds the design tokens (colors, brushes, radii) and **`Controls.axaml`** the control styles (the `nav` button, chips, status dots, headings). Light and dark are both supported via the theme toggle.
- **`Converters/`** — `StringEqualsConverter` (active-route highlighting), `OnlineBrushConverter` (status dot color), `NonEmptyConverter` (show the model chip only when a model is set).
- **`Assets/`** holds the app icon and fonts.

---

## How a single upload flows through every project

Tie it together with one path you can narrate end to end:

1. You drop a file on the upload control. `UploadViewModel` calls `IRevaClient.UploadAsync` (**Reva.App**).
2. `RevaClient` opens a scope and calls `IDocumentWorkflow.IngestAsync` (**Reva.App → Reva.Infrastructure**).
3. The workflow stores and hashes the file, then parses it through `ParserRouter`, OCR'ing scans with PaddleOCR (**Reva.Infrastructure**).
4. It runs the deterministic extractor; if `UseLlmAssist` is on, it asks the `VlmFieldExtractor` for a proposal and merges it conservatively (**Reva.Infrastructure → Reva.Ai → Reva.Infrastructure**).
5. It schema-maps the headers, reconciles stated totals against line items, and persists everything to SQLite via EF Core (**Reva.Infrastructure**, using **Reva.Core** contracts and **Reva.Core** field names).
6. The dashboard refreshes; you open the document in Review and see each field with its citation. If you correct a mapping, a learned rule is saved for that sender.
7. If you ask the copilot to export it, the agent tool runs the export and publishes a `Navigate` action; `NavigationService` moves you to the Export screen (**Reva.Infrastructure agent → bus → Reva.App navigation**).

Every step is an in-process call. There is no HTTP anywhere in that chain.
