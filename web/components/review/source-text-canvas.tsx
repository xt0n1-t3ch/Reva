"use client";

import { useMemo } from "react";
import { highlightSegments } from "@/lib/highlight";

export function SourceTextCanvas({
  text,
  activeValues,
}: {
  text: string;
  activeValues: string[];
}) {
  const segments = useMemo(() => highlightSegments(text, activeValues), [text, activeValues]);

  if (!text.trim()) {
    return (
      <div className="flex h-full items-center justify-center rounded-md border border-dashed border-border bg-surface-2/30 px-6 py-12 text-center text-sm text-muted-foreground">
        No parsed source available for this document.
      </div>
    );
  }

  return (
    <div className="rounded-md border border-border bg-surface shadow-soft">
      <div className="flex items-center justify-between border-b border-border px-3 py-1.5">
        <span className="text-[11px] font-semibold uppercase tracking-wider text-subtle-foreground">
          Source document
        </span>
        <span className="text-[11px] text-subtle-foreground">Hover a field to locate its source</span>
      </div>
      <pre className="max-h-full overflow-x-auto whitespace-pre-wrap break-words px-4 py-3 font-mono text-[12.5px] leading-relaxed text-foreground">
        {segments.map((segment, index) =>
          segment.active ? (
            <mark
              key={index}
              data-active="true"
              className="rounded-[2px] bg-citation-active px-0.5 text-foreground ring-1 ring-citation-ring"
            >
              {segment.text}
            </mark>
          ) : (
            <span key={index}>{segment.text}</span>
          ),
        )}
      </pre>
    </div>
  );
}
