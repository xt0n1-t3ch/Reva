"use client";

import { useMemo } from "react";
import { cn } from "@/lib/cn";
import type { FieldValue, LineItemValue, ReinsuranceDocumentType } from "@/lib/api/types";

/**
 * The structured-template view: the same extracted record, ordered the way a
 * reinsurer files a bordereau — parties, contract, period, financials, then the
 * line-item schedule. Reads only from the extracted fields, so it never
 * diverges from the source pane beside it.
 */
const GROUPS: { title: string; labels: string[] }[] = [
  { title: "Parties", labels: ["Cedent", "Broker", "Reinsurer"] },
  { title: "Contract", labels: ["Contract Reference", "Line of Business", "Period", "Currency"] },
  { title: "Financials", labels: ["Premium", "Claims", "Commission", "Cession %", "Retention", "Limit"] },
];

const MONEY_LABELS = new Set(["Premium", "Claims", "Commission", "Limit"]);

const findField = (fields: FieldValue[], label: string): FieldValue | undefined =>
  fields.find((field) => field.label.toLowerCase() === label.toLowerCase());

const formatValue = (label: string, raw: string): string => {
  const value = raw.trim();
  if (!value) {
    return value;
  }
  if (MONEY_LABELS.has(label)) {
    const numeric = Number(value.replace(/[^0-9.-]/g, ""));
    if (Number.isFinite(numeric) && /\d/.test(value)) {
      const hasCurrency = /[a-z]/i.test(value);
      const formatted = numeric.toLocaleString("en-US", { maximumFractionDigits: 2 });
      return hasCurrency ? value.replace(/[\d,]+(\.\d+)?/, formatted) : formatted;
    }
  }
  return value;
};

function Row({ field, label }: { field: FieldValue | undefined; label: string }) {
  const present = field && field.value.trim().length > 0;
  return (
    <div className="flex items-baseline justify-between gap-4 border-b border-border/60 py-2 last:border-b-0">
      <dt className="shrink-0 text-[11px] font-medium uppercase tracking-[0.05em] text-subtle-foreground">{label}</dt>
      <dd
        className={cn(
          "min-w-0 truncate text-right text-sm",
          present ? "font-medium text-foreground" : "italic text-subtle-foreground",
        )}
      >
        {present ? formatValue(label, field!.value) : "—"}
      </dd>
    </div>
  );
}

export function TemplateView({
  fileName,
  documentType,
  fields,
  lineItems,
}: {
  fileName: string;
  documentType: ReinsuranceDocumentType;
  fields: FieldValue[];
  lineItems: LineItemValue[];
}) {
  const headerCurrency = findField(fields, "Currency")?.value.trim();
  const headerPeriod = findField(fields, "Period")?.value.trim();
  const lineColumns = useMemo(() => {
    const seen = new Set<string>();
    const columns: string[] = [];
    for (const item of lineItems) {
      for (const field of item.fields) {
        if (!seen.has(field.label)) {
          seen.add(field.label);
          columns.push(field.label);
        }
      }
    }
    return columns;
  }, [lineItems]);

  return (
    <div className="mx-auto max-w-2xl">
      <article className="overflow-hidden rounded-lg border border-border bg-surface shadow-pop ring-1 ring-black/[0.02] dark:ring-white/[0.03]">
        <header className="flex items-start justify-between gap-3 border-b border-border bg-gradient-to-br from-surface-2/60 to-surface px-6 py-4">
          <div className="min-w-0">
            <p className="text-[10px] font-semibold uppercase tracking-[0.14em] text-subtle-foreground">
              Reva · Reinsurance bordereau
            </p>
            <h3 className="mt-0.5 truncate text-base font-semibold tracking-tight text-foreground">
              {documentType.replace(/([a-z])([A-Z])/g, "$1 $2")}
            </h3>
            <p className="mt-0.5 truncate font-mono text-[11px] text-muted-foreground">{fileName}</p>
          </div>
          <div className="shrink-0 text-right">
            {headerPeriod && (
              <p className="text-sm font-semibold tabular text-foreground">{headerPeriod}</p>
            )}
            {headerCurrency && (
              <p className="text-[11px] uppercase tracking-[0.08em] text-subtle-foreground">{headerCurrency}</p>
            )}
          </div>
        </header>

        <div className="space-y-5 px-6 py-5">
          {GROUPS.map((group) => (
            <section key={group.title}>
              <h4 className="mb-1 text-[10.5px] font-semibold uppercase tracking-[0.1em] text-primary">{group.title}</h4>
              <dl>
                {group.labels.map((label) => (
                  <Row key={label} label={label} field={findField(fields, label)} />
                ))}
              </dl>
            </section>
          ))}

          {lineItems.length > 0 && lineColumns.length > 0 && (
            <section>
              <h4 className="mb-1.5 text-[10.5px] font-semibold uppercase tracking-[0.1em] text-primary">
                Risk schedule · {lineItems.length} line{lineItems.length === 1 ? "" : "s"}
              </h4>
              <div className="overflow-x-auto rounded-md border border-border">
                <table className="w-full border-collapse text-left text-[11.5px]">
                  <thead>
                    <tr className="bg-surface-2/50">
                      <th className="px-2 py-1.5 font-mono text-[10px] uppercase tracking-[0.05em] text-subtle-foreground">#</th>
                      {lineColumns.map((column) => (
                        <th
                          key={column}
                          className="whitespace-nowrap px-2 py-1.5 font-mono text-[10px] uppercase tracking-[0.05em] text-subtle-foreground"
                        >
                          {column}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {lineItems.map((item) => (
                      <tr key={item.id} className="border-t border-border/60">
                        <td className="px-2 py-1.5 font-mono text-subtle-foreground">{item.rowNumber}</td>
                        {lineColumns.map((column) => {
                          const cell = item.fields.find((field) => field.label === column);
                          return (
                            <td key={column} className="whitespace-nowrap px-2 py-1.5 text-foreground/90">
                              {cell?.value.trim() ? formatValue(column, cell.value) : "—"}
                            </td>
                          );
                        })}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          )}
        </div>

        <footer className="border-t border-border bg-surface-2/30 px-6 py-2.5">
          <p className="text-[10.5px] text-subtle-foreground">
            Ordered to Reva&apos;s default reinsurance template · customize columns in Export.
          </p>
        </footer>
      </article>
    </div>
  );
}
