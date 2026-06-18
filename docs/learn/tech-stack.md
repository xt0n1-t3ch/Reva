# Tech stack

Why each technology is in Reva, in plain language, with a short "how to explain it" line you can say out loud in an interview. The theme runs through all of it: Reva is a local-first desktop product, so every choice favors running offline, in one process, on the analyst's own machine.

---

## .NET 10 and C# 13

**What it is.** The runtime and language the whole product is written in. One toolchain compiles the domain logic, the database layer, the AI layer, and the desktop UI.

**Why it is here.** Reva needs to be a single self-contained executable an analyst can double-click — no installer, no runtime to chase. .NET's self-contained publish bundles the runtime into the app. C# 13 gives records, pattern matching, nullable reference types, and primary constructors, which is why the codebase is terse and the contracts read like a domain model instead of plumbing.

**How to explain it.** "It's all .NET 10. That lets me ship one self-contained `Reva.exe` with no external runtime, and use one language and one dependency-injection container across the UI, the data layer, and the AI integration. Nullable reference types are on everywhere, so null-safety is enforced at compile time rather than discovered at runtime."

---

## Avalonia 12

**What it is.** A cross-platform native UI framework for .NET. It renders its own controls with Skia, so the app looks and behaves the same regardless of the OS theme.

**Why it is here.** Reva 2.0 is a native desktop app, not a web page. The previous version served a browser UI over localhost; that meant a web server, a port, and a Node build. Avalonia removes all of that — the window is the app. It uses XAML for layout and supports compiled bindings, which catch binding mistakes at build time instead of failing silently at runtime.

**How to explain it.** "The UI is Avalonia, a native XAML framework. The end user gets a real desktop window, not a browser tab — no localhost server, no port. I turned on compiled bindings, so every binding is type-checked against its view model at compile time. Each view declares its `x:DataType`, so if I rename a property, the build breaks instead of the screen going blank."

**The one detail that shows you understand it.** Reva uses a `ViewLocator` that resolves a view from a view model by naming convention. That is why the view-model namespace and the view namespace are kept flat — so `DashboardViewModel` always finds `DashboardView` without manual registration.

---

## MVVM with CommunityToolkit.Mvvm

**What it is.** The Model-View-ViewModel pattern, implemented with Microsoft's CommunityToolkit source generators. `[ObservableProperty]` generates a bindable property from a field; `[RelayCommand]` generates an `ICommand` from a method.

**Why it is here.** It keeps the view models small and the views declarative. The generators remove the boilerplate that usually makes MVVM verbose — no hand-written `INotifyPropertyChanged`, no manual `RelayCommand` classes. View models stay testable because they hold no UI types; they talk to one `IRevaClient` façade.

**How to explain it.** "View models use CommunityToolkit source generators. I write a private field with `[ObservableProperty]` and the generator produces the full bindable property with change notification. I write a method with `[RelayCommand]` and it becomes a command the XAML binds to. The view models never reference Avalonia types, so I can reason about them — and test them — as plain classes. They depend on a single `IRevaClient` interface, which is the seam to the backend."

---

## Dependency injection (Microsoft.Extensions.DependencyInjection)

**What it is.** The same DI container ASP.NET uses, here driving a desktop app.

**Why it is here.** It is the glue that lets the desktop app reuse the exact backend the web host used. `AppServices.Build()` registers infrastructure, AI, the client façade, navigation, and all view models. Calling `AddRevaInfrastructure` and `AddRevaAi` is all it takes to bring the whole pipeline into the app. Scopes are opened per operation so EF's `DbContext` is used correctly.

**How to explain it.** "There's one composition root, `AppServices.Build()`. It registers the infrastructure and AI layers with the same extension methods the old web project used — `AddRevaInfrastructure` and `AddRevaAi` — plus the view models. The app didn't reinvent the backend; it re-hosted it. Each call opens a DI scope so the EF context has the right lifetime."

---

## EF Core (SQLite by default, SQL Server by config)

**What it is.** Entity Framework Core, the .NET ORM, mapping the `*Record` classes to tables and managing schema with migrations.

**Why it is here.** Reva needs durable per-user state — documents, fields, citations, learned mappings, settings, audit events — with zero setup. SQLite gives a single-file database at `%LOCALAPPDATA%\Reva\reva.db`, no server to install. The same code targets SQL Server when a team wants shared storage; that switch is one configuration value, and the registration picks the provider at startup.

**How to explain it.** "Persistence is EF Core. The default provider is SQLite, so the whole database is one file under the user's local app data — nothing to install, nothing to administer. The provider is chosen from configuration, so the identical model runs on SQL Server when an org needs shared storage. Migrations are checked in, so the schema is versioned and reproducible."

**The detail that lands.** Citations are stored as geometry — page, bounding box, polygon, and OCR confidence in `DocumentSourceSpanRecord` — normalized so the review overlay can highlight the exact source region of a value at any zoom.

---

## PaddleOCR (Sdcb.PaddleOCR, PP-OCR V5)

**What it is.** A local optical-character-recognition engine with bundled models, wrapped for .NET by `Sdcb.PaddleOCR`.

**Why it is here.** Bordereaux arrive as scans and photographs. Cloud OCR would mean an API key, a network call, and sending client data off the machine — none of which fit a local-first product. PaddleOCR runs entirely offline with models shipped inside the app, and it returns per-line geometry, which is what makes citations point at the right region instead of the whole page.

**How to explain it.** "Scanned documents go through PaddleOCR, running fully offline with bundled PP-OCR V5 models — no Python, no cloud, no key. It returns text plus bounding boxes per line, so the review screen can highlight the exact region a field came from. Keeping OCR on-device is a hard requirement for a product handling client data."

---

## Ollama and the configurable VLM (Microsoft.Extensions.AI + OpenAI client)

**What it is.** Ollama runs local large and vision-language models and exposes an OpenAI-compatible API. Reva talks to it through `Microsoft.Extensions.AI`'s `IChatClient` abstraction, using the OpenAI client pointed at the local endpoint.

**Why it is here.** The AI is additive and local. A vision model can read a page image and propose fields the rules missed, and the copilot can answer questions and act on the app. Using the OpenAI-compatible surface means the same client code works against any model Ollama serves — which is exactly why the model is a setting, not a constant. `Microsoft.Extensions.AI` gives a provider-neutral `IChatClient` and a built-in function-calling loop, so the agent's tools are ordinary C# methods.

**How to explain it.** "AI is local and optional, served by Ollama over its OpenAI-compatible endpoint. I talk to it through `Microsoft.Extensions.AI`, so the client code is provider-neutral and the model is chosen at runtime from a menu rather than hardcoded. The same `IChatClient` drives both VLM-assisted extraction and the agent. The agent's tools are plain C# methods registered as functions — `Microsoft.Extensions.AI` runs the tool-calling loop for me."

**The principle to state.** The model never overrides validated data. The merger only accepts a proposal with a citation and sufficient confidence, and it will not overwrite a money total the deterministic tier already found. So the model can help without ever being trusted blindly.

---

## Skia (via Avalonia and LiveCharts)

**What it is.** The 2D graphics engine Avalonia renders with. `LiveChartsCore.SkiaSharpView.Avalonia` uses the same engine to draw charts.

**Why it is here.** Native, consistent rendering across platforms without depending on the OS's own controls, plus a charting library that draws on the same surface so the dashboard's visuals match the rest of the UI.

**How to explain it.** "Avalonia renders with Skia, the same engine behind Chrome's canvas, so the UI is pixel-consistent everywhere and doesn't inherit OS control quirks. Charts use LiveCharts on the same Skia surface, so dashboard visuals are part of the native render, not an embedded widget."

---

## The supporting parser libraries

These are the file-format readers behind the parser router. They matter because "handles any document the back office receives" is a product claim, and each library backs one slice of it.

| Library | Format |
|:---|:---|
| `PdfPig` | Digital PDF text and layout |
| `DocumentFormat.OpenXml` | DOCX, PPTX |
| `ClosedXML` | XLSX |
| `MimeKit` | EML email, including attachments |
| `MSGReader` | Outlook MSG |

**How to explain it.** "Every format has a dedicated parser behind a router that picks by sniffed content, not just extension. PDFs use PdfPig, Office files OpenXml and ClosedXML, email MimeKit and MSGReader with recursive attachment handling. Anything unrecognized falls back to a low-confidence visible-text record, so ingestion never hard-fails on a weird file."

---

## How the pieces fit, in one sentence each

- **.NET 10** ships it as one self-contained executable.
- **Avalonia** makes it a native window with compile-checked bindings.
- **CommunityToolkit MVVM** keeps view models small and testable.
- **DI** lets the desktop app reuse the exact backend the web host used.
- **EF Core + SQLite** give durable per-user state with zero setup.
- **PaddleOCR** reads scans offline and returns the geometry citations need.
- **Ollama + Microsoft.Extensions.AI** make the AI local, optional, and model-swappable.
- **Skia** renders all of it consistently across platforms.
