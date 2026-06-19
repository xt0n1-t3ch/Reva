"use client";

import { useMemo } from "react";
import { highlightSegments } from "@/lib/highlight";
import { IconDocument } from "@/components/ui/icons";

export function SourceTextCanvas({
  text,
  activeValues,
}: {
  text: string;
  activeValues: string[];
}) {
  const segments = useMemo(() => highlightSegments(text, activeValues), [text, activeValues]);
  const lineCount = useMemo(() => (text ? text.split("\n").length : 0), [text]);

  if (!text.trim()) {
    return (
      <div className="bg-dotgrid flex h-full min-h-72 items-center justify-center rounded-md border border-dashed border-border bg-surface-2/30 px-6 py-12 text-center text-sm text-muted-foreground">
        No parsed source available for this document.
      </div>
    );
  }

  return (
    <div className="flex h-full min-h-0 flex-col gap-3">
      <div className="flex shrink-0 items-center justify-between gap-2 rounded-md border border-border bg-surface px-2.5 py-1.5">
        <span className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-subtle-foreground">
          <IconDocument width={13} height={13} className="text-muted-foreground" />
          Source document
        </span>
        <span className="hidden items-center gap-2 text-[11px] text-subtle-foreground sm:flex">
          <span className="font-mono tabular">{lineCount} lines</span>
          <span aria-hidden="true" className="h-3 w-px bg-border" />
          Hover a field to locate its source
        </span>
      </div>

      {/* Paper surface: a soft-shadowed sheet floating on the dot-grid desk. */}
      <div className="bg-dotgrid min-h-0 flex-1 overflow-auto rounded-md bg-surface-2/20 p-4 sm:p-6">
        <div className="mx-auto max-w-[68ch]">
          <div className="overflow-hidden rounded-md border border-border bg-surface shadow-pop ring-1 ring-black/[0.02] dark:ring-white/[0.03]">
            <div className="flex items-center gap-1.5 border-b border-border bg-surface-2/40 px-4 py-2">
              <span aria-hidden="true" className="size-2 rounded-full bg-border-strong" />
              <span aria-hidden="true" className="size-2 rounded-full bg-border-strong/70" />
              <span aria-hidden="true" className="size-2 rounded-full bg-border-strong/50" />
              <span className="ml-2 truncate font-mono text-[10px] uppercase tracking-[0.12em] text-subtle-foreground">
                parsed source
              </span>
            </div>
            <pre className="whitespace-pre-wrap break-words px-6 py-5 font-mono text-[12.5px] leading-[1.85] text-foreground selection:bg-citation-active sm:px-8">
              {segments.map((segment, index) =>
                segment.active ? (
                  <mark
                    key={index}
                    data-active="true"
                    className="rounded-[3px] bg-citation-active px-0.5 text-foreground shadow-[0_0_0_1px_var(--color-citation-ring)] ring-1 ring-citation-ring"
                  >
                    {segment.text}
                  </mark>
                ) : (
                  <span key={index}>{segment.text}</span>
                ),
              )}
            </pre>
          </div>
        </div>
      </div>
    </div>
  );
}
