"use client";

import { useMemo } from "react";
import { api } from "@/lib/api/client";
import type { SchemaMapping } from "@/lib/api/types";
import { confidenceTier, formatPercent, humanizeEnum, type ConfidenceTier } from "@/lib/format";
import { useApi } from "@/lib/use-api";
import { Badge, ConfidenceMeter, tierTone } from "@/components/ui/primitives";
import { PageContainer, PageHeader, SectionCard } from "@/components/ui/page";
import { EmptyState, ErrorBanner, Skeleton } from "@/components/ui/states";
import { IconMappings } from "@/components/ui/icons";

const defaultSenderLabel = "Default";
const emptyValueLabel = "No normalized sample";
const fallbackHeaderLabel = "Unknown header";
const fallbackCanonicalFieldLabel = "Unmapped field";
const maxSenderLabelLength = 64;

const tierLabel: Record<ConfidenceTier, string> = {
  high: "High",
  medium: "Med",
  low: "Low",
};

type SenderGroup = {
  senderKey: string;
  label: string;
  mappings: SchemaMapping[];
};

const cleanText = (value: string | null | undefined, fallback: string): string => {
  const trimmed = value?.trim();
  return trimmed ? trimmed : fallback;
};

const clampConfidence = (value: number | null | undefined): number => {
  if (typeof value !== "number" || Number.isNaN(value)) {
    return 0;
  }
  return Math.max(0, Math.min(1, value));
};

const senderLabel = (senderKey: string): string => {
  const label = cleanText(senderKey, defaultSenderLabel);
  return label.length > maxSenderLabelLength ? `${label.slice(0, maxSenderLabelLength - 1)}…` : label;
};

const groupMappings = (mappings: SchemaMapping[]): SenderGroup[] => {
  const groups = new Map<string, SchemaMapping[]>();

  for (const mapping of mappings) {
    const senderKey = cleanText(mapping.senderKey, "");
    const senderMappings = groups.get(senderKey) ?? [];
    senderMappings.push(mapping);
    groups.set(senderKey, senderMappings);
  }

  return Array.from(groups, ([senderKey, senderMappings]) => ({
    senderKey,
    label: senderLabel(senderKey),
    mappings: senderMappings.toSorted((left, right) =>
      cleanText(left.sourceHeader, fallbackHeaderLabel).localeCompare(
        cleanText(right.sourceHeader, fallbackHeaderLabel),
      ),
    ),
  })).toSorted((left, right) => left.label.localeCompare(right.label));
};

function MappingSkeletons() {
  return (
    <div className="space-y-4">
      {Array.from({ length: 3 }, (_, index) => (
        <SectionCard key={index} title="Loading sender" meta={<Skeleton className="h-4 w-14" />}>
          <div className="space-y-3 p-3.5">
            <Skeleton className="h-12 w-full" />
            <Skeleton className="h-12 w-full" />
          </div>
        </SectionCard>
      ))}
    </div>
  );
}

function MappingRow({ mapping }: { mapping: SchemaMapping }) {
  const sourceHeader = cleanText(mapping.sourceHeader, fallbackHeaderLabel);
  const canonicalField = cleanText(mapping.canonicalField, fallbackCanonicalFieldLabel);
  const normalizedValue = cleanText(mapping.normalizedValue, emptyValueLabel);
  const confidence = clampConfidence(mapping.confidence);
  const tier = confidenceTier(confidence);

  return (
    <li className="grid gap-3 rounded-md border border-border bg-surface-2/40 p-3 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center">
      <div className="min-w-0 space-y-1.5">
        <div className="flex min-w-0 flex-wrap items-center gap-2 text-sm">
          <span className="min-w-0 max-w-full truncate font-medium" title={sourceHeader}>
            {sourceHeader}
          </span>
          <span className="text-subtle-foreground" aria-hidden="true">
            →
          </span>
          <span className="min-w-0 max-w-full truncate font-semibold text-foreground" title={canonicalField}>
            {humanizeEnum(canonicalField)}
          </span>
        </div>
        <p className="min-w-0 truncate text-xs text-muted-foreground" title={normalizedValue}>
          {normalizedValue}
        </p>
      </div>
      <div className="flex min-w-0 flex-wrap items-center gap-2 sm:justify-end">
        <span className="inline-flex items-center gap-2 rounded-full border border-border bg-surface px-2 py-1">
          <ConfidenceMeter score={confidence} tier={tier} showValue={false} />
          <span className="text-xs font-medium text-muted-foreground">{tierLabel[tier]}</span>
          <span className="font-mono text-xs tabular text-subtle-foreground">{formatPercent(confidence)}</span>
        </span>
        {mapping.isLearned && <Badge tone="primary">Learned</Badge>}
        {mapping.isCorrected && <Badge tone="warning">Corrected</Badge>}
        {!mapping.isLearned && !mapping.isCorrected && <Badge tone={tierTone[tier]}>{humanizeEnum(cleanText(mapping.source, "Static"))}</Badge>}
      </div>
    </li>
  );
}

export function MappingsView() {
  const { data, error, loading, refresh } = useApi(api.listSchemaMappings);
  const groups = useMemo(() => groupMappings(data ?? []), [data]);

  return (
    <PageContainer>
      <PageHeader
        title="Schema mappings"
        subtitle="How each sender's column headers map to canonical reinsurance fields — learned and refined per sender."
      />

      <div className="space-y-4">
        {error && <ErrorBanner message={`Could not load schema mappings: ${error}`} onRetry={refresh} />}

        {loading && !data ? (
          <MappingSkeletons />
        ) : groups.length === 0 && !error ? (
          <EmptyState
            icon={<IconMappings width={18} height={18} />}
            title="No learned mappings yet"
            description="Mappings appear here as the system learns each sender's column layout."
          />
        ) : (
          groups.map((group) => (
            <SectionCard
              key={group.senderKey || defaultSenderLabel}
              title={group.label}
              meta={<span className="tabular">{group.mappings.length} mappings</span>}
            >
              <ul role="list" className="max-h-[28rem] space-y-2 overflow-y-auto p-2.5 sm:p-3.5">
                {group.mappings.map((mapping, index) => (
                  <MappingRow
                    key={`${group.senderKey}-${mapping.sourceHeader}-${mapping.canonicalField}-${index}`}
                    mapping={mapping}
                  />
                ))}
              </ul>
            </SectionCard>
          ))
        )}
      </div>
    </PageContainer>
  );
}

