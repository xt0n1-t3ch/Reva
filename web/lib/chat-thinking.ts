/**
 * Reasoning-effort levels for the assistant. The chosen level is persisted in
 * localStorage and sent to the backend on every request via the
 * `x-reva-reasoning` header; the backend maps each value per provider
 * (off -> no reasoning, low/medium/high -> reasoning_effort, max -> highest).
 */
export const THINKING_LEVELS = ["off", "low", "medium", "high", "max"] as const;

export type ThinkingLevel = (typeof THINKING_LEVELS)[number];

/** localStorage key for the persisted reasoning level. */
export const THINKING_LEVEL_STORAGE_KEY = "reva-chat-thinking-level";

/** HTTP header the chosen level is sent under on each /api/agent request. */
export const REASONING_HEADER = "x-reva-reasoning";

export const DEFAULT_THINKING_LEVEL: ThinkingLevel = "medium";

const LEVEL_LABELS: Record<ThinkingLevel, string> = {
  off: "Off",
  low: "Low",
  medium: "Medium",
  high: "High",
  max: "Max",
};

export const thinkingLevelLabel = (level: ThinkingLevel): string => LEVEL_LABELS[level];

export const isThinkingLevel = (value: unknown): value is ThinkingLevel =>
  typeof value === "string" && (THINKING_LEVELS as readonly string[]).includes(value);

export const parseThinkingLevel = (raw: string | null | undefined): ThinkingLevel =>
  isThinkingLevel(raw) ? raw : DEFAULT_THINKING_LEVEL;

/**
 * Read the persisted level straight from localStorage. Used at request time by
 * the chat transport so the chosen level is always current without holding it
 * in render-scope state. SSR-safe (returns the default when storage is absent).
 */
export const readThinkingLevel = (): ThinkingLevel => {
  if (typeof window === "undefined") {
    return DEFAULT_THINKING_LEVEL;
  }
  try {
    return parseThinkingLevel(window.localStorage.getItem(THINKING_LEVEL_STORAGE_KEY));
  } catch {
    return DEFAULT_THINKING_LEVEL;
  }
};

/** Off hides the reasoning UI entirely; every other level shows it. */
export const thinkingShowsReasoning = (level: ThinkingLevel): boolean => level !== "off";
