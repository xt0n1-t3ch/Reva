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
  warning: "text-warning",
  danger: "text-danger",
  success: "text-success",
};

const toneAccent: Record<Kpi["tone"], string> = {
  neutral: "bg-border-strong",
  warning: "bg-warning",
  danger: "bg-danger",
  success: "bg-success",
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
    return <Skeleton className="h-[7.5rem] rounded-lg" />;
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border bg-border">
      <div className="grid grid-cols-2 gap-px lg:grid-cols-4">
        {computeKpis(documents).map((kpi) => (
          <div
            key={kpi.label}
            className="group/kpi relative bg-background p-4 transition-colors duration-200 hover:bg-surface-2/40 sm:p-5"
          >
            <span
              aria-hidden="true"
              className={`absolute left-0 top-4 h-7 w-0.5 rounded-full opacity-70 sm:top-5 ${toneAccent[kpi.tone]}`}
            />
            <p className="pl-3 text-[11px] font-medium uppercase tracking-[0.1em] text-subtle-foreground">
              {kpi.label}
            </p>
            <p
              className={`mt-3 pl-3 font-mono text-[2.125rem] font-semibold leading-none tracking-[-0.03em] tabular ${toneText[kpi.tone]}`}
            >
              {kpi.value}
            </p>
            <p className="mt-2.5 pl-3 text-xs text-muted-foreground">{kpi.hint}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
