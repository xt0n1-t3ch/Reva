import type { DocumentSummary } from "@/lib/api/types";
import { formatPercent } from "@/lib/format";
import { Skeleton } from "@/components/ui/states";

interface Kpi {
  label: string;
  value: string;
  hint: string;
  tone: "neutral" | "warning" | "danger" | "success";
}

const toneText: Record<Kpi["tone"], string> = {
  neutral: "text-foreground",
  warning: "text-warning-foreground",
  danger: "text-danger",
  success: "text-success",
};

const computeKpis = (documents: DocumentSummary[]): Kpi[] => {
  const pending = documents.filter((document) => document.reviewState === "Pending").length;
  const exceptions = documents.reduce((total, document) => total + document.exceptionCount, 0);
  const extracted = documents.filter((document) => document.confidence > 0);
  const avgConfidence = extracted.length
    ? extracted.reduce((total, document) => total + document.confidence, 0) / extracted.length
    : 0;

  return [
    { label: "Documents", value: String(documents.length), hint: "ingested", tone: "neutral" },
    { label: "Pending review", value: String(pending), hint: "awaiting sign-off", tone: pending > 0 ? "warning" : "neutral" },
    { label: "Open exceptions", value: String(exceptions), hint: "reconciliation flags", tone: exceptions > 0 ? "danger" : "success" },
    { label: "Avg confidence", value: extracted.length ? formatPercent(avgConfidence) : "–", hint: "extraction quality", tone: "neutral" },
  ];
};

export function KpiStrip({ documents, loading }: { documents: DocumentSummary[]; loading: boolean }) {
  if (loading) {
    return (
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        {Array.from({ length: 4 }).map((_, index) => (
          <Skeleton key={index} className="h-[4.75rem]" />
        ))}
      </div>
    );
  }

  return (
    <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
      {computeKpis(documents).map((kpi) => (
        <div key={kpi.label} className="rounded-lg border border-border bg-surface p-3.5 shadow-soft">
          <p className="text-[11px] font-medium uppercase tracking-wider text-subtle-foreground">
            {kpi.label}
          </p>
          <p className={`mt-1.5 font-mono text-2xl font-semibold tabular leading-none ${toneText[kpi.tone]}`}>
            {kpi.value}
          </p>
          <p className="mt-1.5 text-[11px] text-muted-foreground">{kpi.hint}</p>
        </div>
      ))}
    </div>
  );
}
