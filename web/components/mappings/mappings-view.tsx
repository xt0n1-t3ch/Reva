"use client";

import { useMemo, useState } from "react";
import { api } from "@/lib/api/client";
import type { SchemaMapping } from "@/lib/api/types";
import { confidenceTier, formatPercent, humanizeEnum, type ConfidenceTier } from "@/lib/format";
import { useApi } from "@/lib/use-api";
import { Badge, Button, ConfidenceMeter, Dot, tierTone } from "@/components/ui/primitives";
import { PageContainer, PageHeader, SectionCard } from "@/components/ui/page";
import { EmptyState, ErrorBanner, Skeleton } from "@/components/ui/states";
import { Dialog, DialogContent, DialogHeader, DialogBody, DialogFooter, DialogClose } from "@/components/ui/dialog";
import { Field, Input } from "@/components/ui/form";
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
} from "@/components/ui/dropdown-menu";
import { IconMappings, IconMore, IconPencil, IconSearch } from "@/components/ui/icons";

const defaultSenderLabel = "Default";
const emptyValueLabel = "No normalized sample";
const fallbackHeaderLabel = "Unknown header";
const fallbackCanonicalFieldLabel = "Unmapped field";
const maxSenderLabelLength = 64;

const tierLabel: Record<ConfidenceTier, string> = { high: "High", medium: "Med", low: "Low" };

type SenderGroup = { senderKey: string; label: string; mappings: SchemaMapping[] };

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
    const list = groups.get(senderKey) ?? [];
    list.push(mapping);
    groups.set(senderKey, list);
  }
  return Array.from(groups, ([senderKey, list]) => ({
    senderKey,
    label: senderLabel(senderKey),
    mappings: list.toSorted((left, right) =>
      cleanText(left.sourceHeader, fallbackHeaderLabel).localeCompare(cleanText(right.sourceHeader, fallbackHeaderLabel)),
    ),
  })).toSorted((left, right) => left.label.localeCompare(right.label));
};

const matchesQuery = (mapping: SchemaMapping, query: string): boolean => {
  if (!query) {
    return true;
  }
  const haystack = [mapping.sourceHeader, mapping.canonicalField, mapping.normalizedValue, mapping.source]
    .join(" ")
    .toLowerCase();
  return haystack.includes(query.toLowerCase());
};

function MappingSkeletons() {
  return (
    <div className="space-y-4">
      {Array.from({ length: 3 }, (_, index) => (
        <SectionCard key={index} title="Loading sender" meta={<Skeleton className="h-4 w-14" />}>
          <div className="space-y-2 p-3.5">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
          </div>
        </SectionCard>
      ))}
    </div>
  );
}

function MappingTable({ mappings, onOverride }: { mappings: SchemaMapping[]; onOverride: (mapping: SchemaMapping) => void }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[40rem] border-collapse text-left text-sm">
        <thead>
          <tr className="border-b border-border bg-surface-2/30 text-[11px] font-semibold uppercase tracking-[0.08em] text-subtle-foreground">
            <th className="px-4 py-2 font-semibold">Source header</th>
            <th className="px-3.5 py-2 font-semibold">Canonical field</th>
            <th className="px-3.5 py-2 font-semibold">Normalized sample</th>
            <th className="px-3.5 py-2 text-right font-semibold">Confidence</th>
            <th className="px-3.5 py-2 font-semibold">Origin</th>
            <th className="w-10 px-2 py-2" />
          </tr>
        </thead>
        <tbody>
          {mappings.map((mapping, index) => {
            const sourceHeader = cleanText(mapping.sourceHeader, fallbackHeaderLabel);
            const canonicalField = cleanText(mapping.canonicalField, fallbackCanonicalFieldLabel);
            const normalizedValue = cleanText(mapping.normalizedValue, emptyValueLabel);
            const confidence = clampConfidence(mapping.confidence);
            const tier = confidenceTier(confidence);
            return (
              <tr
                key={`${mapping.senderKey}-${sourceHeader}-${canonicalField}-${index}`}
                className="group border-b border-border transition-colors last:border-0 hover:bg-surface-2/50"
              >
                <td className="px-4 py-2.5">
                  <span className="block max-w-[14rem] truncate font-medium" title={sourceHeader}>
                    {sourceHeader}
                  </span>
                </td>
                <td className="px-3.5 py-2.5">
                  <span className="block max-w-[14rem] truncate font-semibold text-foreground" title={canonicalField}>
                    {humanizeEnum(canonicalField)}
                  </span>
                </td>
                <td className="px-3.5 py-2.5">
                  <span className="block max-w-[16rem] truncate font-mono text-xs text-muted-foreground" title={normalizedValue}>
                    {normalizedValue}
                  </span>
                </td>
                <td className="px-3.5 py-2.5">
                  <span className="flex items-center justify-end gap-2">
                    <ConfidenceMeter score={confidence} tier={tier} showValue={false} />
                    <span className="font-mono text-xs tabular text-muted-foreground">{formatPercent(confidence)}</span>
                    <Badge tone={tierTone[tier]}>{tierLabel[tier]}</Badge>
                  </span>
                </td>
                <td className="px-3.5 py-2.5">
                  {mapping.isCorrected ? (
                    <Badge tone="warning">Corrected</Badge>
                  ) : mapping.isLearned ? (
                    <Badge tone="primary">Learned</Badge>
                  ) : (
                    <Badge tone="neutral">{humanizeEnum(cleanText(mapping.source, "Static"))}</Badge>
                  )}
                </td>
                <td className="px-2 py-2.5 text-right">
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="size-8 opacity-0 transition-opacity focus-visible:opacity-100 group-hover:opacity-100 data-[state=open]:opacity-100"
                        aria-label={`Actions for ${sourceHeader}`}
                      >
                        <IconMore width={16} height={16} />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent>
                      <DropdownMenuItem onSelect={() => onOverride(mapping)}>
                        <IconPencil width={14} height={14} />
                        Override mapping
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function OverrideDialog({
  mapping,
  onClose,
}: {
  mapping: SchemaMapping;
  onClose: () => void;
}) {
  const [canonicalField, setCanonicalField] = useState(() => cleanText(mapping.canonicalField, ""));

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent size="sm">
        <DialogHeader
          title="Override mapping"
          description={`Remap "${cleanText(mapping.sourceHeader, fallbackHeaderLabel)}" to a different canonical field.`}
        />
        <DialogBody className="flex flex-col gap-4">
          <Field label="Source header" htmlFor="override-source">
            <Input id="override-source" value={cleanText(mapping.sourceHeader, fallbackHeaderLabel)} disabled />
          </Field>
          <Field
            label="Canonical field"
            htmlFor="override-canonical"
            hint="The reinsurance field this column should map to."
          >
            <Input
              id="override-canonical"
              value={canonicalField}
              onChange={(event) => setCanonicalField(event.target.value)}
              autoFocus
            />
          </Field>
          <p className="rounded-md border border-warning/30 bg-warning-soft px-3 py-2 text-xs text-warning-foreground" role="note">
            Persisting overrides is not available yet — the backend has no mapping update endpoint. This change is
            preview-only and will not be saved.
          </p>
        </DialogBody>
        <DialogFooter>
          <DialogClose asChild>
            <Button variant="ghost" size="sm">
              Cancel
            </Button>
          </DialogClose>
          {/* TODO(backend): wire to api.updateSchemaMapping once PUT /api/schema-mappings exists. */}
          <Button variant="primary" size="sm" disabled title="No mapping update endpoint available">
            Save override
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function MappingsView() {
  const { data, error, loading, refresh } = useApi(api.listSchemaMappings);
  const [query, setQuery] = useState("");
  const [override, setOverride] = useState<SchemaMapping | null>(null);

  const groups = useMemo(() => groupMappings(data ?? []), [data]);
  const filteredGroups = useMemo(
    () =>
      groups
        .map((group) => ({ ...group, mappings: group.mappings.filter((mapping) => matchesQuery(mapping, query)) }))
        .filter((group) => group.mappings.length > 0),
    [groups, query],
  );

  const totalMappings = data?.length ?? 0;
  const learnedCount = useMemo(() => (data ?? []).filter((mapping) => mapping.isLearned).length, [data]);
  const correctedCount = useMemo(() => (data ?? []).filter((mapping) => mapping.isCorrected).length, [data]);

  return (
    <PageContainer>
      <PageHeader
        title="Schema mappings"
        subtitle="How each sender's column headers map to canonical reinsurance fields — learned and refined per sender."
        actions={
          totalMappings > 0 ? (
            <div className="relative">
              <IconSearch
                width={15}
                height={15}
                className="pointer-events-none absolute left-2.5 top-1/2 -translate-y-1/2 text-subtle-foreground"
              />
              <Input
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder="Filter mappings…"
                aria-label="Filter mappings"
                className="h-9 w-56 pl-8"
              />
            </div>
          ) : undefined
        }
      />

      <div className="space-y-4">
        {error && <ErrorBanner message={`Could not load schema mappings: ${error}`} onRetry={refresh} />}

        {!loading && totalMappings > 0 && (
          <div className="flex flex-wrap items-center gap-x-5 gap-y-2 rounded-lg border border-border bg-surface px-4 py-3 text-xs text-muted-foreground transition-colors hover:border-border-strong">
            <span className="flex items-center gap-2">
              <span className="font-mono text-base tabular tracking-tight text-foreground">{totalMappings}</span> mappings across{" "}
              <span className="font-mono tabular text-foreground">{groups.length}</span> senders
            </span>
            <span aria-hidden="true" className="h-3.5 w-px bg-border" />
            <span className="flex items-center gap-1.5">
              <Dot tone="primary" />
              <span className="font-mono tabular text-foreground">{learnedCount}</span> learned
            </span>
            <span className="flex items-center gap-1.5">
              <Dot tone="warning" />
              <span className="font-mono tabular text-foreground">{correctedCount}</span> corrected
            </span>
          </div>
        )}

        {loading && !data ? (
          <MappingSkeletons />
        ) : groups.length === 0 && !error ? (
          <EmptyState
            icon={<IconMappings width={18} height={18} />}
            title="No learned mappings yet"
            description="Mappings appear here as the system learns each sender's column layout. Ingest a few documents from the same sender to seed them."
          />
        ) : filteredGroups.length === 0 ? (
          <EmptyState
            icon={<IconSearch width={18} height={18} />}
            title="No mappings match your filter"
            description="Try a different header, field, or sender name."
            action={
              <Button variant="outline" size="sm" onClick={() => setQuery("")}>
                Clear filter
              </Button>
            }
          />
        ) : (
          filteredGroups.map((group) => (
            <SectionCard
              key={group.senderKey || defaultSenderLabel}
              title={group.label}
              meta={<span className="font-mono tabular">{group.mappings.length} mappings</span>}
            >
              <MappingTable mappings={group.mappings} onOverride={setOverride} />
            </SectionCard>
          ))
        )}
      </div>

      {override && <OverrideDialog mapping={override} onClose={() => setOverride(null)} />}
    </PageContainer>
  );
}
