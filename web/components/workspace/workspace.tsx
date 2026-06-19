"use client";

import { api } from "@/lib/api/client";
import { useApi } from "@/lib/use-api";
import { PageContainer, PageHeader, SectionCard } from "@/components/ui/page";
import { ErrorBanner } from "@/components/ui/states";
import { Button } from "@/components/ui/primitives";
import { KpiStrip } from "@/components/workspace/kpi-strip";
import { UploadZone } from "@/components/workspace/upload-zone";
import { WorkQueue } from "@/components/workspace/work-queue";
import {
  ConfidenceChart,
  ExceptionsChart,
  ThroughputChart,
} from "@/components/workspace/charts";

function LegendSwatch({ label, color }: { label: string; color: string }) {
  return (
    <span className="flex items-center gap-1.5 text-[10px] uppercase tracking-[0.06em] text-subtle-foreground">
      <span
        aria-hidden="true"
        className="size-1.5 rounded-[1px]"
        style={{ backgroundColor: `var(${color})` }}
      />
      {label}
    </span>
  );
}

export function Workspace() {
  const { data, error, loading, refresh } = useApi((signal) => api.listDocuments(signal));
  const documents = data ?? [];
  const chartsLoading = loading && !data;

  return (
    <PageContainer fill>
      <PageHeader
        title="Workspace"
        subtitle="Ingest, extract, and triage reinsurance bordereaux — local-first and source-cited."
        actions={
          <Button variant="outline" size="sm" onClick={refresh}>
            Refresh
          </Button>
        }
      />

      <div className="flex min-h-0 flex-1 flex-col gap-5">
        <KpiStrip documents={documents} loading={chartsLoading} />

        <UploadZone onUploaded={refresh} />

        {error && <ErrorBanner message={`Could not reach the API: ${error}`} onRetry={refresh} />}

        <section aria-label="Workspace analytics" className="grid grid-cols-1 gap-px overflow-hidden rounded-lg border border-border bg-border xl:grid-cols-2">
          <SectionCard
            className="rounded-none border-0"
            title="Throughput"
            meta={
              <>
                <LegendSwatch label="Reviewed" color="--accent" />
                <LegendSwatch label="Pending" color="--warning" />
                <span className="font-mono tabular text-subtle-foreground">14d</span>
              </>
            }
          >
            <ThroughputChart documents={documents} loading={chartsLoading} />
          </SectionCard>

          <SectionCard
            className="rounded-none border-0"
            title="Confidence distribution"
            meta={<span className="font-mono tabular text-subtle-foreground">by tier</span>}
          >
            <ConfidenceChart documents={documents} loading={chartsLoading} />
          </SectionCard>

          <SectionCard
            className="rounded-none border-0 xl:col-span-2"
            title="Exceptions by document type"
            meta={<span className="font-mono tabular text-subtle-foreground">open flags</span>}
          >
            <ExceptionsChart documents={documents} loading={chartsLoading} />
          </SectionCard>
        </section>

        <SectionCard
          fill
          title="Work queue"
          meta={<span className="font-mono tabular">{documents.length} documents</span>}
          bodyClassName="bg-surface"
        >
          <WorkQueue documents={documents} loading={chartsLoading} />
        </SectionCard>
      </div>
    </PageContainer>
  );
}
