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

function VerdictRow({
  check,
  onActivate,
  activeSpanIds,
  onApplyExpected,
  applied,
}: {
  check: ReconciliationCheck;
  onApplyExpected?: (check: ReconciliationCheck) => void;
  applied?: boolean;
} & ActivateProps) {
  const spanIds = check.citations.map((citation) => citation.sourceSpanId);
  const active = spanIds.some((id) => activeSpanIds.has(id));
  const delta = reconciliationDelta(check);
  const tone = reconciliationTone(check.status);
  const canApply = Boolean(onApplyExpected) && Boolean(check.expected.value);

  return (
    <div
      onMouseEnter={() => onActivate(spanIds, [check.detected.value, check.expected.value])}
      onMouseLeave={() => onActivate([], [])}
      className={cn(
        "group/row relative grid grid-cols-[minmax(0,1fr)_auto] items-center gap-x-3 gap-y-1 overflow-hidden rounded-md border px-3 py-2 pl-4 text-left transition-colors sm:grid-cols-[8rem_minmax(0,1fr)_auto]",
        active ? "border-primary-border bg-primary-soft" : "border-border bg-surface hover:bg-surface-2/60",
      )}
    >
      <span
        aria-hidden="true"
        className={cn(
          "absolute inset-y-0 left-0 w-[3px]",
          tone === "success" ? "bg-success" : tone === "danger" ? "bg-danger" : "bg-warning",
        )}
      />
      <span className="truncate text-sm font-medium tracking-tight">{humanizeEnum(check.name)}</span>
      <span className="col-span-2 flex flex-wrap items-baseline gap-x-2 font-mono text-xs tabular text-muted-foreground sm:col-span-1">
        <span>
          <span className="text-subtle-foreground">stated</span> {check.detected.value}
        </span>
        <span aria-hidden="true" className="text-subtle-foreground">→</span>
        <span>
          <span className="text-subtle-foreground">expected</span> {check.expected.value}
        </span>
      </span>
      <span className="flex items-center justify-end gap-2">
        {applied ? (
          <span className="inline-flex items-center gap-1 rounded-md bg-success-soft px-2 py-1 text-[11px] font-semibold text-success">
            <IconCheck width={12} height={12} /> Applied
          </span>
        ) : (
          canApply && (
            <button
              type="button"
              onClick={() => onApplyExpected?.(check)}
              title={`Set ${humanizeEnum(check.name)} to the reconciled value ${check.expected.value}`}
              className="inline-flex shrink-0 items-center gap-1 rounded-md border border-border bg-surface px-2 py-1 text-[11px] font-medium text-muted-foreground transition-colors hover:border-primary-border hover:bg-primary-soft hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/40"
            >
              Use expected
            </button>
          )
        )}
        {delta && delta.direction !== "flat" && (
          <span
            className={cn(
              "rounded px-1.5 py-0.5 font-mono text-xs font-semibold tabular",
              tone === "danger" ? "bg-danger-soft text-danger" : "bg-warning-soft text-warning",
            )}
          >
            {delta.text}
          </span>
        )}
        <span
          aria-hidden="true"
          title={check.status}
          className={cn(
            "flex size-5 shrink-0 items-center justify-center rounded-full",
            tone === "success"
              ? "bg-success-soft text-success"
              : tone === "danger"
                ? "bg-danger-soft text-danger"
                : "bg-warning-soft text-warning",
          )}
        >
          {tone === "success" ? <IconCheck width={12} height={12} /> : <IconAlert width={12} height={12} />}
        </span>
      </span>
    </div>
  );
}

const normalizeName = (name: string): string => name.replace(/\s+/g, "").toLowerCase();

export function VerdictHeader({
  documentType,
  confidence,
  fields,
  reconciliation,
  onActivate,
  activeSpanIds,
  onApplyExpected,
  appliedChecks,
}: {
  documentType: ReinsuranceDocumentType;
  confidence: number;
  fields: FieldValue[];
  reconciliation: ReconciliationCheck[];
  onApplyExpected?: (check: ReconciliationCheck) => void;
  appliedChecks?: Set<string>;
} & ActivateProps) {
  const { failing, total } = reconciliationSummary(reconciliation);
  const cedent = findValue(fields, "Cedent");
  const period = findValue(fields, "Period");
  const tier = confidenceTier(confidence);
  const clean = failing === 0;

  const confidenceTone = tier === "high" ? "success" : tier === "medium" ? "warning" : "danger";

  return (
    <section className="flex flex-col gap-3 border-b border-border bg-surface px-4 py-3">
      <div className="flex flex-wrap items-center gap-x-3 gap-y-1.5">
        <Badge tone="primary" className="uppercase tracking-wide">
          {documentTypeLabel(documentType)}
        </Badge>
        {cedent && <span className="text-sm font-semibold tracking-tight">{cedent}</span>}
        {period && (
          <>
            <span aria-hidden="true" className="h-3 w-px bg-border" />
            <span className="font-mono text-xs tabular text-muted-foreground">{period}</span>
          </>
        )}
        <span
          className={cn(
            "ml-auto inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 text-[11px] font-medium",
            confidenceTone === "success" && "border-success/30 bg-success-soft text-success",
            confidenceTone === "warning" && "border-warning/30 bg-warning-soft text-warning",
            confidenceTone === "danger" && "border-danger/30 bg-danger-soft text-danger",
          )}
        >
          <Dot tone={confidenceTone} />
          {tierLabel[tier]} confidence
          <span className="font-mono tabular opacity-70">{formatPercent(confidence)}</span>
        </span>
      </div>

      {total > 0 && (
        <div
          className={cn(
            "flex flex-col gap-2 rounded-lg border px-3.5 py-3",
            clean ? "border-success/25 bg-success-soft/40" : "border-danger/25 bg-danger-soft/40",
          )}
          data-tour="reconciliation-panel"
        >
          <div className="flex items-center gap-2.5">
            {clean ? (
              <span className="flex size-6 shrink-0 items-center justify-center rounded-full bg-success-soft text-success ring-1 ring-inset ring-success/30">
                <IconCheck width={14} height={14} />
              </span>
            ) : (
              <span className="flex size-6 shrink-0 items-center justify-center rounded-full bg-danger-soft text-danger ring-1 ring-inset ring-danger/30">
                <IconAlert width={14} height={14} />
              </span>
            )}
            <span className="text-sm font-semibold tracking-tight">
              {clean
                ? `All ${total} control totals reconcile`
                : `${failing} of ${total} control totals don't reconcile`}
            </span>
            <span className="ml-auto font-mono text-[11px] tabular text-subtle-foreground">
              {total - failing}/{total} pass
            </span>
          </div>
          {!clean && (
            <div className="grid gap-1.5">
              {reconciliation.map((check) => (
                <VerdictRow
                  key={check.id}
                  check={check}
                  onActivate={onActivate}
                  activeSpanIds={activeSpanIds}
                  onApplyExpected={onApplyExpected}
                  applied={appliedChecks?.has(normalizeName(check.name))}
                />
              ))}
            </div>
          )}
        </div>
      )}
    </section>
  );
}
