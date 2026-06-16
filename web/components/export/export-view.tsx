"use client";

import { useMemo, useState } from "react";
import { api } from "@/lib/api/client";
import type { ExportColumn, ExportFormat, ExportTemplate, ExportTemplateDraft } from "@/lib/api/types";
import { useApi } from "@/lib/use-api";
import { PageContainer, PageHeader, SectionCard } from "@/components/ui/page";
import { ErrorBanner, EmptyState, Skeleton } from "@/components/ui/states";
import { Badge, Button } from "@/components/ui/primitives";
import { IconDocument, IconExport } from "@/components/ui/icons";

const exportFormats: ExportFormat[] = ["Csv", "Excel", "Json"];
const formatLabels: Record<ExportFormat, string> = {
  Csv: "CSV",
  Excel: "Excel",
  Json: "JSON",
};
const defaultColumn: ExportColumn = { header: "Policy Number", source: "policy.number" };
const sourceSuggestions = [
  "document.fileName",
  "document.documentType",
  "document.reviewState",
  "policy.number",
  "insured.name",
  "cedant.name",
  "broker.name",
  "period.effectiveDate",
  "period.expirationDate",
  "premium.gross",
  "limit.amount",
  "deductible.amount",
];

type EditorState = {
  mode: "create" | "edit";
  templateId: string | null;
  draft: ExportTemplateDraft;
};

const createDraft = (template?: ExportTemplate): ExportTemplateDraft => ({
  name: template ? `${template.name} copy` : "",
  format: template?.format ?? "Csv",
  columns: template?.columns.length ? template.columns.map((column) => ({ ...column })) : [{ ...defaultColumn }],
});

const editDraft = (template: ExportTemplate): ExportTemplateDraft => ({
  name: template.name,
  format: template.format,
  columns: template.columns.length ? template.columns.map((column) => ({ ...column })) : [{ ...defaultColumn }],
});

const validateDraft = (draft: ExportTemplateDraft): string | null => {
  if (!draft.name.trim()) {
    return "Template name is required.";
  }
  if (draft.columns.length === 0) {
    return "Add at least one column.";
  }
  if (draft.columns.some((column) => !column.header.trim())) {
    return "Every column needs a header.";
  }
  const normalizedHeaders = draft.columns.map((column) => column.header.trim().toLocaleLowerCase());
  if (new Set(normalizedHeaders).size !== normalizedHeaders.length) {
    return "Column headers must be unique.";
  }
  return null;
};

function TemplateCard({
  template,
  onEdit,
  onDuplicate,
  onDelete,
  busy,
}: {
  template: ExportTemplate;
  onEdit: (template: ExportTemplate) => void;
  onDuplicate: (template: ExportTemplate) => void;
  onDelete: (template: ExportTemplate) => void;
  busy: boolean;
}) {
  return (
    <div className="rounded-lg border border-border bg-surface p-3.5 shadow-soft">
      <div className="flex items-start justify-between gap-2">
        <h3 className="min-w-0 truncate text-sm font-semibold" title={template.name}>
          {template.name}
        </h3>
        {template.isBuiltIn && <Badge tone="primary">Built-in</Badge>}
      </div>
      <div className="mt-2 flex items-center gap-2 text-xs text-muted-foreground">
        <Badge tone="neutral">{formatLabels[template.format]}</Badge>
        <span className="tabular">{template.columns.length} columns</span>
      </div>
      <p className="mt-2 truncate text-[11px] text-subtle-foreground" title={template.columns.map((column) => column.header).join(" · ")}>
        {template.columns.map((column) => column.header).join(" · ") || "No columns defined"}
      </p>
      <div className="mt-3 flex flex-wrap gap-2">
        {!template.isBuiltIn && (
          <Button type="button" variant="outline" size="sm" onClick={() => onEdit(template)} disabled={busy}>
            Edit
          </Button>
        )}
        <Button type="button" variant="subtle" size="sm" onClick={() => onDuplicate(template)} disabled={busy}>
          Duplicate
        </Button>
        {!template.isBuiltIn && (
          <Button type="button" variant="danger" size="sm" onClick={() => onDelete(template)} disabled={busy}>
            Delete
          </Button>
        )}
      </div>
    </div>
  );
}

function TemplateEditor({
  editor,
  error,
  busy,
  onChange,
  onCancel,
  onSave,
}: {
  editor: EditorState;
  error: string | null;
  busy: boolean;
  onChange: (draft: ExportTemplateDraft) => void;
  onCancel: () => void;
  onSave: () => void;
}) {
  const validationError = validateDraft(editor.draft);
  const updateColumn = (index: number, patch: Partial<ExportColumn>) => {
    onChange({
      ...editor.draft,
      columns: editor.draft.columns.map((column, columnIndex) =>
        columnIndex === index ? { ...column, ...patch } : column,
      ),
    });
  };
  const moveColumn = (index: number, direction: -1 | 1) => {
    const nextIndex = index + direction;
    if (nextIndex < 0 || nextIndex >= editor.draft.columns.length) {
      return;
    }
    const columns = [...editor.draft.columns];
    const [column] = columns.splice(index, 1);
    columns.splice(nextIndex, 0, column);
    onChange({ ...editor.draft, columns });
  };
  const removeColumn = (index: number) => {
    onChange({ ...editor.draft, columns: editor.draft.columns.filter((_, columnIndex) => columnIndex !== index) });
  };

  return (
    <section className="rounded-lg border border-primary-border bg-surface p-4 shadow-soft" aria-labelledby="template-editor-title">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h3 id="template-editor-title" className="text-sm font-semibold">
            {editor.mode === "create" ? "New template" : "Edit template"}
          </h3>
          <p className="mt-1 text-xs text-subtle-foreground">Define headers, sources, order, and export format.</p>
        </div>
        <div className="flex gap-2">
          <Button type="button" variant="ghost" size="sm" onClick={onCancel} disabled={busy}>
            Cancel
          </Button>
          <Button type="button" variant="primary" size="sm" onClick={onSave} disabled={busy || Boolean(validationError)}>
            {busy ? "Saving" : "Save"}
          </Button>
        </div>
      </div>

      {(error || validationError) && (
        <p className="mt-3 rounded-md border border-danger/30 bg-danger-soft px-3 py-2 text-xs text-danger" role="alert">
          {error ?? validationError}
        </p>
      )}

      <div className="mt-4 grid gap-3 md:grid-cols-[minmax(0,1fr)_10rem]">
        <label className="flex flex-col gap-1.5 text-xs font-medium text-muted-foreground">
          Name
          <input
            value={editor.draft.name}
            onChange={(event) => onChange({ ...editor.draft, name: event.target.value })}
            className="min-h-9 rounded-md border border-input bg-surface px-3 py-2 text-sm text-foreground"
            disabled={busy}
          />
        </label>
        <label className="flex flex-col gap-1.5 text-xs font-medium text-muted-foreground">
          Format
          <select
            value={editor.draft.format}
            onChange={(event) => onChange({ ...editor.draft, format: event.target.value as ExportFormat })}
            className="min-h-9 rounded-md border border-input bg-surface px-3 py-2 text-sm text-foreground"
            disabled={busy}
          >
            {exportFormats.map((format) => (
              <option key={format} value={format}>
                {formatLabels[format]}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="mt-4 flex items-center justify-between gap-3">
        <h4 className="text-xs font-semibold uppercase tracking-wider text-subtle-foreground">Columns</h4>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => onChange({ ...editor.draft, columns: [...editor.draft.columns, { header: "", source: "" }] })}
          disabled={busy}
        >
          Add column
        </Button>
      </div>

      <datalist id="template-source-suggestions">
        {sourceSuggestions.map((source) => (
          <option key={source} value={source} />
        ))}
      </datalist>

      <div className="mt-2 flex flex-col gap-2">
        {editor.draft.columns.map((column, index) => (
          <div key={index} className="grid gap-2 rounded-md border border-border bg-surface-2 p-2 md:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_auto]">
            <label className="flex flex-col gap-1 text-[11px] font-medium text-muted-foreground">
              Header
              <input
                value={column.header}
                onChange={(event) => updateColumn(index, { header: event.target.value })}
                className="min-h-8 rounded-md border border-input bg-surface px-2 py-1.5 text-sm text-foreground"
                disabled={busy}
              />
            </label>
            <label className="flex flex-col gap-1 text-[11px] font-medium text-muted-foreground">
              Source
              <input
                value={column.source}
                onChange={(event) => updateColumn(index, { source: event.target.value })}
                list="template-source-suggestions"
                className="min-h-8 rounded-md border border-input bg-surface px-2 py-1.5 text-sm text-foreground"
                disabled={busy}
              />
            </label>
            <div className="flex items-end gap-1">
              <Button type="button" variant="ghost" size="sm" onClick={() => moveColumn(index, -1)} disabled={busy || index === 0}>
                Up
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => moveColumn(index, 1)}
                disabled={busy || index === editor.draft.columns.length - 1}
              >
                Down
              </Button>
              <Button type="button" variant="danger" size="sm" onClick={() => removeColumn(index)} disabled={busy}>
                Remove
              </Button>
            </div>
          </div>
        ))}
      </div>

      <div className="mt-4 overflow-x-auto rounded-md border border-border">
        <table className="w-full min-w-96 text-left text-xs">
          <thead className="bg-surface-2 text-muted-foreground">
            <tr>
              {editor.draft.columns.length === 0 ? (
                <th className="px-3 py-2 font-medium">No preview columns</th>
              ) : (
                editor.draft.columns.map((column, index) => (
                  <th key={`${column.header}-${index}`} className="px-3 py-2 font-medium">
                    {column.header.trim() || "Untitled"}
                  </th>
                ))
              )}
            </tr>
          </thead>
          <tbody>
            <tr className="border-t border-border text-subtle-foreground">
              {editor.draft.columns.length === 0 ? (
                <td className="px-3 py-2">Add columns to preview sources.</td>
              ) : (
                editor.draft.columns.map((column, index) => (
                  <td key={`${column.source}-${index}`} className="px-3 py-2">
                    {column.source.trim() || "No source"}
                  </td>
                ))
              )}
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  );
}

export function ExportView() {
  const templates = useApi((signal) => api.listTemplates(signal));
  const documents = useApi((signal) => api.listDocuments(signal));
  const settings = useApi((signal) => api.getSettings(signal));
  const [templateId, setTemplateId] = useState<string>("");
  const [defaultApplied, setDefaultApplied] = useState(false);
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [templateError, setTemplateError] = useState<string | null>(null);
  const [busyAction, setBusyAction] = useState<string | null>(null);

  const templateList = useMemo(() => templates.data ?? [], [templates.data]);

  // Pre-select the configured default export template once both lists have loaded.
  if (!defaultApplied && settings.data && templateList.length > 0) {
    setDefaultApplied(true);
    const preferred = settings.data.defaultTemplateId;
    if (preferred && templateList.some((template) => template.id === preferred)) {
      setTemplateId(preferred);
    }
  }

  const documentList = documents.data ?? [];
  const selectedTemplate = useMemo(
    () => templateList.find((template) => template.id === templateId) ?? null,
    [templateId, templateList],
  );

  const refreshTemplates = () => {
    templates.refresh();
    setTemplateError(null);
  };
  const runTemplateAction = async (actionId: string, action: () => Promise<void>) => {
    if (busyAction) {
      return;
    }
    setBusyAction(actionId);
    setTemplateError(null);
    try {
      await action();
    } catch (cause: unknown) {
      setTemplateError(cause instanceof Error ? cause.message : "Template request failed");
    } finally {
      setBusyAction(null);
    }
  };

  const saveEditor = () => {
    if (!editor) {
      return;
    }
    const validationError = validateDraft(editor.draft);
    if (validationError) {
      setTemplateError(validationError);
      return;
    }
    void runTemplateAction("save", async () => {
      const draft = {
        ...editor.draft,
        name: editor.draft.name.trim(),
        columns: editor.draft.columns.map((column) => ({ header: column.header.trim(), source: column.source.trim() })),
      };
      const saved = editor.mode === "edit" && editor.templateId
        ? await api.updateTemplate(editor.templateId, draft)
        : await api.createTemplate(draft);
      setTemplateId(saved.id);
      setEditor(null);
      refreshTemplates();
    });
  };

  const duplicateTemplate = (template: ExportTemplate) => {
    void runTemplateAction(`duplicate-${template.id}`, async () => {
      const duplicated = await api.duplicateTemplate(template.id);
      setTemplateId(duplicated.id);
      setEditor({ mode: "edit", templateId: duplicated.id, draft: editDraft(duplicated) });
      refreshTemplates();
    });
  };

  const deleteTemplate = (template: ExportTemplate) => {
    if (template.isBuiltIn || !window.confirm(`Delete template "${template.name}"?`)) {
      return;
    }
    void runTemplateAction(`delete-${template.id}`, async () => {
      await api.deleteTemplate(template.id);
      if (templateId === template.id) {
        setTemplateId("");
      }
      if (editor?.templateId === template.id) {
        setEditor(null);
      }
      refreshTemplates();
    });
  };

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
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h3 className="text-xs font-semibold uppercase tracking-wider text-subtle-foreground">Templates</h3>
            <Button
              type="button"
              variant="primary"
              size="sm"
              onClick={() => {
                setTemplateError(null);
                setEditor({ mode: "create", templateId: null, draft: createDraft() });
              }}
              disabled={Boolean(busyAction)}
            >
              New template
            </Button>
          </div>

          {templateError && !editor && (
            <p className="rounded-md border border-danger/30 bg-danger-soft px-3 py-2 text-xs text-danger" role="alert">
              {templateError}
            </p>
          )}

          {editor && (
            <TemplateEditor
              editor={editor}
              error={templateError}
              busy={Boolean(busyAction)}
              onChange={(draft) => {
                setTemplateError(null);
                setEditor({ ...editor, draft });
              }}
              onCancel={() => {
                setTemplateError(null);
                setEditor(null);
              }}
              onSave={saveEditor}
            />
          )}

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
                <TemplateCard
                  key={template.id}
                  template={template}
                  onEdit={(templateToEdit) => {
                    setTemplateError(null);
                    setEditor({ mode: "edit", templateId: templateToEdit.id, draft: editDraft(templateToEdit) });
                  }}
                  onDuplicate={duplicateTemplate}
                  onDelete={deleteTemplate}
                  busy={Boolean(busyAction)}
                />
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
                            {selectedTemplate ? formatLabels[selectedTemplate.format] : "Template"}
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
