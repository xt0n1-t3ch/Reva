# Model landscape

Reva treats model access as a setting, not as the product identity.

The core workflow must run without a hosted model. Providers can improve chat, summaries, extraction suggestions, and difficult document understanding when configured.

## Provider choices

| Provider class | Best for | Product rule |
|:---|:---|:---|
| No provider | Offline review, deterministic demos, locked-down environments. | Core upload, extraction, reconciliation, review, and export still work. |
| Local Ollama | Private demos and local experimentation. | Good for showing optional AI without sending data to a hosted service. |
| OpenAI-compatible endpoint | Stronger hosted models behind a standard API shape. | Configure base URL, model, and key outside source control. |
| HuggingFace-backed inference | Comparing model families and document/vision experiments. | Useful for research, not required for baseline workflow. |
| Optional layout worker | Harder document-layout extraction. | Disabled unless configured. |

## Local model families worth discussing

| Family | Why it is relevant |
|:---|:---|
| Qwen VL | Strong document and visual understanding for local tests. |
| Granite document models | Document-oriented models that are useful for extraction experiments. |
| Llama | General reasoning and assistant-style chat. |
| Gemma | Smaller general models for constrained hardware. |

## Guardrail for product quality

A model can propose. A model can explain. A model can summarize. It should not silently overwrite reviewed financial data.

Reva keeps the trust loop in the application:

- provenance stays attached to values
- reconciliation stays deterministic
- analyst review stays explicit
- mutations go through backend tools
- provider configuration stays optional

## Interview line

"The model layer is deliberately replaceable. The product value is the workflow: source-cited extraction, reconciliation, review, and export. AI improves the experience when available, but the system remains useful without it."
