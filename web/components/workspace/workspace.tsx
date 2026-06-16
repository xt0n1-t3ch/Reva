"use client";

import { api } from "@/lib/api/client";
import { useApi } from "@/lib/use-api";
import { PageContainer, PageHeader, SectionCard } from "@/components/ui/page";
import { ErrorBanner } from "@/components/ui/states";
import { Button } from "@/components/ui/primitives";
import { KpiStrip } from "@/components/workspace/kpi-strip";
import { UploadZone } from "@/components/workspace/upload-zone";
import { WorkQueue } from "@/components/workspace/work-queue";

export function Workspace() {
  const { data, error, loading, refresh } = useApi((signal) => api.listDocuments(signal));
  const documents = data ?? [];

  return (
    <PageContainer>
      <PageHeader
        title="Workspace"
        subtitle="Ingest, extract, and triage reinsurance bordereaux — local-first and source-cited."
        actions={
          <Button variant="outline" size="sm" onClick={refresh}>
            Refresh
          </Button>
        }
      />

      <div className="flex flex-col gap-5">
        <KpiStrip documents={documents} loading={loading && !data} />

        <UploadZone onUploaded={refresh} />

        {error && <ErrorBanner message={`Could not reach the API: ${error}`} onRetry={refresh} />}

        <SectionCard
          title="Work queue"
          meta={<span className="tabular">{documents.length} documents</span>}
        >
          <WorkQueue documents={documents} loading={loading && !data} />
        </SectionCard>
      </div>
    </PageContainer>
  );
}
