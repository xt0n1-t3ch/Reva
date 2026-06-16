import type { FieldValue, ReconciliationCheck } from "@/lib/api/types";
import { reconciliationTone, type Tone } from "@/lib/format";

export interface FieldGroup {
  title: string;
  keys: string[];
}

// Canonical reinsurance fields grouped so a reviewer scans by meaning, not a flat list.
export const fieldGroups: FieldGroup[] = [
  { title: "Parties", keys: ["Cedent", "Broker", "Reinsurer"] },
  { title: "Contract", keys: ["Contract Reference", "Line of Business", "Period", "Currency"] },
  { title: "Financials", keys: ["Premium", "Claims", "Commission", "Cession %", "Retention", "Limit"] },
];

const normalizeKey = (value: string) => value.replace(/\s+/g, "").toLowerCase();

export const groupFields = (fields: FieldValue[]): { title: string; fields: FieldValue[] }[] => {
  const remaining = new Map(fields.map((field) => [normalizeKey(field.label), field]));
  const groups = fieldGroups.map((group) => {
    const picked: FieldValue[] = [];
    for (const key of group.keys) {
      const found = remaining.get(normalizeKey(key));
      if (found) {
        picked.push(found);
        remaining.delete(normalizeKey(key));
      }
    }
    return { title: group.title, fields: picked };
  });
  const leftovers = [...remaining.values()];
  if (leftovers.length > 0) {
    groups.push({ title: "Other", fields: leftovers });
  }
  return groups.filter((group) => group.fields.length > 0);
};

export interface ParsedAmount {
  currency: string;
  value: number;
  isPercent: boolean;
}

export const parseAmount = (raw: string): ParsedAmount | null => {
  if (!raw) {
    return null;
  }
  const isPercent = raw.includes("%");
  const currencyMatch = raw.match(/[A-Z]{3}/);
  const numberMatch = raw.replace(/,/g, "").match(/-?\d+(\.\d+)?/);
  if (!numberMatch) {
    return null;
  }
  return {
    currency: currencyMatch ? currencyMatch[0] : "",
    value: Number(numberMatch[0]),
    isPercent,
  };
};

export const isMoneyField = (label: string): boolean =>
  ["premium", "claims", "commission", "retention", "limit"].includes(label.replace(/\s+/g, "").toLowerCase());

const amountFormatter = new Intl.NumberFormat("en-US", { maximumFractionDigits: 2 });

export interface ReconciliationDelta {
  text: string;
  tone: Tone;
  direction: "over" | "under" | "flat";
}

// Signed difference between the stated (detected) and computed (expected) values, in the
// document's own units, framed so a reviewer reads the discrepancy at a glance.
export const reconciliationDelta = (check: ReconciliationCheck): ReconciliationDelta | null => {
  const detected = parseAmount(check.detected.value);
  const expected = parseAmount(check.expected.value);
  if (!detected || !expected) {
    return null;
  }
  const diff = detected.value - expected.value;
  const tone = reconciliationTone(check.status);
  if (Math.abs(diff) < 1e-9) {
    return { text: "matches", tone: "success", direction: "flat" };
  }
  const sign = diff > 0 ? "+" : "−";
  const magnitude = amountFormatter.format(Math.abs(diff));
  const unit = detected.isPercent ? " pts" : detected.currency ? ` ${detected.currency}` : "";
  return {
    text: `${sign}${detected.isPercent ? magnitude : `${unit.trim()} ${magnitude}`.trim()}${detected.isPercent ? unit : ""}`,
    tone,
    direction: diff > 0 ? "over" : "under",
  };
};

export const reconciliationSummary = (checks: ReconciliationCheck[]): { failing: number; total: number } => ({
  failing: checks.filter((check) => reconciliationTone(check.status) !== "success").length,
  total: checks.length,
});
