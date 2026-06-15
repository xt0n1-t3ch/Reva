"use client";

import { api } from "@/lib/api/client";
import { useApi } from "@/lib/use-api";
import { Dot } from "@/components/ui/primitives";
import { IconMail } from "@/components/ui/icons";

export function InboundStatus() {
  const { data } = useApi((signal) => api.listInboundSources(signal));
  const sources = data ?? [];
  const enabled = sources.filter((source) => source.enabled).length;

  return (
    <details className="group relative">
      <summary className="flex cursor-pointer list-none items-center gap-2 rounded-md border border-border bg-surface-2 px-2.5 py-1.5 text-xs text-muted-foreground transition-colors hover:text-foreground">
        <IconMail width={14} height={14} />
        <span className="font-medium">Sources</span>
        <Dot tone={enabled > 0 ? "success" : "warning"} />
        <span className="tabular">{enabled}/{sources.length || "–"}</span>
      </summary>
      <div className="absolute right-0 z-30 mt-2 w-72 rounded-lg border border-border bg-surface p-2 shadow-pop">
        <p className="px-1.5 pb-1.5 text-[11px] font-semibold uppercase tracking-wider text-subtle-foreground">
          Inbound channels
        </p>
        <ul className="flex flex-col gap-0.5">
          {sources.map((source) => (
            <li key={source.name} className="flex items-start gap-2 rounded-md px-1.5 py-1.5">
              <Dot tone={source.enabled ? "success" : "neutral"} className="mt-1.5" />
              <span className="flex min-w-0 flex-col">
                <span className="text-sm font-medium capitalize">{source.name}</span>
                <span className="text-[11px] leading-snug text-muted-foreground">{source.detail}</span>
              </span>
            </li>
          ))}
          {sources.length === 0 && (
            <li className="px-1.5 py-2 text-xs text-muted-foreground">No channels reported.</li>
          )}
        </ul>
      </div>
    </details>
  );
}
