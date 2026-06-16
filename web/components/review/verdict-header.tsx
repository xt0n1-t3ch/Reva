"use client";

import { cn } from "@/lib/cn";
import type { FieldValue, ReconciliationCheck, ReinsuranceDocumentType } from "@/lib/api/types";
import {
  confidenceTier,
  documentTypeLabel,
  formatPercent,
  humanizeEnum,
  reconciliationTone,
} from "@/lib/format";
import { reconciliationDelta, reconciliationSummary } from "@/lib/review";
import { Badge, Dot } from "@/components/ui/primitives";
import { IconAlert, IconCheck } from "@/components/ui/icons";

const tierLabel = { high: "High", medium: "Medium", low: "Low" } as const;

interface ActivateProps {
  onActivate: (spanIds: string[], values: string[]) => void;
  activeSpanIds: Set<string>;
}

const findValue = (fields: FieldValue[], label: string): string | undefined =>
  fields.find((field) => field.label.replace(/\s+/g, "").toLowerCase() === label.replace(/\s+/g, "").toLowerCase())
    ?.value || undefined;

function VerdictRow({ check, onActivate, activeSpanIds }: { check: ReconciliationCheck } & ActivateProps) {
  const spanIds = check.citations.map((citation) => citation.sourceSpanId);
  const active = spanIds.some((id) => activeSpanIds.has(id));
  const delta = reconciliationDelta(check);
  const tone = reconciliationTone(check.status);

  return (
    <button
      type="button"
      onMouseEnter={() => onActivate(spanIds, [check.detected.value, check.expected.value])}
      onMouseLeave={() => onActivate([], [])}
      onFocus={() => onActivate(spanIds, [check.detected.value, check.expected.value])}
      onBlur={() => onActivate([], [])}
      className={cn(
        "grid grid-cols-[minmax(0,1fr)_auto] items-center gap-x-3 gap-y-1 rounded-md border border-border px-3 py-2 text-left transition-colors sm:grid-cols-[7rem_minmax(0,1fr)_auto]",
        active ? "border-primary-border bg-primary-soft" : "bg-surface hover:bg-surface-2/60",
      )}
    >
      <span className="text-sm font-medium">{humanizeEnum(check.name)}</span>
      <span className="col-span-2 flex flex-wrap items-baseline gap-x-2 font-mono text-xs tabular text-muted-foreground sm:col-span-1">
        <span>stated {check.detected.value}</span>
        <span className="text-subtle-foreground">vs expected {check.expected.value}</span>
      </span>
      <span className="flex items-center justify-end gap-2">
        {delta && delta.direction !== "flat" && (
          <span
            className={cn(
              "rounded px-1.5 py-0.5 font-mono text-xs font-semibold tabular",
              tone === "danger" ? "bg-danger-soft text-danger" : "bg-warning-soft text-warning-foreground",
            )}
          >
            {delta.text}
          </span>
        )}
        <Badge tone={tone}>{check.status}</Badge>
      </span>
    </button>
  );
}

export function VerdictHeader({
  documentType,
  confidence,
  fields,
  reconciliation,
  onActivate,
  activeSpanIds,
}: {
  documentType: ReinsuranceDocumentType;
  confidence: number;
  fields: FieldValue[];
  reconciliation: ReconciliationCheck[];
} & ActivateProps) {
  const { failing, total } = reconciliationSummary(reconciliation);
  const cedent = findValue(fields, "Cedent");
  const period = findValue(fields, "Period");
  const tier = confidenceTier(confidence);
  const clean = failing === 0;

  return (
    <section className="flex flex-col gap-3 border-b border-border bg-surface px-4 py-3">
      <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
        <Badge tone="primary" className="uppercase tracking-wide">
          {documentTypeLabel(documentType)}
        </Badge>
        {cedent && <span className="text-sm font-semibold">{cedent}</span>}
        {period && <span className="text-sm text-muted-foreground">· {period}</span>}
        <span className="ml-auto flex items-center gap-2 text-xs text-muted-foreground">
          <Dot tone={tier === "high" ? "success" : tier === "medium" ? "warning" : "danger"} />
          {tierLabel[tier]} confidence · <span className="font-mono tabular">{formatPercent(confidence)}</span>
        </span>
      </div>

      {total > 0 && (
        <div className="flex flex-col gap-2" data-tour="reconciliation-panel">
          <div className="flex items-center gap-2 text-sm font-medium">
            {clean ? (
              <>
                <span className="flex size-5 items-center justify-center rounded-full bg-success-soft text-success">
                  <IconCheck width={13} height={13} />
                </span>
                <span>All {total} control totals reconcile</span>
              </>
            ) : (
              <>
                <span className="flex size-5 items-center justify-center rounded-full bg-danger-soft text-danger">
                  <IconAlert width={13} height={13} />
                </span>
                <span>
                  {failing} of {total} control totals don&apos;t reconcile
                </span>
              </>
            )}
          </div>
          {!clean && (
            <div className="grid gap-1.5">
              {reconciliation.map((check) => (
                <VerdictRow
                  key={check.id}
                  check={check}
                  onActivate={onActivate}
                  activeSpanIds={activeSpanIds}
                />
              ))}
            </div>
          )}
        </div>
      )}
    </section>
  );
}
