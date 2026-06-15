"use client";

import Link from "next/link";
import { api } from "@/lib/api/client";
import { useApi } from "@/lib/use-api";
import { PageContainer, PageHeader, SectionCard } from "@/components/ui/page";
import { ErrorBanner, Skeleton } from "@/components/ui/states";
import { WorkQueue } from "@/components/workspace/work-queue";

export function DocPicker() {
  const { data, error, loading, refresh } = useApi((signal) => api.listDocuments(signal));
  const documents = data ?? [];

  return (
    <PageContainer>
      <PageHeader
        title="Review"
        subtitle="Select a document to open the source-cited split-view and reconcile control totals."
      />
      {error && <ErrorBanner message={`Could not reach the API: ${error}`} onRetry={refresh} />}
      {loading && !data ? (
        <div className="flex flex-col gap-2">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-12" />
          ))}
        </div>
      ) : (
        <SectionCard title="Documents" meta={<span className="tabular">{documents.length}</span>}>
          <WorkQueue documents={documents} loading={false} />
        </SectionCard>
      )}
      <p className="mt-3 text-xs text-subtle-foreground">
        <Link href="/" className="text-primary hover:underline">
          Go to the workspace
        </Link>{" "}
        to ingest more documents.
      </p>
    </PageContainer>
  );
}
