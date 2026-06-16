"use client";

import { cn } from "@/lib/cn";
import type { FieldValue, ReconciliationCheck } from "@/lib/api/types";
import { confidenceTier, humanizeEnum, reconciliationTone } from "@/lib/format";
import { Badge, ConfidenceMeter } from "@/components/ui/primitives";
import { IconScale } from "@/components/ui/icons";

const fieldStatusTone = (status: string) => {
  const normalized = status.toLowerCase();
  if (normalized.includes("missing")) {
    return "danger" as const;
  }
  if (normalized.includes("low")) {
    return "warning" as const;
  }
  if (normalized.includes("confirmed") || normalized.includes("detected")) {
    return "success" as const;
  }
  return "neutral" as const;
};

interface ActivateProps {
  onActivate: (spanIds: string[], values: string[]) => void;
  activeSpanIds: Set<string>;
}

function FieldRow({ field, onActivate, activeSpanIds }: { field: FieldValue } & ActivateProps) {
  const spanIds = field.provenance.citations.map((citation) => citation.sourceSpanId);
  const active = spanIds.some((id) => activeSpanIds.has(id));
  const hasCitation = spanIds.length > 0;

  return (
    <button
      type="button"
      data-citation-row="field"
      onMouseEnter={() => onActivate(spanIds, [field.value])}
      onMouseLeave={() => onActivate([], [])}
      onFocus={() => onActivate(spanIds, [field.value])}
      onBlur={() => onActivate([], [])}
      className={cn(
        "flex w-full flex-col gap-1.5 border-b border-border px-3.5 py-2.5 text-left transition-colors last:border-0",
        active ? "bg-primary-soft" : "hover:bg-surface-2/60",
      )}
    >
      <div className="flex items-center justify-between gap-2">
        <span className="text-xs font-medium text-muted-foreground">{humanizeEnum(field.label)}</span>
        <Badge tone={fieldStatusTone(field.status)}>{humanizeEnum(field.status)}</Badge>
      </div>
      <div className="flex items-center justify-between gap-2">
        <span className="truncate font-mono text-sm tabular">{field.value || "—"}</span>
        {field.confidence > 0 && (
          <ConfidenceMeter
            score={field.confidence}
            tier={confidenceTier(field.confidence)}
            showValue={false}
          />
        )}
      </div>
      <div className="flex items-center gap-2 text-[11px] text-subtle-foreground">
        <span>{humanizeEnum(field.provenance.method)}</span>
        {hasCitation ? (
          <span className="inline-flex items-center gap-1 text-primary">
            <span className="size-1 rounded-full bg-primary" />
            {spanIds.length} citation{spanIds.length === 1 ? "" : "s"}
          </span>
        ) : (
          <span>no source span</span>
        )}
      </div>
    </button>
  );
}

function CheckRow({
  check,
  onActivate,
  activeSpanIds,
}: { check: ReconciliationCheck } & ActivateProps) {
  const spanIds = check.citations.map((citation) => citation.sourceSpanId);
  const active = spanIds.some((id) => activeSpanIds.has(id));
  const tone = reconciliationTone(check.status);
  const values = [check.detected.value, check.expected.value];

  return (
    <button
      type="button"
      data-citation-row="reconciliation"
      onMouseEnter={() => onActivate(spanIds, values)}
      onMouseLeave={() => onActivate([], [])}
      onFocus={() => onActivate(spanIds, values)}
      onBlur={() => onActivate([], [])}
      className={cn(
        "flex w-full flex-col gap-2 border-b border-border px-3.5 py-3 text-left transition-colors last:border-0",
        active ? "bg-primary-soft" : "hover:bg-surface-2/60",
      )}
    >
      <div className="flex items-center justify-between gap-2">
        <span className="text-sm font-medium">{humanizeEnum(check.name)}</span>
        <Badge tone={tone}>{check.status}</Badge>
      </div>
      <div className="grid grid-cols-2 gap-2">
        <div className="rounded-md bg-surface-2/60 px-2.5 py-1.5">
          <p className="text-[10px] font-semibold uppercase tracking-wider text-subtle-foreground">Detected</p>
          <p className="truncate font-mono text-sm tabular">{check.detected.value || "—"}</p>
        </div>
        <div className="rounded-md bg-surface-2/60 px-2.5 py-1.5">
          <p className="text-[10px] font-semibold uppercase tracking-wider text-subtle-foreground">Expected</p>
          <p className="truncate font-mono text-sm tabular">{check.expected.value || "—"}</p>
        </div>
      </div>
      {check.explanation && (
        <p className="text-[11px] leading-snug text-muted-foreground">{check.explanation}</p>
      )}
    </button>
  );
}

export function ReviewDetails({
  fields,
  reconciliation,
  onActivate,
  activeSpanIds,
}: {
  fields: FieldValue[];
  reconciliation: ReconciliationCheck[];
} & ActivateProps) {
  return (
    <div className="flex flex-col">
      <section>
        <header className="flex items-center justify-between border-b border-border bg-surface px-3.5 py-2">
          <h3 className="text-xs font-semibold uppercase tracking-wider text-subtle-foreground">
            Extracted fields
          </h3>
          <span className="font-mono text-xs tabular text-muted-foreground">{fields.length}</span>
        </header>
        {fields.length === 0 ? (
          <p className="px-3.5 py-4 text-xs text-muted-foreground">No fields extracted for this document.</p>
        ) : (
          fields.map((field) => (
            <FieldRow
              key={field.key}
              field={field}
              onActivate={onActivate}
              activeSpanIds={activeSpanIds}
            />
          ))
        )}
      </section>

      <section data-tour="reconciliation-panel">
        <header className="flex items-center justify-between border-y border-border bg-surface px-3.5 py-2">
          <h3 className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-subtle-foreground">
            <IconScale width={13} height={13} />
            Reconciliation
          </h3>
          <span className="font-mono text-xs tabular text-muted-foreground">{reconciliation.length}</span>
        </header>
        {reconciliation.length === 0 ? (
          <p className="px-3.5 py-4 text-xs text-success">All control totals reconcile. No exceptions.</p>
        ) : (
          reconciliation.map((check) => (
            <CheckRow
              key={check.id}
              check={check}
              onActivate={onActivate}
              activeSpanIds={activeSpanIds}
            />
          ))
        )}
      </section>
    </div>
  );
}
