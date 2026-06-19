"use client";

import Link from "next/link";
import { api } from "@/lib/api/client";
import { useApi } from "@/lib/use-api";
import { PageContainer, PageHeader, SectionCard } from "@/components/ui/page";
import { ErrorBanner, Skeleton } from "@/components/ui/states";
import { Button } from "@/components/ui/primitives";
import { WorkQueue } from "@/components/workspace/work-queue";
import { IconUpload } from "@/components/ui/icons";

export function DocPicker() {
  const { data, error, loading, refresh } = useApi((signal) => api.listDocuments(signal));
  const documents = data ?? [];

  return (
    <PageContainer fill>
      <PageHeader
        title="Review"
        subtitle="Select a document to open the source-cited split-view and reconcile control totals."
        actions={
          <Link href="/">
            <Button variant="outline" size="sm">
              <IconUpload width={15} height={15} />
              Ingest documents
            </Button>
          </Link>
        }
      />
      {error && (
        <div className="pb-4">
          <ErrorBanner message={`Could not reach the API: ${error}`} onRetry={refresh} />
        </div>
      )}
      {loading && !data ? (
        <div className="flex flex-col gap-2">
          {Array.from({ length: 6 }).map((_, index) => (
            <Skeleton key={index} className="h-[3.25rem]" />
          ))}
        </div>
      ) : (
        <SectionCard
          fill
          title="Documents"
          meta={<span className="font-mono tabular">{documents.length}</span>}
          bodyClassName="bg-surface"
        >
          <WorkQueue documents={documents} loading={false} />
        </SectionCard>
      )}
    </PageContainer>
  );
}
