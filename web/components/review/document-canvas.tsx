"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { cn } from "@/lib/cn";
import { config } from "@/lib/config";
import type { BdxPage, SourceSpan } from "@/lib/api/types";
import { rectStyle, spanRect } from "@/lib/geometry";
import { IconChevronLeft, IconChevronRight, IconFit, IconZoomIn, IconZoomOut } from "@/components/ui/icons";

const imageSrc = (url: string) => (url.startsWith("http") ? url : `${config.apiBaseUrl}${url}`);

const zoomMin = 0.5;
const zoomMax = 3;
const zoomStep = 1.25;

function ToolbarButton({
  label,
  onClick,
  disabled,
  children,
}: {
  label: string;
  onClick: () => void;
  disabled?: boolean;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      aria-label={label}
      title={label}
      onClick={onClick}
      disabled={disabled}
      className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-surface-2 hover:text-foreground disabled:pointer-events-none disabled:opacity-40"
    >
      {children}
    </button>
  );
}

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
  const [zoom, setZoom] = useState(1);
  const [currentPage, setCurrentPage] = useState(pages[0]?.page ?? 1);
  const [failedPages, setFailedPages] = useState<Set<number>>(new Set());
  const [loadedPages, setLoadedPages] = useState<Set<number>>(new Set());
  const scrollRef = useRef<HTMLDivElement>(null);

  const firstPage = pages[0]?.page ?? 1;
  const lastPage = pages[pages.length - 1]?.page ?? 1;

  const spansByPage = useMemo(() => {
    const map = new Map<number, SourceSpan[]>();
    for (const span of spans) {
      const list = map.get(span.page) ?? [];
      list.push(span);
      map.set(span.page, list);
    }
    return map;
  }, [spans]);

  const goToPage = useCallback((page: number) => {
    scrollRef.current
      ?.querySelector(`[data-page="${page}"]`)
      ?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, []);

  // Track the most-visible page so the indicator and prev/next stay in sync with scrolling.
  useEffect(() => {
    const root = scrollRef.current;
    if (!root || pages.length < 2) {
      return;
    }
    const observer = new IntersectionObserver(
      (entries) => {
        const visible = entries
          .filter((entry) => entry.isIntersecting)
          .sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];
        const page = visible ? Number((visible.target as HTMLElement).dataset.page) : NaN;
        if (Number.isFinite(page)) {
          setCurrentPage(page);
        }
      },
      { root, threshold: [0.2, 0.5, 0.8] },
    );
    root.querySelectorAll("[data-page]").forEach((element) => observer.observe(element));
    return () => observer.disconnect();
  }, [pages]);

  // When a field is hovered/focused, bring its source region into view.
  useEffect(() => {
    if (activeSpanIds.size === 0) {
      return;
    }
    const target = spans.find((span) => activeSpanIds.has(span.id));
    if (target) {
      scrollRef.current
        ?.querySelector(`[data-page="${target.page}"]`)
        ?.scrollIntoView({ behavior: "smooth", block: "nearest" });
    }
  }, [activeSpanIds, spans]);

  return (
    <div className="flex h-full min-h-0 flex-col gap-2">
      <div className="flex shrink-0 items-center justify-between gap-2 rounded-md border border-border bg-surface px-2 py-1.5">
        {pages.length > 1 ? (
          <div className="flex items-center gap-1.5">
            <span className="hidden pl-1 text-[11px] font-semibold uppercase tracking-wider text-subtle-foreground sm:inline">
              Source
            </span>
            <span aria-hidden="true" className="hidden h-3.5 w-px bg-border sm:inline-block" />
            <div className="flex items-center gap-0.5">
              <ToolbarButton label="Previous page" onClick={() => goToPage(currentPage - 1)} disabled={currentPage <= firstPage}>
                <IconChevronLeft width={15} height={15} />
              </ToolbarButton>
              <span className="min-w-20 text-center font-mono text-xs tabular text-muted-foreground">
                Page {currentPage} / {pages.length}
              </span>
              <ToolbarButton label="Next page" onClick={() => goToPage(currentPage + 1)} disabled={currentPage >= lastPage}>
                <IconChevronRight width={15} height={15} />
              </ToolbarButton>
            </div>
          </div>
        ) : (
          <span className="px-1 text-[11px] font-semibold uppercase tracking-wider text-subtle-foreground">
            Source document
          </span>
        )}

        <div className="flex items-center gap-0.5">
          <ToolbarButton label="Zoom out" onClick={() => setZoom((value) => Math.max(zoomMin, value / zoomStep))} disabled={zoom <= zoomMin}>
            <IconZoomOut width={15} height={15} />
          </ToolbarButton>
          <button
            type="button"
            onClick={() => setZoom(1)}
            aria-label="Fit to width"
            title="Fit to width"
            className="flex items-center gap-1 rounded px-1.5 py-0.5 font-mono text-xs tabular text-muted-foreground transition-colors hover:bg-surface-2 hover:text-foreground"
          >
            <IconFit width={13} height={13} />
            {Math.round(zoom * 100)}%
          </button>
          <ToolbarButton label="Zoom in" onClick={() => setZoom((value) => Math.min(zoomMax, value * zoomStep))} disabled={zoom >= zoomMax}>
            <IconZoomIn width={15} height={15} />
          </ToolbarButton>
        </div>
      </div>

      <div ref={scrollRef} className="bg-dotgrid min-h-0 flex-1 overflow-auto rounded-md bg-surface-2/20 p-4 sm:p-6">
        <div className="mx-auto flex flex-col items-center gap-5" style={{ width: `${zoom * 100}%` }}>
          {pages.map((page) => {
            const ratio = page.width > 1 && page.height > 1 ? page.width / page.height : undefined;
            const pageSpans = spansByPage.get(page.page) ?? [];

            if (failedPages.has(page.page)) {
              return (
                <figure
                  key={page.page}
                  data-page={page.page}
                  className="bg-dotgrid flex min-h-48 w-full flex-col items-center justify-center gap-2 rounded-md border border-dashed border-border bg-surface-2/30 px-6 py-10 text-center"
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
                data-page={page.page}
                className="relative w-full overflow-hidden rounded-md border border-border bg-surface shadow-pop ring-1 ring-black/[0.03] transition-shadow dark:ring-white/[0.04]"
                style={ratio ? { aspectRatio: ratio } : undefined}
              >
                {!loadedPages.has(page.page) && (
                  <div className="absolute inset-0 animate-pulse bg-surface-3" aria-hidden="true" />
                )}
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={imageSrc(page.imageUrl)}
                  alt={`Document page ${page.page}`}
                  loading={page.page === firstPage ? "eager" : "lazy"}
                  onLoad={() => setLoadedPages((current) => new Set(current).add(page.page))}
                  onError={() => setFailedPages((current) => new Set(current).add(page.page))}
                  className="relative block h-auto w-full select-none"
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
                        data-active={active ? "true" : undefined}
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
                <figcaption className="pointer-events-none absolute bottom-2 right-2 rounded border border-white/10 bg-black/60 px-1.5 py-0.5 font-mono text-[10px] tabular text-white shadow-soft backdrop-blur-sm">
                  {page.page}/{pages.length}
                </figcaption>
              </figure>
            );
          })}
        </div>
      </div>
    </div>
  );
}
