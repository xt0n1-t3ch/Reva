import type { DocumentSummary } from "@/lib/api/types";
import { confidenceTier, documentTypeLabel } from "@/lib/format";

/** A single day's ingest count, split into reviewed vs still-pending. */
export interface ThroughputPoint {
  /** ISO day key (yyyy-mm-dd), used for sorting. */
  key: string;
  /** Short axis label, e.g. "Jun 14". */
  label: string;
  reviewed: number;
  pending: number;
  total: number;
}

export interface CategoryBar {
  label: string;
  value: number;
}

export interface ConfidenceBucket {
  label: string;
  range: string;
  tier: "high" | "medium" | "low";
  count: number;
}

const dayLabel = new Intl.DateTimeFormat("en-US", { month: "short", day: "numeric" });

const dayKey = (iso: string): string | null => {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return null;
  }
  return date.toISOString().slice(0, 10);
};

/**
 * Documents ingested per calendar day over the trailing window, split into
 * reviewed (Approved/Rejected) and pending. Backfills empty days so the axis
 * reads as a continuous timeline rather than a sparse scatter.
 */
export const buildThroughput = (documents: DocumentSummary[], days = 14): ThroughputPoint[] => {
  const buckets = new Map<string, { reviewed: number; pending: number }>();
  for (const document of documents) {
    const key = dayKey(document.createdAt);
    if (!key) {
      continue;
    }
    const bucket = buckets.get(key) ?? { reviewed: 0, pending: 0 };
    if (document.reviewState === "Approved" || document.reviewState === "Rejected") {
      bucket.reviewed += 1;
    } else {
      bucket.pending += 1;
    }
    buckets.set(key, bucket);
  }

  if (buckets.size === 0) {
    return [];
  }

  // Anchor the window on the most recent ingest day so seeded/historic data still shows.
  const latest = [...buckets.keys()].sort().at(-1)!;
  const end = new Date(`${latest}T00:00:00Z`);
  const points: ThroughputPoint[] = [];
  for (let offset = days - 1; offset >= 0; offset -= 1) {
    const date = new Date(end);
    date.setUTCDate(end.getUTCDate() - offset);
    const key = date.toISOString().slice(0, 10);
    const bucket = buckets.get(key) ?? { reviewed: 0, pending: 0 };
    points.push({
      key,
      label: dayLabel.format(date),
      reviewed: bucket.reviewed,
      pending: bucket.pending,
      total: bucket.reviewed + bucket.pending,
    });
  }
  return points;
};

/** Open exceptions grouped by document type, busiest first. Empty when all clear. */
export const buildExceptionsByType = (documents: DocumentSummary[]): CategoryBar[] => {
  const totals = new Map<string, number>();
  for (const document of documents) {
    if (document.exceptionCount <= 0) {
      continue;
    }
    const label = documentTypeLabel(document.documentType);
    totals.set(label, (totals.get(label) ?? 0) + document.exceptionCount);
  }
  return [...totals.entries()]
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => b.value - a.value);
};

const bucketOrder: ConfidenceBucket[] = [
  { label: "High", range: "85–100%", tier: "high", count: 0 },
  { label: "Medium", range: "60–85%", tier: "medium", count: 0 },
  { label: "Low", range: "0–60%", tier: "low", count: 0 },
];

/** Distribution of extracted documents across the three confidence tiers. */
export const buildConfidenceDistribution = (documents: DocumentSummary[]): ConfidenceBucket[] => {
  const counts: Record<ConfidenceBucket["tier"], number> = { high: 0, medium: 0, low: 0 };
  for (const document of documents) {
    if (document.confidence <= 0) {
      continue;
    }
    counts[confidenceTier(document.confidence)] += 1;
  }
  return bucketOrder.map((bucket) => ({ ...bucket, count: counts[bucket.tier] }));
};
