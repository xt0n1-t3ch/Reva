import type { UIMessage } from "ai";

/**
 * The active cloud model's context window. The agent runs on a ~262K-token
 * cloud model; this is the denominator for the context gauge. Override at
 * runtime with NEXT_PUBLIC_CHAT_CONTEXT_TOKENS if the deployment swaps models.
 */
const parsedContext = Number(process.env.NEXT_PUBLIC_CHAT_CONTEXT_TOKENS);
export const CHAT_CONTEXT_TOKENS =
  Number.isFinite(parsedContext) && parsedContext > 0 ? parsedContext : 262_144;

export type ChatUsage = {
  /** Tokens consumed by the prompt (input), when reported by the server. */
  promptTokens?: number;
  /** Tokens produced by the completion (output), when reported by the server. */
  completionTokens?: number;
  /** Total tokens for the turn. Always populated (measured or estimated). */
  totalTokens: number;
  /** True when totalTokens is a heuristic estimate, not a server figure. */
  estimated: boolean;
};

/**
 * Roughly four characters per token for English prose — the standard
 * back-of-envelope used when the model layer does not report real usage.
 */
const CHARS_PER_TOKEN = 4;

export const estimateTokensFromText = (text: string): number =>
  text.length === 0 ? 0 : Math.max(1, Math.round(text.length / CHARS_PER_TOKEN));

const asNumber = (value: unknown): number | undefined =>
  typeof value === "number" && Number.isFinite(value) ? value : undefined;

/**
 * AI SDK message metadata is server-defined. We probe the common shapes the
 * UI-message stream uses when a `message-metadata` part carries usage, without
 * coupling to one exact schema: { usage: { totalTokens, promptTokens, … } } or
 * flat { totalTokens, … } / { inputTokens, outputTokens }.
 */
const readMetadataUsage = (metadata: unknown): ChatUsage | null => {
  if (metadata == null || typeof metadata !== "object") {
    return null;
  }
  const record = metadata as Record<string, unknown>;
  const usage = (record.usage ?? record) as Record<string, unknown>;

  const promptTokens = asNumber(usage.promptTokens) ?? asNumber(usage.inputTokens);
  const completionTokens = asNumber(usage.completionTokens) ?? asNumber(usage.outputTokens);
  const totalTokens =
    asNumber(usage.totalTokens) ??
    (promptTokens != null || completionTokens != null
      ? (promptTokens ?? 0) + (completionTokens ?? 0)
      : undefined);

  if (totalTokens == null) {
    return null;
  }
  return { promptTokens, completionTokens, totalTokens, estimated: false };
};

const textLength = (message: UIMessage): number =>
  message.parts.reduce((sum, part) => {
    if (part.type === "text" || part.type === "reasoning") {
      return sum + (typeof part.text === "string" ? part.text.length : 0);
    }
    return sum;
  }, 0);

/**
 * Resolve usage for the whole conversation. Prefers real server-reported usage
 * carried on the latest assistant message's metadata; falls back to a clearly
 * flagged estimate derived from the total characters streamed so far.
 */
export const resolveChatUsage = (messages: UIMessage[]): ChatUsage => {
  for (let i = messages.length - 1; i >= 0; i -= 1) {
    const message = messages[i];
    if (message.role !== "assistant") {
      continue;
    }
    const reported = readMetadataUsage(message.metadata);
    if (reported) {
      return reported;
    }
    break;
  }

  const totalChars = messages.reduce((sum, message) => sum + textLength(message), 0);
  return { totalTokens: estimateTokensFromText("x".repeat(totalChars)), estimated: true };
};

/** Compact mono-friendly token label: 980, 12.4K, 1.2M. */
export const formatTokens = (value: number): string => {
  if (value < 1_000) {
    return String(Math.round(value));
  }
  if (value < 1_000_000) {
    const thousands = value / 1_000;
    return `${thousands >= 100 ? Math.round(thousands) : thousands.toFixed(1)}K`;
  }
  return `${(value / 1_000_000).toFixed(2)}M`;
};

/** Duration in seconds with one decimal, e.g. "2.4s"; sub-second stays precise. */
export const formatDuration = (ms: number): string => {
  if (ms < 1_000) {
    return `${Math.round(ms)}ms`;
  }
  return `${(ms / 1_000).toFixed(1)}s`;
};
