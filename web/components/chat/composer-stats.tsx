"use client";

import { cn } from "@/lib/cn";
import {
  CHAT_CONTEXT_TOKENS,
  formatDuration,
  formatTokens,
  type ChatUsage,
} from "@/lib/chat-usage";
import { IconClock } from "@/components/chat/chat-icons";

/**
 * A small circular gauge for context-window utilisation. Pure SVG ring so it
 * stays crisp at any DPI and tints with the semantic accent / warning / danger
 * tokens as the window fills.
 */
function ContextRing({ ratio, label }: { ratio: number; label: string }) {
  const clamped = Math.max(0, Math.min(1, ratio));
  const size = 30;
  const stroke = 3;
  const radius = (size - stroke) / 2;
  const circumference = 2 * Math.PI * radius;
  const dash = circumference * clamped;
  const tint = clamped >= 0.9 ? "text-danger" : clamped >= 0.75 ? "text-warning" : "text-primary";

  return (
    <span
      className="relative inline-flex shrink-0 items-center justify-center"
      role="meter"
      aria-valuenow={Math.round(clamped * 100)}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-label={label}
      title={label}
    >
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} className="-rotate-90">
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          strokeWidth={stroke}
          className="stroke-border"
        />
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={`${dash} ${circumference}`}
          className={cn("transition-[stroke-dasharray] duration-500 ease-out", tint)}
          stroke="currentColor"
        />
      </svg>
      <span className="absolute font-mono text-[8px] font-semibold tabular text-muted-foreground">
        {Math.round(clamped * 100)}
      </span>
    </span>
  );
}

export function ComposerStats({
  usage,
  durationMs,
  running,
}: {
  usage: ChatUsage;
  durationMs: number | null;
  running: boolean;
}) {
  const ratio = usage.totalTokens / CHAT_CONTEXT_TOKENS;
  const usedLabel = formatTokens(usage.totalTokens);
  const maxLabel = formatTokens(CHAT_CONTEXT_TOKENS);
  const ringLabel = `Context window ${usedLabel} of ${maxLabel} tokens used`;

  return (
    <div className="flex items-center gap-3 px-1 pb-2 text-[11px] text-muted-foreground">
      <ContextRing ratio={ratio} label={ringLabel} />
      <div className="flex min-w-0 flex-col leading-tight">
        <span className="font-mono tabular text-foreground">
          {usedLabel}
          <span className="text-subtle-foreground"> / {maxLabel}</span>
        </span>
        <span className="text-[10px] uppercase tracking-wide text-subtle-foreground">
          context{usage.estimated ? " · est" : ""}
        </span>
      </div>

      {(usage.promptTokens != null || usage.completionTokens != null) && (
        <div className="hidden min-w-0 flex-col leading-tight sm:flex">
          <span className="font-mono tabular text-foreground">
            {usage.promptTokens != null ? formatTokens(usage.promptTokens) : "—"}
            <span className="text-subtle-foreground"> in</span>
            {" · "}
            {usage.completionTokens != null ? formatTokens(usage.completionTokens) : "—"}
            <span className="text-subtle-foreground"> out</span>
          </span>
          <span className="text-[10px] uppercase tracking-wide text-subtle-foreground">tokens</span>
        </div>
      )}

      <div className="ml-auto flex items-center gap-1.5 font-mono tabular">
        <IconClock
          width={12}
          height={12}
          className={cn("shrink-0", running ? "animate-spin text-primary" : "text-subtle-foreground")}
        />
        <span className={cn(running ? "text-primary" : "text-foreground")}>
          {durationMs != null ? formatDuration(durationMs) : "—"}
        </span>
      </div>
    </div>
  );
}
