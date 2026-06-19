"use client";

import { useState } from "react";
import { cn } from "@/lib/cn";
import type { FieldValue } from "@/lib/api/types";
import { confidenceTier, humanizeEnum, type ConfidenceTier } from "@/lib/format";
import { groupFields, isMoneyField } from "@/lib/review";
import { Button } from "@/components/ui/primitives";
import { Input } from "@/components/ui/form";
import { IconCheck, IconClose, IconPencil } from "@/components/ui/icons";

const tierMeta: Record<ConfidenceTier, { label: string; className: string; dot: string }> = {
  high: { label: "High", className: "text-success", dot: "bg-confidence-high" },
  medium: { label: "Medium", className: "text-warning", dot: "bg-confidence-medium" },
  low: { label: "Low", className: "text-danger", dot: "bg-confidence-low" },
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

interface CorrectionProps {
  corrections: Record<string, string>;
  onCorrect: (key: string, value: string) => void;
  onClearCorrection: (key: string) => void;
  editingDisabled: boolean;
}

function FieldRow({
  field,
  onActivate,
  activeSpanIds,
  corrections,
  onCorrect,
  onClearCorrection,
  editingDisabled,
}: { field: FieldValue } & ActivateProps & CorrectionProps) {
  const [editing, setEditing] = useState(false);
  const spanIds = field.provenance.citations.map((citation) => citation.sourceSpanId);
  const active = spanIds.some((id) => activeSpanIds.has(id));
  const corrected = field.key in corrections;
  const correctedValue = corrections[field.key];
  const displayValue = corrected ? correctedValue : field.value;
  const missing = !displayValue || field.status.toLowerCase().includes("missing");
  const money = isMoneyField(field.label);
  const tier = confidenceTier(field.confidence);
  const method = methodLabel[field.provenance.method] ?? humanizeEnum(field.provenance.method);
  const [draft, setDraft] = useState(displayValue ?? "");

  const startEditing = () => {
    setDraft(displayValue ?? "");
    setEditing(true);
  };
  const commit = () => {
    const next = draft.trim();
    if (next === (field.value ?? "").trim()) {
      onClearCorrection(field.key);
    } else {
      onCorrect(field.key, next);
    }
    setEditing(false);
  };

  if (editing) {
    return (
      <div className="flex flex-col gap-2 border-b border-border bg-surface-2/50 px-3.5 py-2.5 shadow-[inset_2px_0_0_0_var(--color-primary)] last:border-0">
        <div className="flex items-center justify-between gap-2">
          <span className="text-xs font-medium text-foreground">{humanizeEnum(field.label)}</span>
          <span className="hidden items-center gap-1 text-[10px] text-subtle-foreground sm:flex">
            <kbd className="rounded border border-border bg-surface px-1 font-mono text-[9px] tabular">↵</kbd>
            save
            <kbd className="ml-1 rounded border border-border bg-surface px-1 font-mono text-[9px] tabular">esc</kbd>
            cancel
          </span>
        </div>
        <div className="flex items-center gap-2">
          <Input
            value={draft}
            autoFocus
            onChange={(event) => setDraft(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                event.preventDefault();
                commit();
              }
              if (event.key === "Escape") {
                event.preventDefault();
                setEditing(false);
              }
            }}
            className="h-8 font-mono text-sm"
            aria-label={`Correct ${humanizeEnum(field.label)}`}
          />
          <Button variant="primary" size="icon" className="size-8 shrink-0" onClick={commit} aria-label="Save correction">
            <IconCheck width={15} height={15} />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="size-8 shrink-0"
            onClick={() => setEditing(false)}
            aria-label="Cancel correction"
          >
            <IconClose width={15} height={15} />
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div
      className={cn(
        "group relative flex items-stretch border-b border-border transition-colors last:border-0",
        active
          ? "bg-primary-soft shadow-[inset_2px_0_0_0_var(--color-primary)]"
          : "hover:bg-surface-2/60",
      )}
    >
      <button
        type="button"
        data-citation-row="field"
        onMouseEnter={() => onActivate(spanIds, [displayValue])}
        onMouseLeave={() => onActivate([], [])}
        onFocus={() => onActivate(spanIds, [displayValue])}
        onBlur={() => onActivate([], [])}
        className="flex min-w-0 flex-1 items-center justify-between gap-3 px-3.5 py-2.5 pr-9 text-left"
      >
        <span className="flex min-w-0 flex-col gap-0.5">
          <span className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
            {humanizeEnum(field.label)}
            {corrected && (
              <span className="rounded-full border border-warning/30 bg-warning-soft px-1.5 py-px text-[10px] font-semibold uppercase tracking-wide text-warning">
                Corrected
              </span>
            )}
          </span>
          {missing ? (
            <span className="text-sm italic text-subtle-foreground">Not found in source</span>
          ) : (
            <span
              className={cn(
                "truncate font-mono tabular",
                money ? "text-[15px] font-semibold text-foreground" : "text-sm",
              )}
            >
              {displayValue}
            </span>
          )}
        </span>
        <span className="flex shrink-0 flex-col items-end gap-1">
          {!missing && field.confidence > 0 ? (
            <span className={cn("flex items-center gap-1.5 text-[11px] font-semibold", tierMeta[tier].className)}>
              <span aria-hidden="true" className={cn("size-1.5 rounded-full", tierMeta[tier].dot)} />
              {tierMeta[tier].label}
            </span>
          ) : missing ? (
            <span className="text-[11px] font-medium text-danger">missing</span>
          ) : null}
          <span
            className={cn("text-[10px]", corrected ? "font-medium text-warning" : "text-subtle-foreground")}
            title={method}
          >
            {corrected ? "Reviewer corrected" : method}
          </span>
        </span>
      </button>
      {!editingDisabled && (
        <button
          type="button"
          onClick={startEditing}
          aria-label={`Edit ${humanizeEnum(field.label)}`}
          className="absolute right-2 top-1/2 grid size-7 -translate-y-1/2 place-items-center rounded-md text-subtle-foreground opacity-60 transition-all hover:bg-surface-3 hover:text-foreground hover:opacity-100 focus-visible:opacity-100 group-hover:opacity-100"
        >
          <IconPencil width={14} height={14} />
        </button>
      )}
    </div>
  );
}

export function FieldGroups({
  fields,
  onActivate,
  activeSpanIds,
  corrections,
  onCorrect,
  onClearCorrection,
  editingDisabled,
}: { fields: FieldValue[] } & ActivateProps & CorrectionProps) {
  const groups = groupFields(fields);

  if (fields.length === 0) {
    return (
      <div className="bg-dotgrid m-4 flex flex-col items-center justify-center gap-1 rounded-md border border-dashed border-border bg-surface-2/30 px-6 py-10 text-center">
        <p className="text-sm font-medium text-muted-foreground">No fields extracted</p>
        <p className="max-w-xs text-xs text-subtle-foreground">
          This document produced no structured fields. Check the source on the left for the raw content.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col">
      {groups.map((group) => (
        <section key={group.title}>
          <header className="sticky top-[33px] z-10 flex items-center justify-between border-b border-border bg-surface-2/85 px-4 py-1.5 backdrop-blur-sm">
            <h3 className="text-[10.5px] font-semibold uppercase tracking-[0.09em] text-subtle-foreground">{group.title}</h3>
            <span className="font-mono text-[11px] tabular text-subtle-foreground">{group.fields.length}</span>
          </header>
          {group.fields.map((field) => (
            <FieldRow
              key={field.key}
              field={field}
              onActivate={onActivate}
              activeSpanIds={activeSpanIds}
              corrections={corrections}
              onCorrect={onCorrect}
              onClearCorrection={onClearCorrection}
              editingDisabled={editingDisabled}
            />
          ))}
        </section>
      ))}
    </div>
  );
}
