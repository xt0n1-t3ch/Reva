"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { api } from "@/lib/api/client";
import type { BdxReviewPayload, Citation, ReconciliationCheck } from "@/lib/api/types";
import { useApi } from "@/lib/use-api";
import { Badge, Button } from "@/components/ui/primitives";
import { ErrorBanner, Skeleton } from "@/components/ui/states";
import {
  IconChevronRight,
  IconCheck,
  IconAlert,
  IconExport,
  IconClose,
} from "@/components/ui/icons";
import { reviewStateTone, humanizeEnum } from "@/lib/format";
import { DocumentCanvas } from "@/components/review/document-canvas";
import { SourceTextCanvas } from "@/components/review/source-text-canvas";
import { TemplateView } from "@/components/review/template-view";
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
  const [corrections, setCorrections] = useState<Record<string, string>>({});
  const [sourceTab, setSourceTab] = useState<"source" | "template">("source");

  const citedSpanIds = useMemo(() => (data ? collectCitedSpanIds(data) : new Set<string>()), [data]);
  const hasPageImages = useMemo(
    () => Boolean(data?.document.pages.some((page) => page.width > 1 && page.height > 1)),
    [data],
  );
  const correctionCount = Object.keys(corrections).length;

  const fieldLabelByKey = useMemo(() => {
    const map = new Map<string, string>();
    data?.fields.forEach((field) => map.set(field.key, field.label));
    return map;
  }, [data]);

  const fieldKeyByName = useMemo(() => {
    const map = new Map<string, string>();
    data?.fields.forEach((field) => map.set(field.label.replace(/\s+/g, "").toLowerCase(), field.key));
    return map;
  }, [data]);

  const applyExpected = (check: ReconciliationCheck) => {
    const key = fieldKeyByName.get(check.name.replace(/\s+/g, "").toLowerCase());
    if (key && check.expected.value) {
      setCorrection(key, check.expected.value);
    }
  };

  const appliedChecks = useMemo(() => {
    const names = new Set<string>();
    data?.reconciliation.forEach((check) => {
      const key = fieldKeyByName.get(check.name.replace(/\s+/g, "").toLowerCase());
      if (key && corrections[key] === check.expected.value) {
        names.add(check.name.replace(/\s+/g, "").toLowerCase());
      }
    });
    return names;
  }, [data, fieldKeyByName, corrections]);

  const activate = (spanIds: string[], values: string[]) => {
    setActiveSpanIds(new Set(spanIds));
    setActiveValues(values);
  };

  const setCorrection = (key: string, value: string) => {
    setCorrections((current) => ({ ...current, [key]: value }));
  };
  const clearCorrection = (key: string) => {
    setCorrections((current) => {
      if (!(key in current)) {
        return current;
      }
      const next = { ...current };
      delete next[key];
      return next;
    });
  };

  const decide = async (decision: string) => {
    setDecisionPending(decision);
    try {
      const fieldCorrections = Object.entries(corrections).map(([key, value]) => ({
        name: fieldLabelByKey.get(key) ?? key,
        value,
      }));
      await api.reviewDocument(documentId, {
        decision,
        reviewer: "Reviewer",
        notes:
          decision === "NeedsCorrection" && fieldCorrections.length > 0
            ? `${fieldCorrections.length} field correction${fieldCorrections.length === 1 ? "" : "s"} submitted.`
            : null,
        fieldCorrections,
        mappingCorrections: [],
      });
      setCorrections({});
      detail.refresh();
      refresh();
    } catch {
      // Surfaced via the detail banner on next load; keep the action area responsive.
    } finally {
      setDecisionPending(null);
    }
  };

  if (loading && !data) {
    return (
      <div className="flex h-full flex-col">
        <div className="flex shrink-0 items-center justify-between gap-3 border-b border-border bg-surface px-4 py-2.5">
          <Skeleton className="h-4 w-48" />
          <div className="flex items-center gap-1.5">
            <Skeleton className="h-7 w-20" />
            <Skeleton className="h-7 w-28" />
            <Skeleton className="h-7 w-24" />
          </div>
        </div>
        <div className="flex shrink-0 flex-col gap-2 border-b border-border bg-surface px-4 py-3">
          <Skeleton className="h-5 w-64" />
          <Skeleton className="h-4 w-40" />
        </div>
        <div className="grid min-h-0 flex-1 grid-cols-1 lg:grid-cols-[1.4fr_1fr]">
          <div className="bg-dotgrid border-b border-border bg-surface-2/30 p-4 lg:border-b-0 lg:border-r">
            <Skeleton className="h-full min-h-72 w-full" />
          </div>
          <div className="flex flex-col gap-px bg-surface p-4">
            {Array.from({ length: 7 }).map((_, index) => (
              <Skeleton key={index} className="h-12 w-full" />
            ))}
          </div>
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

  const busy = decisionPending !== null;

  return (
    <div className="flex h-full flex-col" data-tour="review-split-view">
      <header className="flex shrink-0 flex-wrap items-center gap-x-4 gap-y-2.5 border-b border-border bg-surface px-4 py-2.5">
        <nav
          className="flex min-w-0 items-center gap-1.5 text-sm text-muted-foreground"
          aria-label="Breadcrumb"
        >
          <Link href="/review" className="shrink-0 transition-colors hover:text-foreground">
            Review
          </Link>
          <IconChevronRight width={14} height={14} className="shrink-0 text-subtle-foreground" />
          <span className="truncate font-medium tracking-tight text-foreground">
            {data.document.filename}
          </span>
          {reviewState && (
            <Badge tone={reviewStateTone[reviewState]} className="ml-1 shrink-0">
              {humanizeEnum(reviewState)}
            </Badge>
          )}
        </nav>

        <div className="ml-auto flex items-center gap-1.5">
          {correctionCount > 0 && (
            <span className="mr-1 hidden items-center gap-1.5 rounded-full border border-warning/30 bg-warning-soft px-2 py-0.5 text-[11px] font-medium tabular text-warning sm:inline-flex">
              <span aria-hidden="true" className="size-1.5 rounded-full bg-warning" />
              {correctionCount} pending
            </span>
          )}

          <Button
            variant="ghost"
            size="sm"
            disabled={busy}
            onClick={() => void decide("Rejected")}
            className="text-danger hover:bg-danger-soft hover:text-danger"
          >
            <IconClose width={14} height={14} />
            {decisionPending === "Rejected" ? "Rejecting…" : "Reject"}
          </Button>

          <Button
            variant={correctionCount > 0 ? "primary" : "outline"}
            size="sm"
            disabled={busy}
            onClick={() => void decide("NeedsCorrection")}
          >
            <IconAlert width={14} height={14} />
            {decisionPending === "NeedsCorrection"
              ? "Submitting…"
              : correctionCount > 0
                ? `Submit ${correctionCount} correction${correctionCount === 1 ? "" : "s"}`
                : "Request changes"}
          </Button>

          <Button
            variant={correctionCount > 0 ? "outline" : "primary"}
            size="sm"
            disabled={busy}
            onClick={() => void decide("Approved")}
          >
            <IconCheck width={14} height={14} />
            {decisionPending === "Approved" ? "Approving…" : "Approve"}
          </Button>

          <span aria-hidden="true" className="mx-0.5 h-5 w-px bg-border" />

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
          onApplyExpected={applyExpected}
          appliedChecks={appliedChecks}
        />
      )}

      <div className="grid min-h-0 flex-1 grid-cols-1 lg:grid-cols-[1.4fr_1fr]">
        <div className="flex min-h-0 flex-col border-b border-border bg-surface-2/30 lg:border-b-0 lg:border-r">
          <div className="sticky top-0 z-20 flex items-center gap-1 border-b border-border bg-surface/95 px-4 py-2 backdrop-blur-sm">
            <div className="flex rounded-md border border-border bg-surface-2/60 p-0.5">
              {(["source", "template"] as const).map((tab) => (
                <button
                  key={tab}
                  type="button"
                  onClick={() => setSourceTab(tab)}
                  className={`rounded px-2.5 py-1 text-[11px] font-medium capitalize transition-colors ${
                    sourceTab === tab
                      ? "bg-surface text-foreground shadow-soft"
                      : "text-muted-foreground hover:text-foreground"
                  }`}
                >
                  {tab === "source" ? "Source" : "Template"}
                </button>
              ))}
            </div>
            <span className="ml-auto text-[10px] uppercase tracking-[0.08em] text-subtle-foreground">
              {sourceTab === "source" ? "As received" : "Filed · CRS-ordered"}
            </span>
          </div>
          <div className="min-h-0 flex-1 overflow-y-auto p-4">
            {sourceTab === "template" ? (
              <TemplateView
                fileName={data.document.filename}
                documentType={detail.data?.documentType ?? "Unknown"}
                fields={data.fields}
                lineItems={data.lineItems}
              />
            ) : hasPageImages ? (
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
        </div>
        <div className="flex min-h-0 flex-col overflow-y-auto bg-surface">
          <div className="sticky top-0 z-20 flex items-center justify-between gap-2 border-b border-border bg-surface/95 px-4 py-2 backdrop-blur-sm">
            <span className="text-[10.5px] font-semibold uppercase tracking-[0.09em] text-subtle-foreground">
              Extracted fields
            </span>
            <span className="font-mono text-[11px] tabular text-subtle-foreground">
              {data.fields.length}
            </span>
          </div>
          {correctionCount > 0 && (
            <div className="flex items-center justify-between gap-2 border-b border-warning/30 bg-warning-soft px-4 py-2 text-xs">
              <span className="flex items-center gap-2 font-medium text-warning">
                <span aria-hidden="true" className="size-1.5 shrink-0 rounded-full bg-warning" />
                {correctionCount} field{correctionCount === 1 ? "" : "s"} edited — submit to record the
                {correctionCount === 1 ? " correction." : " corrections."}
              </span>
              <Button
                variant="ghost"
                size="sm"
                className="h-6 shrink-0 px-2 text-warning hover:bg-warning/15"
                onClick={() => setCorrections({})}
                disabled={decisionPending !== null}
              >
                Discard
              </Button>
            </div>
          )}
          <FieldGroups
            fields={data.fields}
            onActivate={activate}
            activeSpanIds={activeSpanIds}
            corrections={corrections}
            onCorrect={setCorrection}
            onClearCorrection={clearCorrection}
            editingDisabled={decisionPending !== null}
          />
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
