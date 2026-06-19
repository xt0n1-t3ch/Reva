# Model landscape

Reva treats model access as a setting, not a product identity. The deterministic pipeline works without it. Providers add extraction proposals, chat reasoning, summarization, and Knowledge Hub answers when configured.

## Provider classes

| Provider class | Best use | Notes |
|:---|:---|:---|
| Local Ollama | Private local testing, offline demos, small/medium models. | Uses an OpenAI-compatible endpoint. |
| OpenAI-compatible hosted endpoint | Stronger chat and extraction assist behind a standard streaming shape. | Configure endpoint, model, and key outside source control. |
| HuggingFace cloud | Experiments with document or vision models. | Useful when comparing model families. |
| No provider | Default deterministic workflow. | Upload, OCR, extraction, reconciliation, review, and export remain usable. |

## Local models worth knowing

| Model family | Why it matters |
|:---|:---|
| Qwen VL family | Strong document and chart understanding for local vision tests. |
| Granite document models | Small document-focused models with layout/OCR relevance. |
| Llama family | General text reasoning and chat. |
| Gemma family | Smaller general chat models for constrained hardware. |

## Product rule

A model can assist; it does not become the source of truth. Proposals need provenance, confidence, and a conservative merge path. Reviewed deterministic values win.

## Interview line

"The product uses a provider-neutral model seam. Local Ollama is great for privacy demos, compatible hosted endpoints give stronger streaming models, and HuggingFace is useful for experiments. The important part is that none of those are required for the core workflow."
