"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { api } from "@/lib/api/client";
import type { BdxReviewPayload, Citation } from "@/lib/api/types";
import { useApi } from "@/lib/use-api";
import { Badge, Button } from "@/components/ui/primitives";
import { ErrorBanner, Skeleton } from "@/components/ui/states";
import { IconChevronRight, IconCheck, IconAlert, IconExport } from "@/components/ui/icons";
import { reviewStateTone, humanizeEnum } from "@/lib/format";
import { DocumentCanvas } from "@/components/review/document-canvas";
import { SourceTextCanvas } from "@/components/review/source-text-canvas";
import { VerdictHeader } from "@/components/review/verdict-header";
import { FieldGroups } from "@/components/review/field-groups";
import { LineItems } from "@/components/review/line-items";
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
  const [decisionPending, setDecisionPending] = useState<string | null>(null);

  const citedSpanIds = useMemo(() => (data ? collectCitedSpanIds(data) : new Set<string>()), [data]);
  const hasPageImages = useMemo(
    () => Boolean(data?.document.pages.some((page) => page.width > 1 && page.height > 1)),
    [data],
  );

  const activate = (spanIds: string[], values: string[]) => {
    setActiveSpanIds(new Set(spanIds));
    setActiveValues(values);
  };

  const decide = async (decision: string) => {
    setDecisionPending(decision);
    try {
      await api.reviewDocument(documentId, {
        decision,
        reviewer: "Reviewer",
        notes: null,
        fieldCorrections: [],
        mappingCorrections: [],
      });
      detail.refresh();
    } catch {
      // Surfaced via the detail banner on next load; keep the action area responsive.
    } finally {
      setDecisionPending(null);
    }
  };

  if (loading && !data) {
    return (
      <div className="flex h-full flex-col gap-4 p-4">
        <Skeleton className="h-24" />
        <div className="grid flex-1 grid-cols-1 gap-4 lg:grid-cols-[1.4fr_1fr]">
          <Skeleton className="h-full min-h-72" />
          <Skeleton className="h-full min-h-72" />
        </div>
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="p-4">
        <ErrorBanner message={error ?? "Document not found."} onRetry={refresh} />
        <Link href="/review" className="mt-3 inline-block text-xs text-primary underline">
          Back to documents
        </Link>
      </div>
    );
  }

  const reviewState = detail.data?.reviewState;

  return (
    <div className="flex h-full flex-col" data-tour="review-split-view">
      <header className="flex shrink-0 flex-wrap items-center gap-3 border-b border-border bg-surface px-4 py-2.5">
        <nav className="flex min-w-0 items-center gap-1 text-sm text-muted-foreground" aria-label="Breadcrumb">
          <Link href="/review" className="hover:text-foreground">
            Review
          </Link>
          <IconChevronRight width={14} height={14} className="text-subtle-foreground" />
          <span className="truncate font-medium text-foreground">{data.document.filename}</span>
        </nav>
        <div className="ml-auto flex items-center gap-2">
          {reviewState && <Badge tone={reviewStateTone[reviewState]}>{humanizeEnum(reviewState)}</Badge>}
          <Button
            variant="outline"
            size="sm"
            disabled={decisionPending !== null}
            onClick={() => void decide("NeedsCorrection")}
          >
            <IconAlert width={14} height={14} />
            Request changes
          </Button>
          <Button
            variant="primary"
            size="sm"
            disabled={decisionPending !== null}
            onClick={() => void decide("Approved")}
          >
            <IconCheck width={14} height={14} />
            Approve
          </Button>
          <a href={api.exportUrl(documentId, "json")} target="_blank" rel="noreferrer">
            <Button variant="ghost" size="icon" aria-label="Export">
              <IconExport width={16} height={16} />
            </Button>
          </a>
        </div>
      </header>

      {detail.data && (
        <VerdictHeader
          documentType={detail.data.documentType}
          confidence={detail.data.confidence}
          fields={data.fields}
          reconciliation={data.reconciliation}
          onActivate={activate}
          activeSpanIds={activeSpanIds}
        />
      )}

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
          <FieldGroups fields={data.fields} onActivate={activate} activeSpanIds={activeSpanIds} />
          <LineItems lineItems={data.lineItems} onActivate={activate} activeSpanIds={activeSpanIds} />
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
