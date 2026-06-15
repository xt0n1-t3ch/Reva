"use client";

import { useMemo, useState } from "react";
import { cn } from "@/lib/cn";
import { config } from "@/lib/config";
import type { BdxPage, SourceSpan } from "@/lib/api/types";
import { rectStyle, spanRect } from "@/lib/geometry";

const imageSrc = (url: string) => (url.startsWith("http") ? url : `${config.apiBaseUrl}${url}`);

export function DocumentCanvas({
  pages,
  spans,
  citedSpanIds,
  activeSpanIds,
}: {
  pages: BdxPage[];
  spans: SourceSpan[];
  citedSpanIds: Set<string>;
  activeSpanIds: Set<string>;
}) {
  const [failedPages, setFailedPages] = useState<Set<number>>(new Set());

  const spansByPage = useMemo(() => {
    const map = new Map<number, SourceSpan[]>();
    for (const span of spans) {
      const list = map.get(span.page) ?? [];
      list.push(span);
      map.set(span.page, list);
    }
    return map;
  }, [spans]);

  return (
    <div className="flex flex-col gap-4">
      {pages.map((page) => {
        const pageSpans = spansByPage.get(page.page) ?? [];
        const ratio = page.width > 1 && page.height > 1 ? page.width / page.height : undefined;
        if (failedPages.has(page.page)) {
          return (
            <figure
              key={page.page}
              className="flex min-h-48 flex-col items-center justify-center gap-2 rounded-md border border-dashed border-border bg-surface-2/30 px-6 py-10 text-center"
            >
              <p className="text-sm font-medium text-muted-foreground">Page preview unavailable</p>
              <p className="max-w-xs text-xs text-subtle-foreground">
                This page has no rendered image. Citations remain available in the field panel.
              </p>
            </figure>
          );
        }
        return (
          <figure
            key={page.page}
            className="relative overflow-hidden rounded-md border border-border bg-surface-2 shadow-soft"
            style={ratio ? { aspectRatio: ratio } : undefined}
          >
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={imageSrc(page.imageUrl)}
              alt={`Document page ${page.page}`}
              loading={page.page === 1 ? "eager" : "lazy"}
              onError={() => setFailedPages((current) => new Set(current).add(page.page))}
              className="block h-auto w-full select-none"
              draggable={false}
            />
            <div className="pointer-events-none absolute inset-0">
              {pageSpans.map((span) => {
                const active = activeSpanIds.has(span.id);
                const cited = citedSpanIds.has(span.id);
                if (!active && !cited) {
                  return null;
                }
                return (
                  <mark
                    key={span.id}
                    title={span.text}
                    style={rectStyle(spanRect(span))}
                    className={cn(
                      "absolute rounded-[2px] transition-colors duration-150",
                      active
                        ? "bg-citation-active ring-2 ring-citation-ring"
                        : "bg-citation ring-1 ring-citation-ring/40",
                    )}
                  />
                );
              })}
            </div>
            <figcaption className="absolute bottom-1.5 right-1.5 rounded bg-black/55 px-1.5 py-0.5 font-mono text-[10px] text-white tabular">
              {page.page}/{pages.length}
            </figcaption>
          </figure>
        );
      })}
    </div>
  );
}
