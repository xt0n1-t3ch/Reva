"use client";

import { useState } from "react";
import { cn } from "@/lib/cn";
import { api } from "@/lib/api/client";
import type { KnowledgeArticle } from "@/lib/api/types";
import { useApi } from "@/lib/use-api";
import { Markdown } from "@/lib/markdown";
import { PageContainer, PageHeader, SectionCard } from "@/components/ui/page";
import { ErrorBanner, Skeleton } from "@/components/ui/states";
import { Input } from "@/components/ui/form";
import { IconBook, IconSearch } from "@/components/ui/icons";

export function KnowledgeView() {
  const list = useApi((signal) => api.listKnowledge(signal));
  const [query, setQuery] = useState("");
  const [slug, setSlug] = useState<string | null>(null);

  const articles = list.data ?? [];
  const term = query.trim().toLowerCase();
  const filtered = term
    ? articles.filter((article) =>
        `${article.title} ${article.summary} ${article.category}`.toLowerCase().includes(term),
      )
    : articles;
  const activeSlug = (slug && filtered.some((a) => a.slug === slug) ? slug : null) ?? filtered[0]?.slug ?? null;
  const article = useApi<KnowledgeArticle | null>(
    (signal) => (activeSlug ? api.getKnowledge(activeSlug, signal) : Promise.resolve(null)),
    [activeSlug],
  );
  const categories = Array.from(new Set(filtered.map((a) => a.category)));

  return (
    <PageContainer className="max-w-[1120px]">
      <PageHeader
        title="Knowledge"
        subtitle="How to use Reva and the reinsurance standards it applies — searchable, and grounded into the Assistant so you can ask it anything here."
      />

      <div className="grid gap-5 lg:grid-cols-[290px_minmax(0,1fr)]">
        <div className="flex flex-col gap-3">
          <div className="relative">
            <IconSearch
              width={15}
              height={15}
              className="pointer-events-none absolute left-2.5 top-1/2 -translate-y-1/2 text-subtle-foreground"
            />
            <Input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search the knowledge base"
              className="pl-8"
              aria-label="Search the knowledge base"
            />
          </div>

          {list.loading && !list.data ? (
            <div className="flex flex-col gap-2">
              <Skeleton className="h-14" />
              <Skeleton className="h-14" />
              <Skeleton className="h-14" />
            </div>
          ) : list.error ? (
            <ErrorBanner message={list.error} onRetry={list.refresh} />
          ) : (
            <nav className="flex flex-col gap-4" aria-label="Knowledge articles">
              {categories.map((category) => (
                <div key={category} className="flex flex-col gap-1">
                  <p className="px-1 text-[10.5px] font-semibold uppercase tracking-[0.09em] text-subtle-foreground">
                    {category}
                  </p>
                  {filtered
                    .filter((a) => a.category === category)
                    .map((a) => {
                      const active = a.slug === activeSlug;
                      return (
                        <button
                          key={a.slug}
                          type="button"
                          onClick={() => setSlug(a.slug)}
                          aria-current={active ? "true" : undefined}
                          className={cn(
                            "rounded-md border px-3 py-2 text-left transition-colors",
                            active
                              ? "border-primary-border bg-primary-soft"
                              : "border-transparent hover:border-border hover:bg-surface-2/60",
                          )}
                        >
                          <span
                            className={cn(
                              "block text-sm font-medium tracking-tight",
                              active ? "text-foreground" : "text-muted-foreground",
                            )}
                          >
                            {a.title}
                          </span>
                          <span className="mt-0.5 line-clamp-2 block text-[11px] leading-snug text-subtle-foreground">
                            {a.summary}
                          </span>
                        </button>
                      );
                    })}
                </div>
              ))}
              {filtered.length === 0 && (
                <p className="px-1 text-xs text-subtle-foreground">No articles match “{query}”.</p>
              )}
            </nav>
          )}
        </div>

        <SectionCard
          fill
          title={article.data?.category ?? "Article"}
          meta={
            article.data ? (
              <span className="inline-flex items-center gap-1.5">
                <IconBook width={13} height={13} />
                Grounded into the Assistant
              </span>
            ) : undefined
          }
        >
          <div className="p-5 sm:p-6">
            {article.loading && !article.data ? (
              <div className="flex flex-col gap-3">
                <Skeleton className="h-6 w-1/2" />
                <Skeleton className="h-4" />
                <Skeleton className="h-4" />
                <Skeleton className="h-4 w-3/4" />
              </div>
            ) : article.error ? (
              <ErrorBanner message={article.error} onRetry={article.refresh} />
            ) : article.data ? (
              <Markdown content={article.data.content} />
            ) : (
              <p className="text-sm text-muted-foreground">Select an article to read it.</p>
            )}
          </div>
        </SectionCard>
      </div>
    </PageContainer>
  );
}
