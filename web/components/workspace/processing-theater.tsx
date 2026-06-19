"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api/client";
import { cn } from "@/lib/cn";
import { Button, Spinner } from "@/components/ui/primitives";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogBody,
  DialogFooter,
} from "@/components/ui/dialog";
import {
  IconDocument,
  IconSearch,
  IconSpark,
  IconMappings,
  IconScale,
  IconCheck,
  IconAlert,
} from "@/components/ui/icons";
import type { ProcessingEvent, ProcessingStage } from "@/lib/api/types";

type StageStatus = "idle" | "active" | "done";
type StreamState = "connecting" | "streaming" | "done" | "error";

interface ScanLine {
  key: number;
  page: number;
  text: string;
}

interface FoundField {
  field: string;
  value: string;
  confidence: number;
  page: number | null;
}

interface ReconcileRow {
  field: string;
  detected: string;
  expected: string;
  agreement: number;
}

const STAGES: { key: ProcessingStage; label: string; Icon: typeof IconDocument }[] = [
  { key: "parsing", label: "Parse", Icon: IconDocument },
  { key: "ocr", label: "Scan", Icon: IconSearch },
  { key: "extracting", label: "Extract", Icon: IconSpark },
  { key: "mapping", label: "Map", Icon: IconMappings },
  { key: "reconciling", label: "Reconcile", Icon: IconScale },
];

const STAGE_ORDER: ProcessingStage[] = STAGES.map((stage) => stage.key);

function confidenceTone(confidence: number): string {
  if (confidence >= 0.75) return "text-accent";
  if (confidence >= 0.4) return "text-warning";
  return "text-danger";
}

function agreementTone(agreement: number): string {
  if (agreement >= 0.985) return "text-accent";
  if (agreement >= 0.9) return "text-warning";
  return "text-danger";
}

export function ProcessingTheater({
  documentId,
  fileName,
  open,
  onOpenChange,
  onComplete,
}: {
  documentId: string | null;
  fileName: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onComplete?: () => void;
}) {
  const router = useRouter();
  const [state, setState] = useState<StreamState>("connecting");
  const [error, setError] = useState<string | null>(null);
  const [stages, setStages] = useState<Record<ProcessingStage, StageStatus>>(() => ({
    parsing: "idle",
    ocr: "idle",
    extracting: "idle",
    mapping: "idle",
    reconciling: "idle",
  }));
  const [lines, setLines] = useState<ScanLine[]>([]);
  const [fields, setFields] = useState<FoundField[]>([]);
  const [reconciles, setReconciles] = useState<ReconcileRow[]>([]);
  const feedRef = useRef<HTMLDivElement>(null);
  const lineSeq = useRef(0);

  useEffect(() => {
    if (!open || !documentId) {
      return;
    }
    setState("connecting");
    setError(null);
    setStages({ parsing: "idle", ocr: "idle", extracting: "idle", mapping: "idle", reconciling: "idle" });
    setLines([]);
    setFields([]);
    setReconciles([]);
    lineSeq.current = 0;

    const source = new EventSource(api.processStreamUrl(documentId));
    let closed = false;
    const shutdown = () => {
      if (!closed) {
        closed = true;
        source.close();
      }
    };

    source.onopen = () => setState("streaming");
    source.onmessage = (message) => {
      if (message.data === "[DONE]") {
        shutdown();
        setState((prev) => (prev === "error" ? prev : "done"));
        onComplete?.();
        return;
      }
      let event: ProcessingEvent;
      try {
        event = JSON.parse(message.data) as ProcessingEvent;
      } catch {
        return;
      }
      switch (event.type) {
        case "stage": {
          setStages((prev) => {
            const next = { ...prev };
            if (event.status === "start") {
              next[event.stage] = "active";
              for (const stage of STAGE_ORDER) {
                if (stage === event.stage) break;
                if (next[stage] !== "done") next[stage] = "done";
              }
            } else {
              next[event.stage] = "done";
            }
            return next;
          });
          break;
        }
        case "line": {
          const key = lineSeq.current++;
          setLines((prev) => {
            const next = [...prev, { key, page: event.page, text: event.text }];
            return next.length > 500 ? next.slice(next.length - 500) : next;
          });
          break;
        }
        case "field":
          setFields((prev) => [
            ...prev,
            { field: event.field, value: event.value, confidence: event.confidence, page: event.page },
          ]);
          break;
        case "reconcile":
          setReconciles((prev) => [
            ...prev,
            { field: event.field, detected: event.detected, expected: event.expected, agreement: event.agreement },
          ]);
          break;
        case "error":
          setError(event.message);
          setState("error");
          shutdown();
          break;
        case "done":
          break;
      }
    };
    source.onerror = () => {
      setState((prev) => {
        if (prev === "done") return prev;
        setError((existing) => existing ?? "Lost connection to the processing stream.");
        return "error";
      });
      shutdown();
    };

    return shutdown;
  }, [open, documentId, onComplete]);

  useEffect(() => {
    const feed = feedRef.current;
    if (feed) {
      feed.scrollTop = feed.scrollHeight;
    }
  }, [lines]);

  const activeStageLabel = useMemo(() => {
    const active = STAGES.find((stage) => stages[stage.key] === "active");
    if (active) return active.label;
    return state === "done" ? "Complete" : "Working";
  }, [stages, state]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent size="lg" className="max-w-4xl">
        <DialogHeader
          title={
            <span className="flex items-center gap-2">
              <span
                className={cn(
                  "inline-flex size-2 rounded-full",
                  state === "done" ? "bg-accent" : state === "error" ? "bg-danger" : "bg-warning animate-pulse",
                )}
                aria-hidden="true"
              />
              <span className="truncate">Processing {fileName}</span>
            </span>
          }
          description={
            state === "error"
              ? (error ?? "Processing failed")
              : state === "done"
                ? "Extraction complete — every value is cited to its source."
                : `Reading the document live — ${activeStageLabel.toLowerCase()}…`
          }
        />

        <div className="border-b border-border bg-surface-2/30 px-5 py-3">
          <ol className="flex items-center justify-between gap-1">
            {STAGES.map((stage, index) => {
              const status = stages[stage.key];
              return (
                <li key={stage.key} className="flex flex-1 items-center gap-1 last:flex-none">
                  <div className="flex items-center gap-2">
                    <span
                      className={cn(
                        "grid size-7 shrink-0 place-items-center rounded-md border transition-colors duration-300",
                        status === "done"
                          ? "border-accent-border bg-accent-soft text-accent"
                          : status === "active"
                            ? "border-primary-border bg-primary-soft text-primary"
                            : "border-border bg-surface-2 text-subtle-foreground",
                      )}
                    >
                      {status === "done" ? (
                        <IconCheck width={14} height={14} />
                      ) : status === "active" ? (
                        <Spinner className="size-3.5" />
                      ) : (
                        <stage.Icon width={14} height={14} />
                      )}
                    </span>
                    <span
                      className={cn(
                        "text-xs font-medium transition-colors",
                        status === "idle" ? "text-subtle-foreground" : "text-foreground",
                      )}
                    >
                      {stage.label}
                    </span>
                  </div>
                  {index < STAGES.length - 1 && (
                    <span
                      className={cn(
                        "mx-1 hidden h-px flex-1 transition-colors duration-300 sm:block",
                        status === "done" ? "bg-accent-border" : "bg-border",
                      )}
                      aria-hidden="true"
                    />
                  )}
                </li>
              );
            })}
          </ol>
        </div>

        <DialogBody className="grid grid-cols-1 gap-0 p-0 lg:grid-cols-5">
          <div className="flex min-h-[18rem] flex-col border-b border-border lg:col-span-3 lg:border-b-0 lg:border-r">
            <div className="flex items-center justify-between px-4 py-2 text-[11px] uppercase tracking-[0.08em] text-subtle-foreground">
              <span>Live scan</span>
              <span className="font-mono tabular">{lines.length} lines</span>
            </div>
            <div
              ref={feedRef}
              className="min-h-0 flex-1 overflow-y-auto bg-surface-2/20 px-4 py-2 font-mono text-[12px] leading-relaxed"
            >
              {lines.length === 0 && state !== "done" && state !== "error" && (
                <p className="text-subtle-foreground">Waiting for the first scanned line…</p>
              )}
              {lines.map((line) => (
                <div key={line.key} className="flex animate-rise gap-2.5 py-0.5">
                  <span className="shrink-0 select-none text-subtle-foreground">p{line.page}</span>
                  <span className="min-w-0 break-words text-foreground/90">{line.text}</span>
                </div>
              ))}
            </div>
          </div>

          <div className="flex min-h-0 flex-col lg:col-span-2">
            <div className="flex items-center justify-between px-4 py-2 text-[11px] uppercase tracking-[0.08em] text-subtle-foreground">
              <span>Extracted fields</span>
              <span className="font-mono tabular">{fields.length}</span>
            </div>
            <div className="min-h-0 flex-1 space-y-1.5 overflow-y-auto px-4 pb-3">
              {fields.length === 0 && (
                <p className="text-xs text-subtle-foreground">Fields appear as the model locates them.</p>
              )}
              {fields.map((field, index) => (
                <div
                  key={`${field.field}-${index}`}
                  className="flex animate-rise items-center justify-between gap-2 rounded-md border border-border bg-surface px-2.5 py-1.5"
                >
                  <div className="flex min-w-0 flex-col">
                    <span className="text-[10px] uppercase tracking-[0.05em] text-subtle-foreground">{field.field}</span>
                    <span className="truncate text-xs font-medium text-foreground">{field.value}</span>
                  </div>
                  <span className={cn("shrink-0 font-mono text-[11px] tabular", confidenceTone(field.confidence))}>
                    {Math.round(field.confidence * 100)}%
                  </span>
                </div>
              ))}

              {reconciles.length > 0 && (
                <div className="pt-2">
                  <p className="px-0.5 pb-1 text-[10px] uppercase tracking-[0.06em] text-subtle-foreground">
                    Reconciliation
                  </p>
                  {reconciles.map((row, index) => (
                    <div
                      key={`${row.field}-${index}`}
                      className="flex animate-rise items-center justify-between gap-2 rounded-md border border-border bg-surface px-2.5 py-1.5"
                    >
                      <div className="flex min-w-0 flex-col">
                        <span className="text-[10px] uppercase tracking-[0.05em] text-subtle-foreground">{row.field}</span>
                        <span className="truncate font-mono text-[11px] text-foreground/80">
                          {row.detected} vs {row.expected}
                        </span>
                      </div>
                      <span className={cn("flex shrink-0 items-center gap-1 font-mono text-[11px] tabular", agreementTone(row.agreement))}>
                        {row.agreement >= 0.985 ? <IconCheck width={12} height={12} /> : <IconAlert width={12} height={12} />}
                        {Math.round(row.agreement * 100)}%
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </DialogBody>

        <DialogFooter>
          {state === "connecting" || state === "streaming" ? (
            <span className="mr-auto flex items-center gap-2 text-xs text-muted-foreground">
              <Spinner className="size-3.5" /> Streaming from the API…
            </span>
          ) : state === "error" ? (
            <span className="mr-auto text-xs text-danger">{error}</span>
          ) : (
            <span className="mr-auto flex items-center gap-1.5 text-xs text-accent">
              <IconCheck width={14} height={14} /> Done — {fields.length} fields, {reconciles.length} reconciliations
            </span>
          )}
          <Button variant="outline" size="sm" onClick={() => onOpenChange(false)}>
            Close
          </Button>
          <Button
            size="sm"
            disabled={!documentId}
            onClick={() => {
              onOpenChange(false);
              if (documentId) {
                router.push(`/review?doc=${documentId}`);
              }
            }}
          >
            Open in review
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
