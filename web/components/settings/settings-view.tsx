"use client";

import { useState } from "react";
import { cn } from "@/lib/cn";
import { api } from "@/lib/api/client";
import type { AiProvider, AppSettings, ModelOption } from "@/lib/api/types";
import { useApi } from "@/lib/use-api";
import { PageContainer, PageHeader, SectionCard } from "@/components/ui/page";
import { ErrorBanner, Skeleton } from "@/components/ui/states";
import { Badge, Button, Dot, Spinner } from "@/components/ui/primitives";
import { Field, Input, Select } from "@/components/ui/form";
import { Switch } from "@/components/ui/switch";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { IconCloud, IconCpu } from "@/components/ui/icons";

interface ProviderDoc {
  label: string;
  url: string;
}

const PROVIDERS: Record<
  AiProvider,
  { label: string; baseUrl: string; cloud: boolean; keyHint: string; hint: string; docsLabel: string; docs: ProviderDoc[] }
> = {
  Ollama: {
    label: "Ollama — local",
    baseUrl: "http://localhost:11434/v1",
    cloud: false,
    keyHint: "Not required for local Ollama.",
    hint: "Local Ollama server. Models are listed automatically; runs fully offline, no API key.",
    docsLabel: "Set up Ollama",
    docs: [
      { label: "Install Ollama", url: "https://ollama.com/download" },
      { label: "Browse models", url: "https://ollama.com/library" },
      { label: "Quickstart", url: "https://github.com/ollama/ollama/blob/main/README.md#quickstart" },
    ],
  },
  OpenAiCompatible: {
    label: "OpenAI-compatible — llama.cpp · LM Studio · Unsloth · vLLM",
    baseUrl: "http://localhost:8080/v1",
    cloud: false,
    keyHint: "Optional — only if your server enforces one.",
    hint: "Any OpenAI /v1 endpoint, local or self-hosted. Point the base URL at your server (e.g. LM Studio :1234, llama.cpp :8080, vLLM :8000).",
    docsLabel: "Install a local server",
    docs: [
      { label: "LM Studio", url: "https://lmstudio.ai/docs/app/api" },
      { label: "llama.cpp server", url: "https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md" },
      { label: "Unsloth (OpenAI endpoint)", url: "https://unsloth.ai/docs/basics/inference-and-deployment/llama-server-and-openai-endpoint" },
      { label: "vLLM (OpenAI server)", url: "https://docs.vllm.ai/en/latest/serving/openai_compatible_server.html" },
    ],
  },
  HuggingFace: {
    label: "Hugging Face — cloud inference",
    baseUrl: "https://router.huggingface.co/v1",
    cloud: true,
    keyHint: "Required — paste your Hugging Face access token (hf_…).",
    hint: "Online inference via the Hugging Face router. Paste a token, then discover models or type a model id.",
    docsLabel: "Get set up on Hugging Face",
    docs: [
      { label: "Get your access token", url: "https://huggingface.co/settings/tokens" },
      { label: "Browse served models", url: "https://huggingface.co/models?inference_provider=all&sort=trending" },
      { label: "Inference Providers docs", url: "https://huggingface.co/docs/inference-providers/index" },
    ],
  },
};

const DEFAULT_ACCENT = "#3b5bd6";
const ACCENT_PRESETS = ["#0070F3", "#7C3AED", "#16A34A", "#DC2626", "#EA580C", "#0891B2"];
const HEX_PATTERN = /^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/;

const applyAccent = (hex: string) => {
  const root = document.documentElement;
  if (hex) {
    root.style.setProperty("--accent", hex);
  } else {
    root.style.removeProperty("--accent");
  }
};

const clamp01 = (value: number): number => Math.max(0, Math.min(1, value));

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
  const [confirmAction, setConfirmAction] = useState<"reseed" | "clear" | null>(null);
  const [models, setModels] = useState<ModelOption[]>([]);
  const [discovering, setDiscovering] = useState(false);
  const [discoverMsg, setDiscoverMsg] = useState<string | null>(null);

  if (data && data !== seed) {
    setSeed(data);
    setForm(data);
  }

  const accentValid = !form?.accentColor || HEX_PATTERN.test(form.accentColor);
  const thresholdsValid =
    !form ||
    (form.confidenceLowMax >= 0 &&
      form.confidenceMediumMax <= 1 &&
      form.confidenceLowMax < form.confidenceMediumMax);
  const toleranceValid =
    !form || (form.reconciliationTolerance >= 0 && form.reconciliationTolerance <= 0.5);
  const canSave = accentValid && thresholdsValid && toleranceValid;
  const dirty = form !== null && seed !== null && JSON.stringify(form) !== JSON.stringify(seed);

  const update = <K extends keyof AppSettings>(key: K, value: AppSettings[K]) => {
    setForm((current) => (current ? { ...current, [key]: value } : current));
    setSaved(false);
    if (key === "accentColor") {
      const hex = value as string;
      if (!hex || HEX_PATTERN.test(hex)) {
        applyAccent(hex);
      }
    }
  };

  const changeProvider = (provider: AiProvider) => {
    setModels([]);
    setDiscoverMsg(null);
    setSaved(false);
    setForm((current) =>
      current ? { ...current, aiProvider: provider, aiBaseUrl: PROVIDERS[provider].baseUrl } : current,
    );
  };

  const discover = async () => {
    if (!form) {
      return;
    }
    setDiscovering(true);
    setDiscoverMsg(null);
    try {
      const result = await api.discoverModels({
        provider: form.aiProvider,
        baseUrl: form.aiBaseUrl,
        apiKey: form.aiApiKey,
      });
      setModels(result.models);
      setDiscoverMsg(
        result.models.length > 0
          ? `${result.models.length} model${result.models.length === 1 ? "" : "s"} available · ${result.source}`
          : result.message ?? "No models reported by this endpoint.",
      );
    } catch (cause) {
      setModels([]);
      setDiscoverMsg(cause instanceof Error ? cause.message : "Could not reach the endpoint.");
    } finally {
      setDiscovering(false);
    }
  };

  const save = async () => {
    if (!form || !canSave) {
      return;
    }
    setSaving(true);
    setSaveError(null);
    try {
      const result = await api.saveSettings(form);
      setSeed(result);
      setForm(result);
      setSaved(true);
      window.dispatchEvent(new CustomEvent("reva:settings-updated"));
    } catch (cause) {
      setSaveError(cause instanceof Error ? cause.message : "Save failed");
    } finally {
      setSaving(false);
    }
  };

  const reseed = async () => {
    setConfirmAction(null);
    setMaintenanceBusy("reseed");
    setMaintenanceResult(null);
    try {
      const { seeded } = await api.reseedDemo();
      setMaintenanceResult(
        seeded ? "Demo corpus reseeded." : "Workspace already has documents — clear it first to reseed.",
      );
    } catch (cause) {
      setMaintenanceResult(cause instanceof Error ? cause.message : "Reseed failed.");
    } finally {
      setMaintenanceBusy(null);
    }
  };

  const clear = async () => {
    setConfirmAction(null);
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
    <PageContainer className="max-w-[1040px]">
      <PageHeader
        title="Settings"
        subtitle="Theme, thresholds, branding, and inbound sources — applied across the console."
        actions={
          <div className="flex items-center gap-2">
            {saved && !dirty && <Badge tone="success">Saved</Badge>}
            {dirty && <span className="text-xs text-subtle-foreground">Unsaved changes</span>}
            <Button variant="primary" size="sm" onClick={save} disabled={saving || !canSave || !dirty}>
              {saving ? "Saving…" : "Save changes"}
            </Button>
          </div>
        }
      />

      {saveError && (
        <div className="pb-4">
          <ErrorBanner message={saveError} />
        </div>
      )}

      <div className="flex flex-col gap-5">
        <SectionCard title="Branding & appearance">
          <div className="grid gap-5 p-4 sm:grid-cols-2">
            <Field
              label="Product name"
              htmlFor="settings-product-name"
              hint="Shown in the navigation and document titles."
            >
              <Input
                id="settings-product-name"
                value={form.productName}
                onChange={(event) => update("productName", event.target.value)}
              />
            </Field>
            <Field label="Default theme" htmlFor="settings-theme" hint="Initial color mode for new sessions.">
              <Select
                id="settings-theme"
                value={form.theme}
                onChange={(event) => update("theme", event.target.value as AppSettings["theme"])}
              >
                <option value="Light">Light</option>
                <option value="Dark">Dark</option>
                <option value="System">System</option>
              </Select>
            </Field>
            <Field
              label="Accent color"
              htmlFor="settings-accent"
              className="sm:col-span-2"
              hint={
                accentValid
                  ? "Recolors primary surfaces live across light and dark."
                  : "Enter a valid hex color such as #0070F3."
              }
            >
              <div className="flex flex-wrap items-center gap-2">
                <input
                  type="color"
                  value={HEX_PATTERN.test(form.accentColor) ? form.accentColor : DEFAULT_ACCENT}
                  onChange={(event) => update("accentColor", event.target.value)}
                  className="h-9 w-12 shrink-0 cursor-pointer rounded-md border border-input bg-surface"
                  aria-label="Accent color picker"
                />
                <Input
                  id="settings-accent"
                  value={form.accentColor}
                  placeholder={DEFAULT_ACCENT}
                  onChange={(event) => update("accentColor", event.target.value)}
                  aria-invalid={!accentValid}
                  className="w-40 font-mono tabular"
                />
                <div className="flex items-center gap-1.5" role="group" aria-label="Accent presets">
                  {ACCENT_PRESETS.map((preset) => (
                    <button
                      key={preset}
                      type="button"
                      aria-label={`Use accent ${preset}`}
                      onClick={() => update("accentColor", preset)}
                      style={{ backgroundColor: preset }}
                      className="size-6 rounded-full border border-border-strong transition-transform hover:scale-110 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50"
                    />
                  ))}
                </div>
                <Button variant="ghost" size="sm" onClick={() => update("accentColor", "")} aria-label="Reset accent">
                  Reset
                </Button>
              </div>
            </Field>
          </div>
        </SectionCard>

        <SectionCard
          title="AI model & provider"
          meta={
            <span className="inline-flex items-center gap-1.5">
              {PROVIDERS[form.aiProvider ?? "Ollama"].cloud ? (
                <IconCloud width={13} height={13} />
              ) : (
                <IconCpu width={13} height={13} />
              )}
              {PROVIDERS[form.aiProvider ?? "Ollama"].cloud ? "Cloud" : "Local"}
            </span>
          }
        >
          <div className="grid gap-5 p-4 sm:grid-cols-2">
            <Field
              label="Provider"
              htmlFor="settings-ai-provider"
              className="sm:col-span-2"
              hint={PROVIDERS[form.aiProvider ?? "Ollama"].hint}
            >
              <div className="flex flex-col gap-2">
                <Select
                  id="settings-ai-provider"
                  value={form.aiProvider ?? "Ollama"}
                  onChange={(event) => changeProvider(event.target.value as AiProvider)}
                >
                  {(Object.keys(PROVIDERS) as AiProvider[]).map((key) => (
                    <option key={key} value={key}>
                      {PROVIDERS[key].label}
                    </option>
                  ))}
                </Select>
                <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-[11px]">
                  <span className="font-medium text-subtle-foreground">
                    {PROVIDERS[form.aiProvider ?? "Ollama"].docsLabel}:
                  </span>
                  {PROVIDERS[form.aiProvider ?? "Ollama"].docs.map((doc) => (
                    <a
                      key={doc.url}
                      href={doc.url}
                      target="_blank"
                      rel="noreferrer"
                      className="font-medium text-primary transition-colors hover:underline"
                    >
                      {doc.label} ↗
                    </a>
                  ))}
                </div>
              </div>
            </Field>

            <Field label="Base URL" htmlFor="settings-ai-baseurl" hint="The OpenAI-compatible endpoint Reva calls.">
              <Input
                id="settings-ai-baseurl"
                value={form.aiBaseUrl ?? ""}
                placeholder={PROVIDERS[form.aiProvider ?? "Ollama"].baseUrl}
                onChange={(event) => update("aiBaseUrl", event.target.value)}
                className="font-mono text-xs"
              />
            </Field>

            <Field
              label="API key / token"
              htmlFor="settings-ai-key"
              hint={PROVIDERS[form.aiProvider ?? "Ollama"].keyHint}
            >
              <Input
                id="settings-ai-key"
                type="password"
                value={form.aiApiKey ?? ""}
                placeholder={PROVIDERS[form.aiProvider ?? "Ollama"].cloud ? "hf_…" : "optional"}
                autoComplete="off"
                onChange={(event) => update("aiApiKey", event.target.value || null)}
                className="font-mono text-xs"
              />
            </Field>

            <Field
              label="Model"
              htmlFor="settings-ai-model"
              className="sm:col-span-2"
              hint="Pick a discovered model, or type any model id your endpoint serves."
            >
              <div className="flex flex-col gap-2">
                <div className="flex flex-wrap items-center gap-2">
                  <Input
                    id="settings-ai-model"
                    value={form.aiModel ?? ""}
                    placeholder="e.g. qwen2.5vl:7b"
                    onChange={(event) => update("aiModel", event.target.value)}
                    className="w-full max-w-xs font-mono text-xs"
                  />
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={discover}
                    disabled={discovering || !form.aiBaseUrl}
                  >
                    {discovering ? (
                      <span className="inline-flex items-center gap-1.5">
                        <Spinner className="size-3" /> Discovering…
                      </span>
                    ) : (
                      "Discover models"
                    )}
                  </Button>
                  {discoverMsg && <span className="text-xs text-muted-foreground">{discoverMsg}</span>}
                </div>
                {models.length > 0 && (
                  <div className="flex flex-wrap gap-1.5" role="group" aria-label="Discovered models">
                    {models.map((model) => {
                      const active = model.id === form.aiModel;
                      return (
                        <button
                          key={model.id}
                          type="button"
                          onClick={() => update("aiModel", model.id)}
                          className={cn(
                            "rounded-md border px-2 py-1 font-mono text-[11px] transition-colors",
                            active
                              ? "border-primary-border bg-primary-soft text-foreground"
                              : "border-border bg-surface-2/60 text-muted-foreground hover:border-border-strong hover:text-foreground",
                          )}
                        >
                          {model.label}
                        </button>
                      );
                    })}
                  </div>
                )}
              </div>
            </Field>
          </div>
        </SectionCard>

        <SectionCard title="Confidence thresholds">
          <div className="grid gap-5 p-4 sm:grid-cols-2">
            <Field
              label="Low ceiling"
              htmlFor="settings-low"
              hint="Scores below this render as Low confidence."
            >
              <Input
                id="settings-low"
                type="number"
                min={0}
                max={1}
                step={0.05}
                value={form.confidenceLowMax}
                aria-invalid={!thresholdsValid}
                onChange={(event) => update("confidenceLowMax", clamp01(Number(event.target.value)))}
                className="font-mono tabular"
              />
            </Field>
            <Field
              label="Medium ceiling"
              htmlFor="settings-medium"
              hint={
                thresholdsValid
                  ? "Below this is Medium; at or above is High."
                  : "Low ceiling must be below the medium ceiling, both within 0–1."
              }
            >
              <Input
                id="settings-medium"
                type="number"
                min={0}
                max={1}
                step={0.05}
                value={form.confidenceMediumMax}
                aria-invalid={!thresholdsValid}
                onChange={(event) => update("confidenceMediumMax", clamp01(Number(event.target.value)))}
                className="font-mono tabular"
              />
            </Field>
          </div>
        </SectionCard>

        <SectionCard title="Extraction & reconciliation">
          <div className="grid gap-5 p-4 sm:grid-cols-2">
            <Field
              label="Reconciliation tolerance"
              htmlFor="settings-tolerance"
              hint={
                toleranceValid
                  ? "Money totals within this percentage of the line-item total count as reconciled. Applies to new ingests."
                  : "Tolerance must be between 0% and 50%."
              }
            >
              <div className="flex items-center gap-2">
                <Input
                  id="settings-tolerance"
                  type="number"
                  min={0}
                  max={50}
                  step={0.25}
                  value={Number((form.reconciliationTolerance * 100).toFixed(2))}
                  aria-invalid={!toleranceValid}
                  onChange={(event) =>
                    update("reconciliationTolerance", Math.max(0, Number(event.target.value)) / 100)
                  }
                  className="w-28 font-mono tabular"
                />
                <span className="text-sm text-muted-foreground">%</span>
              </div>
            </Field>
            <Field
              label="Local LLM assist"
              hint="When on and a local model is configured, new ingests use it to assist extraction. Off keeps extraction fully deterministic."
            >
              <div className="flex items-center gap-3 pt-1">
                <Switch
                  checked={form.useLlmAssist}
                  onCheckedChange={(checked) => update("useLlmAssist", checked)}
                  aria-label="Local LLM assist"
                />
                <span className="text-sm text-muted-foreground">{form.useLlmAssist ? "Enabled" : "Disabled"}</span>
              </div>
            </Field>
          </div>
        </SectionCard>

        <SectionCard title="Export defaults">
          <div className="grid gap-5 p-4 sm:grid-cols-2">
            <Field
              label="Default export template"
              htmlFor="settings-default-template"
              hint="Pre-selected when exporting a reviewed document."
            >
              <Select
                id="settings-default-template"
                value={form.defaultTemplateId ?? ""}
                onChange={(event) => update("defaultTemplateId", event.target.value || null)}
              >
                <option value="">Automatic (built-in CSV)</option>
                {(templates.data ?? []).map((template) => (
                  <option key={template.id} value={template.id}>
                    {template.name} · {template.format}
                  </option>
                ))}
              </Select>
            </Field>
          </div>
        </SectionCard>

        <SectionCard title="Data management">
          <div className="flex flex-col gap-3 p-4">
            <p className="text-sm text-muted-foreground">
              Reload the bundled demo corpus into an empty workspace, or remove every document to start clean.
            </p>
            <div className="flex flex-wrap items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setConfirmAction("reseed")}
                disabled={maintenanceBusy !== null}
              >
                {maintenanceBusy === "reseed" ? "Reseeding…" : "Reseed demo data"}
              </Button>
              <Button
                variant="danger"
                size="sm"
                onClick={() => setConfirmAction("clear")}
                disabled={maintenanceBusy !== null}
              >
                {maintenanceBusy === "clear" ? "Clearing…" : "Clear all documents"}
              </Button>
              {maintenanceResult && <span className="text-xs text-muted-foreground">{maintenanceResult}</span>}
            </div>
          </div>
        </SectionCard>

        <SectionCard title="Inbound sources" meta={<span>Read-only</span>}>
          {sources.error && (
            <div className="p-3">
              <ErrorBanner message={`Could not load inbound sources: ${sources.error}`} onRetry={sources.refresh} />
            </div>
          )}
          <ul role="list" className="p-2">
            {(sources.data ?? []).map((source) => (
              <li
                key={source.name}
                className="flex items-center gap-2.5 rounded-md px-2.5 py-2 transition-colors hover:bg-surface-2/50"
              >
                <Dot tone={source.enabled ? "success" : "neutral"} className={source.enabled ? "animate-pulse-dot" : undefined} />
                <span className="text-sm font-medium capitalize tracking-tight">{source.name}</span>
                <span className="ml-auto font-mono text-[11px] tabular text-subtle-foreground">{source.detail}</span>
              </li>
            ))}
            {(sources.data ?? []).length === 0 && (
              <li className="px-2 py-3 text-xs text-muted-foreground">No inbound channels reported.</li>
            )}
          </ul>
        </SectionCard>
      </div>

      <ConfirmDialog
        open={confirmAction === "reseed"}
        onOpenChange={(open) => !open && setConfirmAction(null)}
        title="Reseed demo data"
        description="Loads the bundled demo corpus. This only runs when the workspace is empty."
        confirmLabel="Reseed"
        onConfirm={reseed}
      />
      <ConfirmDialog
        open={confirmAction === "clear"}
        onOpenChange={(open) => !open && setConfirmAction(null)}
        title="Clear all documents"
        description="Delete every document, extraction, and review record. This cannot be undone."
        confirmLabel="Delete everything"
        destructive
        onConfirm={clear}
      />
    </PageContainer>
  );
}
