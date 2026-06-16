"use client";

import { cn } from "@/lib/cn";
import type { FieldValue } from "@/lib/api/types";
import { confidenceTier, humanizeEnum, type ConfidenceTier } from "@/lib/format";
import { groupFields, isMoneyField } from "@/lib/review";

const tierMeta: Record<ConfidenceTier, { label: string; className: string }> = {
  high: { label: "High", className: "text-success" },
  medium: { label: "Medium", className: "text-warning-foreground" },
  low: { label: "Low", className: "text-danger" },
};

const methodLabel: Record<string, string> = {
  digital_parse: "Parsed from text",
  csv_parse: "From table",
  schema_mapping: "AI-mapped",
  manual: "Reviewer confirmed",
};

interface ActivateProps {
  onActivate: (spanIds: string[], values: string[]) => void;
  activeSpanIds: Set<string>;
}

function FieldRow({ field, onActivate, activeSpanIds }: { field: FieldValue } & ActivateProps) {
  const spanIds = field.provenance.citations.map((citation) => citation.sourceSpanId);
  const active = spanIds.some((id) => activeSpanIds.has(id));
  const missing = !field.value || field.status.toLowerCase().includes("missing");
  const money = isMoneyField(field.label);
  const tier = confidenceTier(field.confidence);
  const method = methodLabel[field.provenance.method] ?? humanizeEnum(field.provenance.method);

  return (
    <button
      type="button"
      onMouseEnter={() => onActivate(spanIds, [field.value])}
      onMouseLeave={() => onActivate([], [])}
      onFocus={() => onActivate(spanIds, [field.value])}
      onBlur={() => onActivate([], [])}
      className={cn(
        "flex w-full items-center justify-between gap-3 border-b border-border px-3.5 py-2.5 text-left transition-colors last:border-0",
        active ? "bg-primary-soft" : "hover:bg-surface-2/60",
      )}
    >
      <span className="flex min-w-0 flex-col gap-0.5">
        <span className="text-xs font-medium text-muted-foreground">{humanizeEnum(field.label)}</span>
        {missing ? (
          <span className="text-sm italic text-subtle-foreground">Not found</span>
        ) : (
          <span
            className={cn(
              "truncate font-mono tabular",
              money ? "text-[15px] font-semibold text-foreground" : "text-sm",
            )}
          >
            {field.value}
          </span>
        )}
      </span>
      <span className="flex shrink-0 flex-col items-end gap-1">
        {!missing && field.confidence > 0 ? (
          <span className={cn("text-[11px] font-semibold", tierMeta[tier].className)}>{tierMeta[tier].label}</span>
        ) : missing ? (
          <span className="text-[11px] font-medium text-danger">missing</span>
        ) : null}
        <span className="text-[10px] text-subtle-foreground" title={method}>
          {method}
        </span>
      </span>
    </button>
  );
}

export function FieldGroups({
  fields,
  onActivate,
  activeSpanIds,
}: { fields: FieldValue[] } & ActivateProps) {
  const groups = groupFields(fields);

  if (fields.length === 0) {
    return <p className="px-3.5 py-4 text-xs text-muted-foreground">No fields extracted for this document.</p>;
  }

  return (
    <div className="flex flex-col">
      {groups.map((group) => (
        <section key={group.title}>
          <header className="sticky top-0 z-10 flex items-center justify-between border-b border-border bg-surface/95 px-3.5 py-1.5 backdrop-blur-sm">
            <h3 className="text-[11px] font-semibold uppercase tracking-wider text-subtle-foreground">{group.title}</h3>
            <span className="font-mono text-[11px] tabular text-subtle-foreground">{group.fields.length}</span>
          </header>
          {group.fields.map((field) => (
            <FieldRow key={field.key} field={field} onActivate={onActivate} activeSpanIds={activeSpanIds} />
          ))}
        </section>
      ))}
    </div>
  );
}
