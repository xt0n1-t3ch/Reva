"use client";

import { useState } from "react";
import { api } from "@/lib/api/client";
import type { ExportTemplate } from "@/lib/api/types";
import { useApi } from "@/lib/use-api";
import { PageContainer, PageHeader, SectionCard } from "@/components/ui/page";
import { ErrorBanner, EmptyState, Skeleton } from "@/components/ui/states";
import { Badge, Button } from "@/components/ui/primitives";
import { IconDocument, IconExport } from "@/components/ui/icons";

function TemplateCard({ template }: { template: ExportTemplate }) {
  return (
    <div className="rounded-lg border border-border bg-surface p-3.5 shadow-soft">
      <div className="flex items-start justify-between gap-2">
        <h3 className="text-sm font-semibold">{template.name}</h3>
        {template.isBuiltIn && <Badge tone="primary">Built-in</Badge>}
      </div>
      <div className="mt-2 flex items-center gap-2 text-xs text-muted-foreground">
        <Badge tone="neutral">{template.format}</Badge>
        <span className="tabular">{template.columns.length} columns</span>
      </div>
      <p className="mt-2 truncate text-[11px] text-subtle-foreground">
        {template.columns.map((column) => column.header).join(" · ") || "No columns defined"}
      </p>
    </div>
  );
}

export function ExportView() {
  const templates = useApi((signal) => api.listTemplates(signal));
  const documents = useApi((signal) => api.listDocuments(signal));
  const [templateId, setTemplateId] = useState<string>("");

  const templateList = templates.data ?? [];
  const documentList = documents.data ?? [];

  return (
    <PageContainer>
      <PageHeader
        title="Export"
        subtitle="Apply reusable templates and download canonical reinsurance fields in CSV, Excel, or JSON."
      />

      {(templates.error || documents.error) && (
        <ErrorBanner
          message={`Could not reach the API: ${templates.error ?? documents.error}`}
          onRetry={() => {
            templates.refresh();
            documents.refresh();
          }}
        />
      )}

      <div className="flex flex-col gap-5">
        <section className="flex flex-col gap-3">
          <h3 className="text-xs font-semibold uppercase tracking-wider text-subtle-foreground">Templates</h3>
          {templates.loading && !templates.data ? (
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {Array.from({ length: 3 }).map((_, index) => (
                <Skeleton key={index} className="h-28" />
              ))}
            </div>
          ) : templateList.length === 0 ? (
            <EmptyState
              icon={<IconExport width={20} height={20} />}
              title="No templates configured"
              description="Default all-field exports are still available for ingested documents."
            />
          ) : (
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {templateList.map((template) => (
                <TemplateCard key={template.id} template={template} />
              ))}
            </div>
          )}
        </section>

        <div data-tour="export-panel">
        <SectionCard
          title="Download documents"
          meta={
            <label className="flex items-center gap-2">
              <span className="text-[11px] uppercase tracking-wider text-subtle-foreground">Template</span>
              <select
                value={templateId}
                onChange={(event) => setTemplateId(event.target.value)}
                className="rounded-md border border-input bg-surface px-2 py-1 text-xs"
              >
                <option value="">Default (all fields)</option>
                {templateList.map((template) => (
                  <option key={template.id} value={template.id}>
                    {template.name}
                  </option>
                ))}
              </select>
            </label>
          }
        >
          {documents.loading && !documents.data ? (
            <div className="flex flex-col gap-2 p-3">
              {Array.from({ length: 4 }).map((_, index) => (
                <Skeleton key={index} className="h-12" />
              ))}
            </div>
          ) : documentList.length === 0 ? (
            <div className="p-4">
              <EmptyState
                icon={<IconDocument width={20} height={20} />}
                title="No documents to export"
                description="Ingest a document in the workspace first."
              />
            </div>
          ) : (
            <ul role="list">
              {documentList.map((document) => (
                <li
                  key={document.id}
                  className="flex flex-wrap items-center gap-3 border-b border-border px-3.5 py-2.5 last:border-0"
                >
                  <span className="flex min-w-0 flex-1 items-center gap-2.5">
                    <span className="flex size-8 shrink-0 items-center justify-center rounded-md bg-surface-2 text-muted-foreground">
                      <IconDocument width={16} height={16} />
                    </span>
                    <span className="truncate text-sm font-medium">{document.fileName}</span>
                  </span>
                  <span className="flex items-center gap-2">
                    {templateId ? (
                      <a href={api.exportUrl(document.id, "csv", templateId)} target="_blank" rel="noreferrer">
                        <Button variant="outline" size="sm">
                          <IconExport width={14} height={14} />
                          Template
                        </Button>
                      </a>
                    ) : (
                      <>
                        <a href={api.exportUrl(document.id, "csv")} target="_blank" rel="noreferrer">
                          <Button variant="outline" size="sm">
                            CSV
                          </Button>
                        </a>
                        <a href={api.exportUrl(document.id, "json")} target="_blank" rel="noreferrer">
                          <Button variant="outline" size="sm">
                            JSON
                          </Button>
                        </a>
                      </>
                    )}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </SectionCard>
        </div>
      </div>
    </PageContainer>
  );
}
