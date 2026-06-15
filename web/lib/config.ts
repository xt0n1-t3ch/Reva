const trimTrailingSlash = (value: string): string => value.replace(/\/+$/, "");

export const config = {
  apiBaseUrl: trimTrailingSlash(process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5187"),
  ollamaBaseUrl: trimTrailingSlash(process.env.OLLAMA_BASE_URL ?? "http://localhost:11434/api"),
  ollamaModel: process.env.OLLAMA_MODEL ?? "qwen3-vl:8b",
  agentMaxSteps: 6,
  agentTemperature: 0,
  themeStorageKey: "reva-theme",
  productName: "Reve Intelligence",
} as const;

export const confidenceThresholds = {
  lowMax: 0.6,
  mediumMax: 0.85,
} as const;
