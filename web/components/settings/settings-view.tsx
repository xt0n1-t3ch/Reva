"use client";

import { useState } from "react";
import { cn } from "@/lib/cn";
import { api } from "@/lib/api/client";
import type { AppSettings } from "@/lib/api/types";
import { useApi } from "@/lib/use-api";
import { PageContainer, PageHeader, SectionCard } from "@/components/ui/page";
import { ErrorBanner, Skeleton } from "@/components/ui/states";
import { Badge, Button, Dot } from "@/components/ui/primitives";

const DEFAULT_ACCENT = "#3b5bd6";

const applyAccent = (hex: string) => {
  const root = document.documentElement;
  if (hex) {
    root.style.setProperty("--accent", hex);
  } else {
    root.style.removeProperty("--accent");
  }
};

function Field({
  label,
  hint,
  children,
}: {
  label: string;
  hint?: string;
  children: React.ReactNode;
}) {
  return (
    <label className="flex flex-col gap-1.5">
      <span className="text-sm font-medium">{label}</span>
      {children}
      {hint && <span className="text-[11px] text-subtle-foreground">{hint}</span>}
    </label>
  );
}

export function SettingsView() {
  const { data, error, loading, refresh } = useApi((signal) => api.getSettings(signal));
  const sources = useApi((signal) => api.listInboundSources(signal));
  const templates = useApi((signal) => api.listTemplates(signal));
  const [form, setForm] = useState<AppSettings | null>(null);
  const [seed, setSeed] = useState<AppSettings | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [maintenanceBusy, setMaintenanceBusy] = useState<"reseed" | "clear" | null>(null);
  const [maintenanceResult, setMaintenanceResult] = useState<string | null>(null);

  if (data && data !== seed) {
    setSeed(data);
    setForm(data);
  }

  const update = <K extends keyof AppSettings>(key: K, value: AppSettings[K]) => {
    setForm((current) => (current ? { ...current, [key]: value } : current));
    setSaved(false);
    if (key === "accentColor") {
      applyAccent(value as string);
    }
  };

  const save = async () => {
    if (!form) {
      return;
    }
    setSaving(true);
    setSaveError(null);
    try {
      const result = await api.saveSettings(form);
      setForm(result);
      setSaved(true);
    } catch (cause) {
      setSaveError(cause instanceof Error ? cause.message : "Save failed");
    } finally {
      setSaving(false);
    }
  };

  const reseed = async () => {
    setMaintenanceBusy("reseed");
    setMaintenanceResult(null);
    try {
      const { seeded } = await api.reseedDemo();
      setMaintenanceResult(seeded ? "Demo corpus reseeded." : "Workspace already has documents — clear it first to reseed.");
    } catch (cause) {
      setMaintenanceResult(cause instanceof Error ? cause.message : "Reseed failed.");
    } finally {
      setMaintenanceBusy(null);
    }
  };

  const clear = async () => {
    if (!window.confirm("Delete every document, extraction, and review record? This cannot be undone.")) {
      return;
    }
    setMaintenanceBusy("clear");
    setMaintenanceResult(null);
    try {
      const { deleted } = await api.clearDocuments();
      setMaintenanceResult(`Cleared ${deleted} document${deleted === 1 ? "" : "s"}.`);
    } catch (cause) {
      setMaintenanceResult(cause instanceof Error ? cause.message : "Clear failed.");
    } finally {
      setMaintenanceBusy(null);
    }
  };

  if (loading && !form) {
    return (
      <PageContainer>
        <PageHeader title="Settings" subtitle="Theme, thresholds, branding, and inbound sources." />
        <div className="flex flex-col gap-3">
          <Skeleton className="h-40" />
          <Skeleton className="h-40" />
        </div>
      </PageContainer>
    );
  }

  if (error || !form) {
    return (
      <PageContainer>
        <PageHeader title="Settings" />
        <ErrorBanner message={error ?? "Settings unavailable."} onRetry={refresh} />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <PageHeader
        title="Settings"
        subtitle="Theme, thresholds, branding, and inbound sources — applied across the console."
        actions={
          <div className="flex items-center gap-2">
            {saved && <Badge tone="success">Saved</Badge>}
            <Button variant="primary" size="sm" onClick={save} disabled={saving}>
              {saving ? "Saving…" : "Save changes"}
            </Button>
          </div>
        }
      />

      {saveError && <ErrorBanner message={saveError} />}

      <div className="flex flex-col gap-5">
        <SectionCard title="Branding & appearance">
          <div className="grid gap-4 p-4 sm:grid-cols-2">
            <Field label="Product name" hint="Shown in the navigation and document titles.">
              <input
                value={form.productName}
                onChange={(event) => update("productName", event.target.value)}
                className="rounded-md border border-input bg-surface px-3 py-2 text-sm"
              />
            </Field>
            <Field label="Accent color" hint="Recolors primary surfaces live across light and dark.">
              <div className="flex items-center gap-2">
                <input
                  type="color"
                  value={form.accentColor || DEFAULT_ACCENT}
                  onChange={(event) => update("accentColor", event.target.value)}
                  className="h-9 w-12 cursor-pointer rounded-md border border-input bg-surface"
                  aria-label="Accent color"
                />
                <input
                  value={form.accentColor}
                  placeholder={DEFAULT_ACCENT}
                  onChange={(event) => update("accentColor", event.target.value)}
                  className="flex-1 rounded-md border border-input bg-surface px-3 py-2 font-mono text-sm tabular"
                />
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => update("accentColor", "")}
                  aria-label="Reset accent"
                >
                  Reset
                </Button>
              </div>
            </Field>
            <Field label="Default theme" hint="Initial color mode for new sessions.">
              <select
                value={form.theme}
                onChange={(event) => update("theme", event.target.value as AppSettings["theme"])}
                className="rounded-md border border-input bg-surface px-3 py-2 text-sm"
              >
                <option value="Light">Light</option>
                <option value="Dark">Dark</option>
                <option value="System">System</option>
              </select>
            </Field>
          </div>
        </SectionCard>

        <SectionCard title="Confidence thresholds">
          <div className="grid gap-4 p-4 sm:grid-cols-2">
            <Field label="Low ceiling" hint="Scores below this render as Low confidence.">
              <input
                type="number"
                min={0}
                max={1}
                step={0.05}
                value={form.confidenceLowMax}
                onChange={(event) => update("confidenceLowMax", Number(event.target.value))}
                className="rounded-md border border-input bg-surface px-3 py-2 font-mono text-sm tabular"
              />
            </Field>
            <Field label="Medium ceiling" hint="Below this is Medium; at or above is High.">
              <input
                type="number"
                min={0}
                max={1}
                step={0.05}
                value={form.confidenceMediumMax}
                onChange={(event) => update("confidenceMediumMax", Number(event.target.value))}
                className="rounded-md border border-input bg-surface px-3 py-2 font-mono text-sm tabular"
              />
            </Field>
          </div>
        </SectionCard>

        <SectionCard title="Extraction & reconciliation">
          <div className="grid gap-4 p-4 sm:grid-cols-2">
            <Field
              label="Reconciliation tolerance"
              hint="Money totals within this percentage of the line-item total count as reconciled. Applies to new ingests."
            >
              <div className="flex items-center gap-2">
                <input
                  type="number"
                  min={0}
                  max={50}
                  step={0.25}
                  value={Number((form.reconciliationTolerance * 100).toFixed(2))}
                  onChange={(event) => update("reconciliationTolerance", Math.max(0, Number(event.target.value)) / 100)}
                  className="w-28 rounded-md border border-input bg-surface px-3 py-2 font-mono text-sm tabular"
                />
                <span className="text-sm text-muted-foreground">%</span>
              </div>
            </Field>
            <Field
              label="Local LLM assist"
              hint="When on and a local model is configured, new ingests use it to assist extraction. Off keeps extraction fully deterministic."
            >
              <button
                type="button"
                role="switch"
                aria-checked={form.useLlmAssist}
                onClick={() => update("useLlmAssist", !form.useLlmAssist)}
                className={cn(
                  "relative inline-flex h-6 w-11 shrink-0 items-center rounded-full border border-border transition-colors",
                  form.useLlmAssist ? "bg-primary" : "bg-surface-3",
                )}
              >
                <span
                  className={cn(
                    "inline-block size-5 rounded-full bg-surface shadow-soft transition-transform",
                    form.useLlmAssist ? "translate-x-5" : "translate-x-0.5",
                  )}
                />
              </button>
            </Field>
          </div>
        </SectionCard>

        <SectionCard title="Export defaults">
          <div className="grid gap-4 p-4 sm:grid-cols-2">
            <Field label="Default export template" hint="Pre-selected when exporting a reviewed document.">
              <select
                value={form.defaultTemplateId ?? ""}
                onChange={(event) => update("defaultTemplateId", event.target.value || null)}
                className="rounded-md border border-input bg-surface px-3 py-2 text-sm"
              >
                <option value="">Automatic (built-in CSV)</option>
                {(templates.data ?? []).map((template) => (
                  <option key={template.id} value={template.id}>
                    {template.name} · {template.format}
                  </option>
                ))}
              </select>
            </Field>
          </div>
        </SectionCard>

        <SectionCard title="Data management">
          <div className="flex flex-col gap-3 p-4">
            <p className="text-sm text-muted-foreground">
              Reload the bundled demo corpus into an empty workspace, or remove every document to start clean.
            </p>
            <div className="flex flex-wrap items-center gap-2">
              <Button variant="outline" size="sm" onClick={reseed} disabled={maintenanceBusy !== null}>
                {maintenanceBusy === "reseed" ? "Reseeding…" : "Reseed demo data"}
              </Button>
              <Button variant="danger" size="sm" onClick={clear} disabled={maintenanceBusy !== null}>
                {maintenanceBusy === "clear" ? "Clearing…" : "Clear all documents"}
              </Button>
              {maintenanceResult && (
                <span className="text-xs text-muted-foreground">{maintenanceResult}</span>
              )}
            </div>
          </div>
        </SectionCard>

        <SectionCard title="Inbound sources" meta={<span>Read-only</span>}>
          {sources.error && <div className="p-3"><ErrorBanner message={`Could not load inbound sources: ${sources.error}`} onRetry={sources.refresh} /></div>}
          <ul role="list" className="p-2">
            {(sources.data ?? []).map((source) => (
              <li key={source.name} className="flex items-center gap-2.5 rounded-md px-2 py-2">
                <Dot tone={source.enabled ? "success" : "neutral"} />
                <span className="text-sm font-medium capitalize">{source.name}</span>
                <span className="ml-auto text-[11px] text-muted-foreground">{source.detail}</span>
              </li>
            ))}
            {(sources.data ?? []).length === 0 && (
              <li className="px-2 py-3 text-xs text-muted-foreground">No inbound channels reported.</li>
            )}
          </ul>
        </SectionCard>
      </div>
    </PageContainer>
  );
}
