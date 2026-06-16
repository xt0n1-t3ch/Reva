"use client";

import { cn } from "@/lib/cn";
import type { LineItemValue } from "@/lib/api/types";
import { humanizeEnum } from "@/lib/format";
import { isMoneyField } from "@/lib/review";

export function LineItems({
  lineItems,
  onActivate,
  activeSpanIds,
}: {
  lineItems: LineItemValue[];
  onActivate: (spanIds: string[], values: string[]) => void;
  activeSpanIds: Set<string>;
}) {
  if (lineItems.length === 0) {
    return null;
  }

  const columns = lineItems[0].fields.map((field) => field.label);

  return (
    <section>
      <header className="flex items-center justify-between border-y border-border bg-surface px-3.5 py-2">
        <h3 className="text-[11px] font-semibold uppercase tracking-wider text-subtle-foreground">Line items</h3>
        <span className="font-mono text-[11px] tabular text-subtle-foreground">{lineItems.length}</span>
      </header>
      <div className="overflow-x-auto">
        <table className="w-full min-w-max border-collapse text-xs">
          <thead>
            <tr className="border-b border-border text-subtle-foreground">
              {columns.map((column) => (
                <th
                  key={column}
                  className={cn(
                    "whitespace-nowrap px-3 py-1.5 font-medium",
                    isMoneyField(column) ? "text-right" : "text-left",
                  )}
                >
                  {humanizeEnum(column)}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {lineItems.map((item) => {
              const active = item.rowCitationIds.some((id) => activeSpanIds.has(id));
              return (
                <tr
                  key={item.id}
                  onMouseEnter={() => onActivate(item.rowCitationIds, item.fields.map((field) => field.value))}
                  onMouseLeave={() => onActivate([], [])}
                  className={cn("border-b border-border last:border-0", active ? "bg-primary-soft" : "hover:bg-surface-2/50")}
                >
                  {item.fields.map((field) => (
                    <td
                      key={field.key}
                      className={cn(
                        "whitespace-nowrap px-3 py-1.5",
                        isMoneyField(field.label) ? "text-right font-mono tabular" : "text-left",
                      )}
                    >
                      {field.value || "—"}
                    </td>
                  ))}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </section>
  );
}
