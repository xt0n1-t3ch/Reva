# Model landscape (June 2026)

Why Reva treats the model as a setting instead of a constant, and a map of the local models worth knowing in mid-2026. This is the background behind the model menu in Settings and the `CuratedModels.Menu` list in `src/Reva.Ai/Models/ModelDescriptor.cs`.

---

## The one idea

The local model layer moves faster than a release cycle, and the right model depends on the user's hardware, their documents, and what they have already pulled. So Reva does not bake in a model. It ships a curated menu, discovers what is installed on the machine, and lets the analyst choose. The choice is persisted and used everywhere the AI runs — both VLM-assisted extraction and the copilot.

That single decision is why the AI can keep improving without a code change. A better model lands in Ollama, the user pulls it, picks it from the menu, and Reva uses it.

---

## How extraction is layered (where models fit)

It helps to separate three jobs that often get lumped together as "AI":

- **OCR** — turning pixels into text and geometry. In Reva this is deterministic and always on (PaddleOCR), because citations need exact per-line coordinates.
- **Layout / document parsing** — understanding tables, reading order, and structure. This is the optional Docling tier.
- **Field reasoning** — deciding which text is the Premium, the Cedent, the cession rate. This is where a vision-language model helps, on top of the deterministic rules.

A vision-language model can blur the lines — it can read a page image and reason about fields in one step — which is exactly why the curated menu is VLM-heavy. But Reva keeps OCR deterministic so the geometry stays exact, and treats the VLM as a proposer that the merger checks, not as the source of truth.

---

## The curated menu, and why each entry is there

These are the models in Reva's Settings menu. Each is a sensible local choice in June 2026; the list is opinionated, not exhaustive.

| Model | Kind | Why it's on the list |
|:---|:---|:---|
| **Qwen 3.5** | vision | The newest Qwen generation, multimodal. The recommended pick when the user's hardware can run it — strongest general document reasoning of the set. |
| **Qwen3-VL** | vision | A strong, widely available document VLM. A reliable default for reading bordereaux page images. |
| **Qwen3-VL 8B** | vision | The balanced size — good document reading without needing a large GPU. Reva's default model id when present, because it runs on common hardware. |
| **Granite Docling** | vision / OCR | IBM's small document-specialized VLM, tuned for layout and OCR-style tasks. The lightweight option when resources are tight. |
| **Llama 4** | text | A capable general text model for the copilot when vision isn't needed. |
| **Gemma 4** | text | A second general text option, useful as a smaller copilot model. |

Reva also appends anything else the user has pulled into Ollama, so the menu reflects the real machine, not just this list.

---

## The wider landscape worth knowing

Beyond the menu, these are the names that come up in mid-2026 conversations about local document AI. Knowing where each fits is the point.

**The Qwen family (Qwen3-VL, Qwen 3.5).** The most active open vision-language line. Strong at reading documents from images and reasoning about their contents, available in a range of sizes so the same family scales from a laptop to a workstation. This is why Reva's menu leans on it.

**PaddleOCR-VL.** The vision-language evolution of the PaddleOCR line — OCR plus layout understanding in one model. Reva already uses classic PaddleOCR for deterministic, geometry-exact OCR; the VL variant is the kind of model that could slot into the VLM tier as it matures, which is another reason the tier is a swappable seam.

**MinerU.** A document-extraction pipeline aimed at turning PDFs and scans into clean structured content — strong on complex layouts, formulas, and tables. It belongs to the same problem space as the Docling tier: richer parsing for hard documents.

**Docling and granite-docling.** Docling is IBM's document-conversion toolkit; granite-docling is the compact VLM tuned for it. In Reva, Docling is the optional tier-three parser for difficult PDFs and scans, and granite-docling is on the menu as a small, document-focused vision model. They are the "extra tooling helps" path, off by default.

**General text models (Llama 4, Gemma 4).** When the task is conversation rather than reading a page — answering a question about already-extracted data — a text model is enough and lighter. The menu includes both so the copilot can run small when vision isn't required.

---

## Why "configurable" is the right call

A fixed model would be wrong three ways:

- **Hardware varies.** An 8B vision model that's perfect on a workstation is too heavy for a thin laptop, where a small text or docling-class model is the right trade.
- **Documents vary.** Clean digital PDFs barely need a model; dense scanned bordereaux benefit from a strong VLM. Letting the analyst match the model to the workload beats one global default.
- **The field moves.** The best local model in June 2026 will not be the best in December. A menu plus discovery means the product adopts new models the moment the user pulls them — no release required.

And the safety net underneath makes a wrong choice cheap: the deterministic tier always runs, and the merger refuses any model proposal that lacks a citation or overwrites a validated total. The model can only help; it can't break the result.

---

## How Reva implements it (the short version)

- `CuratedModels.Menu` (`src/Reva.Ai/Models/ModelDescriptor.cs`) holds the curated list above.
- `ModelRegistry` (`src/Reva.Ai/ModelRegistry.cs`) probes Ollama's `/api/tags`, marks installed models, appends extras, and persists the active choice to a state file under `%LOCALAPPDATA%\Reva`.
- The Settings screen shows the menu; the chosen model drives both `VlmFieldExtractor` (tier-two extraction) and the copilot's `AgentChatService`.
- Everything talks to Ollama over its OpenAI-compatible endpoint through `Microsoft.Extensions.AI`, so swapping models is a configuration change, not a code change.
