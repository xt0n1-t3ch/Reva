"use client";

import { useMemo, useState } from "react";
import type { UIMessage } from "ai";
import Image from "next/image";
import { cn } from "@/lib/cn";
import { Markdown } from "@/lib/markdown";
import { Spinner } from "@/components/ui/primitives";
import {
  IconBrain,
  IconCheckSmall,
  IconChevron,
  IconNavigate,
  IconRead,
  IconSearch,
  IconTool,
  IconWrite,
} from "@/components/chat/chat-icons";
import { IconAlert } from "@/components/ui/icons";

type Part = UIMessage["parts"][number];
type AnyPart = Extract<Part, { type: string }> & Record<string, unknown>;

/**
 * Universal step KINDS, like a real agent harness activity log: every tool
 * resolves to one of these verbs + an icon, plus the specific target pulled
 * from the call's input. Unknown / MCP tools fall back to "Tool".
 */
type StepKind = "read" | "search" | "write" | "navigate" | "tool";

const KIND_BY_TOOL: Record<string, StepKind> = {
  list_documents: "read",
  get_document: "read",
  reconcile: "read",
  explain_field: "read",
  refresh_queue: "read",
  search_knowledge: "search",
  filter_queue: "search",
  correct_field: "write",
  set_review_state: "write",
  export_document: "write",
  reseed: "write",
  clear: "write",
  goto: "navigate",
  open_document: "navigate",
};

const KIND_LABEL: Record<StepKind, string> = {
  read: "Read",
  search: "Search",
  write: "Write",
  navigate: "Navigate",
  tool: "Tool",
};

const KIND_ICON: Record<StepKind, typeof IconRead> = {
  read: IconRead,
  search: IconSearch,
  write: IconWrite,
  navigate: IconNavigate,
  tool: IconTool,
};

const KIND_TINT: Record<StepKind, string> = {
  read: "text-primary",
  search: "text-primary",
  write: "text-warning",
  navigate: "text-success",
  tool: "text-muted-foreground",
};

const toolName = (type: string): string =>
  type.replace(/^tool-/, "").replace(/^dynamic-tool$/, "tool");

const isMcpTool = (name: string): boolean => name.includes("__") || name.startsWith("mcp");

const stepKind = (type: string): StepKind => KIND_BY_TOOL[toolName(type)] ?? "tool";

/** A readable detail for the tool, e.g. "document queue", "set review state". */
const stepDetail = (type: string): string => toolName(type).replace(/_/g, " ");

const kindLabel = (type: string): string => {
  const name = toolName(type);
  if (KIND_BY_TOOL[name]) {
    return KIND_LABEL[KIND_BY_TOOL[name]];
  }
  return isMcpTool(name) ? "MCP" : "Tool";
};

const shorten = (value: string, max = 36): string =>
  value.length > max ? `${value.slice(0, max - 1)}…` : value;

/**
 * Pull the human-meaningful target out of a tool call's input args so eight
 * identical reconcile steps read as eight distinct documents.
 */
const TARGET_KEYS = [
  "fileName",
  "file_name",
  "documentName",
  "document",
  "documentId",
  "document_id",
  "fieldName",
  "field",
  "name",
  "title",
  "query",
  "path",
  "route",
  "id",
];

const stepTarget = (input: unknown): string | null => {
  if (input == null) {
    return null;
  }
  if (typeof input === "string") {
    return input.trim() ? shorten(input.trim()) : null;
  }
  if (typeof input !== "object") {
    return null;
  }
  const record = input as Record<string, unknown>;
  for (const key of TARGET_KEYS) {
    const value = record[key];
    if (typeof value === "string" && value.trim()) {
      return shorten(value.trim());
    }
    if (typeof value === "number") {
      return String(value);
    }
  }
  const firstString = Object.values(record).find(
    (value): value is string => typeof value === "string" && value.trim().length > 0,
  );
  return firstString ? shorten(firstString.trim()) : null;
};

const outputPreview = (value: unknown): string | null => {
  if (value == null) {
    return null;
  }
  if (typeof value === "string") {
    return value.trim().slice(0, 120) || null;
  }
  if (typeof value === "object") {
    const record = value as Record<string, unknown>;
    const message = record.message ?? record.summary ?? record.title;
    if (typeof message === "string") {
      return message.slice(0, 120);
    }
    if (Array.isArray(record.data)) {
      return `${record.data.length} result${record.data.length === 1 ? "" : "s"}`;
    }
  }
  return null;
};

type ToolStep = {
  kind: StepKind;
  kindLabel: string;
  detail: string;
  target: string | null;
  state: "running" | "done" | "error";
  preview: string | null;
};

const toStep = (part: AnyPart): ToolStep => {
  const state = typeof part.state === "string" ? part.state : "input-available";
  const running = state === "input-streaming" || state === "input-available";
  const failed = state === "output-error";
  return {
    kind: stepKind(part.type),
    kindLabel: kindLabel(part.type),
    detail: stepDetail(part.type),
    target: stepTarget(part.input),
    state: failed ? "error" : running ? "running" : "done",
    preview: failed
      ? typeof part.errorText === "string"
        ? part.errorText
        : null
      : running
        ? null
        : outputPreview(part.output),
  };
};

function StepStatusIcon({ state }: { state: ToolStep["state"] }) {
  if (state === "running") {
    return <Spinner className="size-3 text-primary" />;
  }
  if (state === "error") {
    return (
      <span className="grid size-4 place-items-center rounded-full bg-danger-soft text-danger">
        <IconAlert width={10} height={10} />
      </span>
    );
  }
  return (
    <span className="grid size-4 place-items-center rounded-full bg-success-soft text-success">
      <IconCheckSmall width={11} height={11} />
    </span>
  );
}

const collectSteps = (message: UIMessage): ToolStep[] =>
  message.parts
    .filter(
      (part): part is AnyPart => part.type.startsWith("tool-") || part.type === "dynamic-tool",
    )
    .map(toStep);

/**
 * The agent activity log: an ordered, collapsible vertical timeline of tool
 * steps. Renders in the message ROW above the answer bubble, not inside it.
 */
export function MessageActivity({ message, streaming }: { message: UIMessage; streaming: boolean }) {
  const steps = useMemo(() => collectSteps(message), [message]);
  const hasRunning = steps.some((step) => step.state === "running");
  const failed = steps.filter((step) => step.state === "error").length;
  // Expanded while live so the user watches progress; collapses once settled.
  const [manualOpen, setManualOpen] = useState<boolean | null>(null);
  const open = manualOpen ?? hasRunning;

  if (steps.length === 0) {
    return null;
  }

  const summary = `${steps.length} step${steps.length === 1 ? "" : "s"}`;

  return (
    <div className="overflow-hidden rounded-md border border-border bg-surface-2/40">
      <button
        type="button"
        onClick={() => setManualOpen(!open)}
        aria-expanded={open}
        className="flex w-full items-center gap-2 px-2.5 py-1.5 text-left text-[11px] font-medium text-muted-foreground transition-colors hover:bg-surface-2/70"
      >
        {hasRunning ? (
          <Spinner className="size-3 shrink-0 text-primary" />
        ) : failed > 0 ? (
          <span className="size-1.5 shrink-0 rounded-full bg-danger" />
        ) : (
          <span className="size-1.5 shrink-0 rounded-full bg-success" />
        )}
        <span className="tabular">{summary}</span>
        {failed > 0 && <span className="text-danger">· {failed} failed</span>}
        {hasRunning && streaming && <span className="text-primary">· working…</span>}
        <IconChevron
          width={13}
          height={13}
          className={cn(
            "ml-auto shrink-0 transition-transform duration-200",
            open ? "rotate-0" : "-rotate-90",
          )}
        />
      </button>

      {open && (
        <ol className="relative space-y-0 px-2.5 pb-2 pt-0.5">
          {steps.map((step, index) => {
            const last = index === steps.length - 1;
            const KindIcon = KIND_ICON[step.kind];
            return (
              <li key={index} className="relative flex gap-2.5 pb-2 last:pb-0">
                {!last && (
                  <span
                    aria-hidden="true"
                    className="absolute left-2 top-5 h-[calc(100%-0.75rem)] w-px bg-border"
                  />
                )}
                <span className="relative z-10 mt-0.5 flex size-4 shrink-0 items-center justify-center">
                  <StepStatusIcon state={step.state} />
                </span>
                <div className="min-w-0 flex-1 leading-snug">
                  <p className="flex flex-wrap items-center gap-x-1.5 gap-y-0.5 text-[12px] text-foreground">
                    <span className="inline-flex items-center gap-1 font-medium">
                      <KindIcon width={12} height={12} className={cn("shrink-0", KIND_TINT[step.kind])} />
                      {step.kindLabel}
                    </span>
                    <span className="text-subtle-foreground">·</span>
                    <span className="text-muted-foreground">{step.detail}</span>
                    {step.target && (
                      <>
                        <span className="text-subtle-foreground">·</span>
                        <span className="font-mono text-[11px] text-muted-foreground">
                          {step.target}
                        </span>
                      </>
                    )}
                  </p>
                  {step.preview && (
                    <p
                      className={cn(
                        "mt-0.5 text-[11px] leading-snug",
                        step.state === "error" ? "text-danger" : "text-subtle-foreground",
                      )}
                    >
                      {step.preview}
                    </p>
                  )}
                </div>
              </li>
            );
          })}
        </ol>
      )}
    </div>
  );
}

const collectReasoning = (message: UIMessage): string =>
  message.parts
    .filter((part): part is Extract<Part, { type: "reasoning" }> => part.type === "reasoning")
    .map((part) => part.text)
    .join("\n")
    .trim();

const hasAnswerText = (message: UIMessage): boolean =>
  message.parts.some((part) => part.type === "text" && part.text.trim().length > 0);

/**
 * The "Thought process" affordance, rendered above the answer bubble. While the
 * turn is processing with no answer yet, it shows a subtle live "Thinking…"
 * line. Once reasoning parts arrive, it becomes a collapsible block (collapsed
 * by default). When `show` is false (level Off), it renders nothing.
 */
export function MessageThinking({
  message,
  streaming,
  show,
}: {
  message: UIMessage;
  streaming: boolean;
  show: boolean;
}) {
  const reasoningText = useMemo(() => collectReasoning(message), [message]);
  const [open, setOpen] = useState(false);

  if (!show) {
    return null;
  }

  const answered = hasAnswerText(message);

  // Only render the collapsible once real reasoning text exists. The live
  // "working" affordance during the pre-text wait is owned by the chat panel so
  // there is exactly one progress indicator at every thinking level.
  if (!reasoningText) {
    return null;
  }

  return (
    <div className="overflow-hidden rounded-md border border-dashed border-border bg-surface-2/30">
      <button
        type="button"
        onClick={() => setOpen(!open)}
        aria-expanded={open}
        className="flex w-full items-center gap-2 px-2.5 py-1.5 text-left text-[11px] font-medium text-muted-foreground transition-colors hover:bg-surface-2/60"
      >
        <IconBrain width={13} height={13} className="shrink-0 text-subtle-foreground" />
        <span>Thought process</span>
        {streaming && !answered && <span className="animate-pulse text-primary">· reasoning…</span>}
        <IconChevron
          width={13}
          height={13}
          className={cn(
            "ml-auto shrink-0 transition-transform duration-200",
            open ? "rotate-0" : "-rotate-90",
          )}
        />
      </button>
      {open && (
        <p className="whitespace-pre-wrap break-words px-2.5 pb-2 font-mono text-[11px] leading-relaxed text-subtle-foreground">
          {reasoningText}
        </p>
      )}
    </div>
  );
}

function FilePart({ part }: { part: Extract<Part, { type: "file" }> }) {
  if (part.mediaType.startsWith("image/")) {
    return (
      <figure className="my-1.5 overflow-hidden rounded-md border border-border bg-surface-2">
        <Image
          src={part.url}
          alt={part.filename ?? "Attached image"}
          width={512}
          height={512}
          unoptimized
          className="h-auto max-h-64 w-auto max-w-full object-contain"
        />
        {part.filename && (
          <figcaption className="border-t border-border px-2 py-1 text-[11px] text-muted-foreground">
            {part.filename}
          </figcaption>
        )}
      </figure>
    );
  }
  return (
    <div className="my-1.5 rounded-md border border-border bg-surface-2/60 px-2.5 py-1.5 text-xs text-muted-foreground">
      {part.filename ?? "Attached file"}
    </div>
  );
}

/**
 * The answer-bubble body: ONLY text and file parts. Reasoning and the tool
 * activity timeline are rendered separately (above the bubble) by
 * MessageThinking / MessageActivity.
 */
export function MessageParts({
  message,
  streaming = false,
}: {
  message: UIMessage;
  streaming?: boolean;
}) {
  // Skip empty text parts: the AI SDK opens a text part on `text-start` before
  // the first delta, and rendering it produces an empty bubble with a lone
  // caret. We only render text once it actually has content.
  const tail = message.parts.filter(
    (part) => (part.type === "text" && part.text.trim().length > 0) || part.type === "file",
  );
  const lastTextIndex = tail.reduce(
    (acc, part, index) => (part.type === "text" ? index : acc),
    -1,
  );
  const isAssistant = message.role === "assistant";

  return (
    <>
      {tail.map((part, index) => {
        if (part.type === "text") {
          const isLast = index === lastTextIndex;
          const caret = streaming && isLast && (
            <span
              aria-hidden="true"
              className="ml-0.5 inline-block h-[1.05em] w-[2px] translate-y-[2px] animate-pulse-dot rounded-full bg-primary align-text-bottom"
            />
          );
          // Assistant prose is real markdown rendered to the Geist tokens; user
          // text stays verbatim. Markdown is a pure function of the text, so it
          // re-renders cleanly as the stream grows.
          if (isAssistant) {
            return (
              <div
                key={index}
                className="reva-md max-w-none break-words [overflow-wrap:anywhere] [&_pre]:overflow-x-auto [&>div]:max-w-none [&>div>*:first-child]:mt-0 [&>div>*:last-child]:mb-0"
              >
                <Markdown content={part.text} />
                {caret}
              </div>
            );
          }
          return (
            <p key={index} className="whitespace-pre-wrap break-words leading-relaxed">
              {part.text}
              {caret}
            </p>
          );
        }
        return <FilePart key={index} part={part as Extract<Part, { type: "file" }>} />;
      })}
    </>
  );
}

/** True when the message has any text or file part for the answer bubble. */
export const messageHasBody = (message: UIMessage): boolean =>
  message.parts.some(
    (part) => (part.type === "text" && part.text.trim().length > 0) || part.type === "file",
  );
