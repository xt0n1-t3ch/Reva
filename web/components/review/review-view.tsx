"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { api } from "@/lib/api/client";
import type { BdxReviewPayload, Citation } from "@/lib/api/types";
import { useApi } from "@/lib/use-api";
import { Button } from "@/components/ui/primitives";
import { ErrorBanner, Skeleton } from "@/components/ui/states";
import { IconChevronRight, IconExport } from "@/components/ui/icons";
import { DocumentCanvas } from "@/components/review/document-canvas";
import { SourceTextCanvas } from "@/components/review/source-text-canvas";
import { ReviewDetails } from "@/components/review/review-details";
import { DocPicker } from "@/components/review/doc-picker";

const collectCitedSpanIds = (payload: BdxReviewPayload): Set<string> => {
  const ids = new Set<string>();
  const add = (citations: Citation[]) => citations.forEach((citation) => ids.add(citation.sourceSpanId));
  payload.fields.forEach((field) => add(field.provenance.citations));
  payload.reconciliation.forEach((check) => add(check.citations));
  payload.lineItems.forEach((item) => item.fields.forEach((field) => add(field.provenance.citations)));
  return ids;
};

function SplitView({ documentId }: { documentId: string }) {
  const { data, error, loading, refresh } = useApi(
    (signal) => api.getReviewPayload(documentId, signal),
    [documentId],
  );
  const detail = useApi((signal) => api.getDocument(documentId, signal), [documentId]);
  const [activeSpanIds, setActiveSpanIds] = useState<Set<string>>(new Set());
  const [activeValues, setActiveValues] = useState<string[]>([]);

  const citedSpanIds = useMemo(() => (data ? collectCitedSpanIds(data) : new Set<string>()), [data]);
  const hasPageImages = useMemo(
    () => Boolean(data?.document.pages.some((page) => page.width > 1 && page.height > 1)),
    [data],
  );

  const activate = (spanIds: string[], values: string[]) => {
    setActiveSpanIds(new Set(spanIds));
    setActiveValues(values);
  };

  if (loading && !data) {
    return (
      <div className="grid h-full grid-cols-1 gap-4 p-4 lg:grid-cols-[1.4fr_1fr]">
        <Skeleton className="h-full min-h-72" />
        <Skeleton className="h-full min-h-72" />
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="p-4">
        <ErrorBanner message={error ?? "Document not found."} onRetry={refresh} />
        <Link href="/review" className="mt-3 inline-block text-xs text-primary hover:underline">
          Back to documents
        </Link>
      </div>
    );
  }

  return (
    <div className="flex h-full flex-col">
      <header className="flex shrink-0 flex-wrap items-center gap-3 border-b border-border bg-surface px-4 py-2.5">
        <nav className="flex min-w-0 items-center gap-1 text-sm text-muted-foreground" aria-label="Breadcrumb">
          <Link href="/review" className="hover:text-foreground">
            Review
          </Link>
          <IconChevronRight width={14} height={14} className="text-subtle-foreground" />
          <span className="truncate font-medium text-foreground">{data.document.filename}</span>
        </nav>
        <div className="ml-auto flex items-center gap-2">
          <a href={api.exportUrl(documentId, "json")} target="_blank" rel="noreferrer">
            <Button variant="outline" size="sm">
              <IconExport width={14} height={14} />
              Export
            </Button>
          </a>
        </div>
      </header>

      <div className="grid min-h-0 flex-1 grid-cols-1 lg:grid-cols-[1.4fr_1fr]">
        <div className="min-h-0 overflow-y-auto border-b border-border bg-surface-2/30 p-4 lg:border-b-0 lg:border-r">
          {hasPageImages ? (
            <DocumentCanvas
              pages={data.document.pages}
              spans={data.sourceSpans}
              citedSpanIds={citedSpanIds}
              activeSpanIds={activeSpanIds}
            />
          ) : (
            <SourceTextCanvas text={detail.data?.parsedMarkdown ?? ""} activeValues={activeValues} />
          )}
        </div>
        <div className="min-h-0 overflow-y-auto bg-surface">
          <ReviewDetails
            fields={data.fields}
            reconciliation={data.reconciliation}
            onActivate={activate}
            activeSpanIds={activeSpanIds}
          />
        </div>
      </div>
    </div>
  );
}

export function ReviewView() {
  const params = useSearchParams();
  const documentId = params.get("doc");
  return documentId ? <SplitView documentId={documentId} /> : <DocPicker />;
}
