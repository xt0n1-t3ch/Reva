import { confidenceThresholds } from "@/lib/config";
import type {
  DocumentStatus,
  ExceptionSeverity,
  ReinsuranceDocumentType,
  ReviewState,
} from "@/lib/api/types";

export type Tone = "neutral" | "primary" | "success" | "warning" | "danger";
export type ConfidenceTier = "high" | "medium" | "low";

export const confidenceTier = (score: number): ConfidenceTier => {
  if (score >= confidenceThresholds.mediumMax) {
    return "high";
  }
  return score >= confidenceThresholds.lowMax ? "medium" : "low";
};

export const formatPercent = (value: number): string =>
  `${Math.round(Math.max(0, Math.min(1, value)) * 100)}%`;

const compactDate = new Intl.DateTimeFormat("en-US", {
  month: "short",
  day: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

export const formatTimestamp = (iso: string): string => {
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? iso : compactDate.format(date);
};

export const formatRelative = (iso: string): string => {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  const seconds = Math.round((Date.now() - date.getTime()) / 1000);
  if (seconds < 60) {
    return "just now";
  }
  const minutes = Math.round(seconds / 60);
  if (minutes < 60) {
    return `${minutes}m ago`;
  }
  const hours = Math.round(minutes / 60);
  if (hours < 24) {
    return `${hours}h ago`;
  }
  return `${Math.round(hours / 24)}d ago`;
};

const wordBoundary = /(?<=[a-z])(?=[A-Z])/g;

export const humanizeEnum = (value: string): string => value.replace(wordBoundary, " ");

export const documentTypeLabel = (type: ReinsuranceDocumentType): string =>
  type === "Unknown" ? "Unclassified" : humanizeEnum(type);

export const statusTone: Record<DocumentStatus, Tone> = {
  Uploaded: "neutral",
  Parsed: "primary",
  Extracted: "success",
  Unsupported: "warning",
  Failed: "danger",
};

export const reviewStateTone: Record<ReviewState, Tone> = {
  Pending: "warning",
  Approved: "success",
  Rejected: "danger",
  NeedsCorrection: "warning",
};

export const severityTone: Record<ExceptionSeverity, Tone> = {
  Info: "neutral",
  Warning: "warning",
  Critical: "danger",
};

export const reconciliationTone = (status: string): Tone => {
  const normalized = status.toLowerCase();
  if (normalized.includes("pass") || normalized.includes("match") || normalized.includes("ok")) {
    return "success";
  }
  if (normalized.includes("fail") || normalized.includes("mismatch") || normalized.includes("error")) {
    return "danger";
  }
  return "warning";
};
