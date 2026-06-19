"use client";

import { useMemo, useState } from "react";
import { api } from "@/lib/api/client";
import type { ExportColumn, ExportFormat, ExportTemplate, ExportTemplateDraft } from "@/lib/api/types";
import { useApi } from "@/lib/use-api";
import { PageContainer, PageHeader, SectionCard } from "@/components/ui/page";
import { ErrorBanner, EmptyState, Skeleton } from "@/components/ui/states";
import { Badge, Button } from "@/components/ui/primitives";
import { Dialog, DialogContent, DialogHeader, DialogBody, DialogFooter, DialogClose } from "@/components/ui/dialog";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";
import { Field, Input, Select } from "@/components/ui/form";
import {
  IconArrowDown,
  IconArrowUp,
  IconCopy,
  IconDocument,
  IconExport,
  IconMore,
  IconPencil,
  IconPlus,
  IconTrash,
} from "@/components/ui/icons";

const exportFormats: ExportFormat[] = ["Csv", "Excel", "Json"];
const formatLabels: Record<ExportFormat, string> = { Csv: "CSV", Excel: "Excel", Json: "JSON" };
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

function ColumnEditor({
  draft,
  busy,
  onChange,
}: {
  draft: ExportTemplateDraft;
  busy: boolean;
  onChange: (draft: ExportTemplateDraft) => void;
}) {
  const updateColumn = (index: number, patch: Partial<ExportColumn>) => {
    onChange({
      ...draft,
      columns: draft.columns.map((column, i) => (i === index ? { ...column, ...patch } : column)),
    });
  };
  const moveColumn = (index: number, direction: -1 | 1) => {
    const next = index + direction;
    if (next < 0 || next >= draft.columns.length) {
      return;
    }
    const columns = [...draft.columns];
    const [moved] = columns.splice(index, 1);
    columns.splice(next, 0, moved);
    onChange({ ...draft, columns });
  };
  const removeColumn = (index: number) => {
    onChange({ ...draft, columns: draft.columns.filter((_, i) => i !== index) });
  };

  return (
    <div className="flex flex-col gap-2.5">
      <div className="flex items-center justify-between">
        <div className="flex items-baseline gap-2">
          <h4 className="text-xs font-semibold uppercase tracking-wider text-subtle-foreground">Columns</h4>
          <span className="font-mono text-[11px] tabular text-subtle-foreground">{draft.columns.length}</span>
        </div>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => onChange({ ...draft, columns: [...draft.columns, { header: "", source: "" }] })}
          disabled={busy}
        >
          <IconPlus width={14} height={14} />
          Add column
        </Button>
      </div>

      <datalist id="template-source-suggestions">
        {sourceSuggestions.map((source) => (
          <option key={source} value={source} />
        ))}
      </datalist>

      <div className="overflow-hidden rounded-md border border-border">
        <div className="grid grid-cols-[2rem_minmax(0,1fr)_minmax(0,1fr)_auto] items-center gap-2 border-b border-border bg-surface-2/60 px-2 py-1.5 text-[11px] font-semibold uppercase tracking-wider text-subtle-foreground">
          <span className="text-center">#</span>
          <span>Header</span>
          <span>Source field</span>
          <span className="pr-1 text-right">Order</span>
        </div>
        {draft.columns.length === 0 ? (
          <p className="px-3 py-4 text-xs text-muted-foreground">Add at least one column to define the export.</p>
        ) : (
          draft.columns.map((column, index) => (
            <div
              key={index}
              className="grid grid-cols-[2rem_minmax(0,1fr)_minmax(0,1fr)_auto] items-center gap-2 border-b border-border px-2 py-2 last:border-0"
            >
              <span className="text-center font-mono text-[11px] tabular text-subtle-foreground">{index + 1}</span>
              <Input
                value={column.header}
                onChange={(event) => updateColumn(index, { header: event.target.value })}
                placeholder="Column header"
                aria-label={`Column ${index + 1} header`}
                className="h-8"
                disabled={busy}
              />
              <Input
                value={column.source}
                onChange={(event) => updateColumn(index, { source: event.target.value })}
                list="template-source-suggestions"
                placeholder="source.field"
                aria-label={`Column ${index + 1} source`}
                className="h-8 font-mono text-xs"
                disabled={busy}
              />
              <div className="flex items-center gap-0.5">
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="size-7"
                  aria-label={`Move column ${index + 1} up`}
                  onClick={() => moveColumn(index, -1)}
                  disabled={busy || index === 0}
                >
                  <IconArrowUp width={14} height={14} />
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="size-7"
                  aria-label={`Move column ${index + 1} down`}
                  onClick={() => moveColumn(index, 1)}
                  disabled={busy || index === draft.columns.length - 1}
                >
                  <IconArrowDown width={14} height={14} />
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="size-7 text-danger hover:bg-danger-soft"
                  aria-label={`Remove column ${index + 1}`}
                  onClick={() => removeColumn(index)}
                  disabled={busy}
                >
                  <IconTrash width={14} height={14} />
                </Button>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}

function TemplateDialog({
  editor,
  error,
  busy,
  onChange,
  onClose,
  onSave,
}: {
  editor: EditorState;
  error: string | null;
  busy: boolean;
  onChange: (draft: ExportTemplateDraft) => void;
  onClose: () => void;
  onSave: () => void;
}) {
  const validationError = validateDraft(editor.draft);
  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent size="lg">
        <DialogHeader
          title={editor.mode === "create" ? "New export template" : "Edit export template"}
          description="Map output headers to canonical source fields, set the order, and pick a format."
        />
        <DialogBody className="flex flex-col gap-5">
          <div className="grid gap-4 sm:grid-cols-[minmax(0,1fr)_12rem]">
            <Field label="Template name" required>
              <Input
                value={editor.draft.name}
                onChange={(event) => onChange({ ...editor.draft, name: event.target.value })}
                placeholder="e.g. Lloyd's bordereau export"
                disabled={busy}
                autoFocus
              />
            </Field>
            <Field label="Format">
              <Select
                value={editor.draft.format}
                onChange={(event) => onChange({ ...editor.draft, format: event.target.value as ExportFormat })}
                disabled={busy}
              >
                {exportFormats.map((format) => (
                  <option key={format} value={format}>
                    {formatLabels[format]}
                  </option>
                ))}
              </Select>
            </Field>
          </div>

          <ColumnEditor draft={editor.draft} busy={busy} onChange={onChange} />

          {(error || validationError) && (
            <p className="rounded-md border border-danger/30 bg-danger-soft px-3 py-2 text-xs text-danger" role="alert">
              {error ?? validationError}
            </p>
          )}
        </DialogBody>
        <DialogFooter>
          <DialogClose asChild>
            <Button variant="ghost" size="sm" disabled={busy}>
              Cancel
            </Button>
          </DialogClose>
          <Button variant="primary" size="sm" onClick={onSave} disabled={busy || Boolean(validationError)}>
            {busy ? "Saving…" : editor.mode === "create" ? "Create template" : "Save changes"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function TemplateRow({
  template,
  selected,
  busy,
  onSelect,
  onEdit,
  onDuplicate,
  onDelete,
}: {
  template: ExportTemplate;
  selected: boolean;
  busy: boolean;
  onSelect: () => void;
  onEdit: () => void;
  onDuplicate: () => void;
  onDelete: () => void;
}) {
  return (
    <div
      className={`group relative flex items-center gap-3 border-b border-border px-4 py-3 transition-colors last:border-0 ${
        selected
          ? "bg-primary-soft shadow-[inset_2px_0_0_0_var(--color-primary)]"
          : "hover:bg-surface-2/60"
      }`}
    >
      <button
        type="button"
        onClick={onSelect}
        className="flex min-w-0 flex-1 flex-col items-start gap-1 text-left"
        aria-pressed={selected}
      >
        <span className="flex w-full min-w-0 items-center gap-2">
          <span className="min-w-0 truncate text-sm font-medium" title={template.name}>
            {template.name}
          </span>
          {template.isBuiltIn ? (
            <Badge tone="neutral">Built-in</Badge>
          ) : (
            <Badge tone="primary">Custom</Badge>
          )}
        </span>
        <span className="flex items-center gap-2 text-[11px] text-subtle-foreground">
          <span className="font-mono uppercase tabular">{formatLabels[template.format]}</span>
          <span aria-hidden="true">·</span>
          <span className="tabular">{template.columns.length} columns</span>
          <span className="min-w-0 truncate" title={template.columns.map((c) => c.header).join(" · ")}>
            {template.columns.map((c) => c.header).join(" · ") || "no columns"}
          </span>
        </span>
      </button>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="size-8 shrink-0"
            aria-label={`Actions for ${template.name}`}
            disabled={busy}
          >
            <IconMore width={16} height={16} />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent>
          {!template.isBuiltIn && (
            <DropdownMenuItem onSelect={onEdit}>
              <IconPencil width={14} height={14} />
              Edit
            </DropdownMenuItem>
          )}
          <DropdownMenuItem onSelect={onDuplicate}>
            <IconCopy width={14} height={14} />
            Duplicate{template.isBuiltIn ? " to customize" : ""}
          </DropdownMenuItem>
          {!template.isBuiltIn && (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuItem tone="danger" onSelect={onDelete}>
                <IconTrash width={14} height={14} />
                Delete
              </DropdownMenuItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}

export function ExportView() {
  const templates = useApi((signal) => api.listTemplates(signal));
  const documents = useApi((signal) => api.listDocuments(signal));
  const settings = useApi((signal) => api.getSettings(signal));
  const [templateId, setTemplateId] = useState<string>("");
  const [defaultApplied, setDefaultApplied] = useState(false);
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [pendingDelete, setPendingDelete] = useState<ExportTemplate | null>(null);
  const [templateError, setTemplateError] = useState<string | null>(null);
  const [busyAction, setBusyAction] = useState<string | null>(null);

  const templateList = useMemo(() => templates.data ?? [], [templates.data]);

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
      const saved =
        editor.mode === "edit" && editor.templateId
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

  const confirmDelete = () => {
    const template = pendingDelete;
    if (!template) {
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
      setPendingDelete(null);
      refreshTemplates();
    });
  };

  return (
    <PageContainer fill>
      <PageHeader
        title="Export"
        subtitle="Build reusable templates and download canonical reinsurance fields in CSV, Excel, or JSON."
        actions={
          <Button
            variant="primary"
            size="sm"
            onClick={() => {
              setTemplateError(null);
              setEditor({ mode: "create", templateId: null, draft: createDraft() });
            }}
            disabled={Boolean(busyAction)}
          >
            <IconPlus width={15} height={15} />
            New template
          </Button>
        }
      />

      {(templates.error || documents.error) && (
        <div className="pb-4">
          <ErrorBanner
            message={`Could not reach the API: ${templates.error ?? documents.error}`}
            onRetry={() => {
              templates.refresh();
              documents.refresh();
            }}
          />
        </div>
      )}

      {templateError && !editor && (
        <p className="mb-4 rounded-md border border-danger/30 bg-danger-soft px-3 py-2 text-xs text-danger" role="alert">
          {templateError}
        </p>
      )}

      <div className="grid min-h-0 flex-1 grid-rows-[minmax(18rem,1fr)_minmax(18rem,1fr)] gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.15fr)] lg:grid-rows-1">

        <SectionCard
          fill
          title="Templates"
          meta={<span className="font-mono tabular">{templateList.length}</span>}
          bodyClassName="bg-surface"
        >
          {templates.loading && !templates.data ? (
            <div className="flex flex-col gap-2 p-3">
              {Array.from({ length: 4 }).map((_, index) => (
                <Skeleton key={index} className="h-[3.25rem]" />
              ))}
            </div>
          ) : templateList.length === 0 ? (
            <div className="p-4">
              <EmptyState
                icon={<IconExport width={20} height={20} />}
                title="No templates yet"
                description="Create a template to control exactly which columns and source fields each export contains."
                action={
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setEditor({ mode: "create", templateId: null, draft: createDraft() })}
                  >
                    <IconPlus width={14} height={14} />
                    New template
                  </Button>
                }
              />
            </div>
          ) : (
            <div role="list">
              {templateList.map((template) => (
                <TemplateRow
                  key={template.id}
                  template={template}
                  selected={template.id === templateId}
                  busy={Boolean(busyAction)}
                  onSelect={() => setTemplateId(template.id === templateId ? "" : template.id)}
                  onEdit={() => {
                    setTemplateError(null);
                    setEditor({ mode: "edit", templateId: template.id, draft: editDraft(template) });
                  }}
                  onDuplicate={() => duplicateTemplate(template)}
                  onDelete={() => setPendingDelete(template)}
                />
              ))}
            </div>
          )}
        </SectionCard>

        <SectionCard
          fill
          title="Download documents"
          meta={
            <span className="flex items-center gap-2">
              <span className="text-[11px] uppercase tracking-wider text-subtle-foreground">Using</span>
              <Select
                value={templateId}
                onChange={(event) => setTemplateId(event.target.value)}
                className="h-7 w-44 text-xs"
                aria-label="Export template"
              >
                <option value="">Default (all fields)</option>
                {templateList.map((template) => (
                  <option key={template.id} value={template.id}>
                    {template.name}
                  </option>
                ))}
              </Select>
            </span>
          }
          bodyClassName="bg-surface"
          data-tour="export-panel"
        >
          {documents.loading && !documents.data ? (
            <div className="flex flex-col gap-2 p-3">
              {Array.from({ length: 5 }).map((_, index) => (
                <Skeleton key={index} className="h-[3.25rem]" />
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
                  className="group flex flex-wrap items-center gap-3 border-b border-border px-4 py-3 transition-colors last:border-0 hover:bg-surface-2/40"
                >
                  <span className="flex min-w-0 flex-1 items-center gap-2.5">
                    <span className="flex size-8 shrink-0 items-center justify-center rounded-md border border-border bg-surface-2 text-subtle-foreground shadow-soft ring-1 ring-inset ring-border/40 transition-colors group-hover:border-border-strong">
                      <IconDocument width={16} height={16} />
                    </span>
                    <span className="truncate text-sm font-medium tracking-tight">{document.fileName}</span>
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

      {editor && (
        <TemplateDialog
          editor={editor}
          error={templateError}
          busy={busyAction === "save"}
          onChange={(draft) => {
            setTemplateError(null);
            setEditor({ ...editor, draft });
          }}
          onClose={() => {
            setTemplateError(null);
            setEditor(null);
          }}
          onSave={saveEditor}
        />
      )}

      <ConfirmDialog
        open={Boolean(pendingDelete)}
        onOpenChange={(open) => !open && setPendingDelete(null)}
        title="Delete template"
        description={pendingDelete ? `"${pendingDelete.name}" will be permanently removed.` : undefined}
        confirmLabel="Delete template"
        destructive
        busy={busyAction?.startsWith("delete-") ?? false}
        onConfirm={confirmDelete}
      />
    </PageContainer>
  );
}
