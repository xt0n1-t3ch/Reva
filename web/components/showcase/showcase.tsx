"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { cn } from "@/lib/cn";
import { api } from "@/lib/api/client";
import type { DocumentSummary } from "@/lib/api/types";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogBody,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/primitives";
import {
  IconUpload,
  IconSearch,
  IconScale,
  IconReview,
  IconExport,
  IconMappings,
  IconBook,
  IconSparkles,
  IconCheck,
  IconChevronRight,
  IconChevronLeft,
} from "@/components/ui/icons";

type Target = { kind: "route"; href: string } | { kind: "chat" };

interface Chapter {
  Icon: typeof IconUpload;
  title: string;
  blurb: string;
  steps: string[];
  cta: string;
  resolve: (docs: DocumentSummary[]) => Target;
}

const isImage = (fileName: string): boolean => /\.(pdf|png|jpe?g|tiff?)$/i.test(fileName);

const pickImage = (docs: DocumentSummary[]): DocumentSummary | undefined =>
  docs.find((doc) => isImage(doc.fileName));
const pickRecon = (docs: DocumentSummary[]): DocumentSummary | undefined =>
  docs.find((doc) => doc.exceptionCount > 0) ?? docs[0];
const reviewHref = (doc: DocumentSummary | undefined): Target =>
  doc ? { kind: "route", href: `/review?doc=${doc.id}` } : { kind: "route", href: "/review" };

const CHAPTERS: Chapter[] = [
  {
    Icon: IconUpload,
    title: "Ingest any format",
    blurb:
      "Reva accepts the documents a reinsurance operation actually receives — digital and scanned PDFs, photographed submissions, Excel and CSV spreadsheets, and email — and classifies each one by its content and structure, not only its file extension.",
    steps: [
      "Open the Workspace and drag any file onto the upload area.",
      "Reva parses, OCRs scans, classifies the document type, and queues it.",
      "Unknown formats degrade to a visible-text record — never a hard failure.",
    ],
    cta: "Open the Workspace",
    resolve: () => ({ kind: "route", href: "/" }),
  },
  {
    Icon: IconSearch,
    title: "Watch it read, line by line",
    blurb:
      "The moment a document lands, a live processing view streams every stage in real time — parsing, scanning each line, locating fields, mapping headers, and reconciling totals.",
    steps: [
      "From the Workspace, drop a document to launch the live scan.",
      "Each scanned line and each extracted field appears as it is found.",
      "Stages light up green as the deterministic engine completes them.",
    ],
    cta: "Try a live scan",
    resolve: () => ({ kind: "route", href: "/" }),
  },
  {
    Icon: IconReview,
    title: "Source-cited review",
    blurb:
      "Every extracted value is linked back to the exact region of the source it came from. Hover a field to highlight its citation, so every figure remains fully auditable.",
    steps: [
      "Open a document in Review to see the source on the left, fields on the right.",
      "Hover any field to locate its source span.",
      "Edit a value inline; corrections are kept separate from machine confidence.",
    ],
    cta: "Open a review",
    resolve: (docs) => reviewHref(pickImage(docs) ?? docs[0]),
  },
  {
    Icon: IconScale,
    title: "Reconciliation that catches breaks",
    blurb:
      "Reva sums the line items and compares them to each stated control total within a configurable tolerance. Disagreements surface as field-level exceptions you can act on.",
    steps: [
      "Open a document with exceptions to see the control-total panel.",
      "Each break shows stated vs expected and the delta.",
      "Click “Use expected” to apply the computed value, then approve.",
    ],
    cta: "See a reconciliation break",
    resolve: (docs) => reviewHref(pickRecon(docs)),
  },
  {
    Icon: IconReview,
    title: "File it to a clean template",
    blurb:
      "Switch the source pane to the Template view to see the same record ordered the way a reinsurer files a bordereau — parties, contract, financials, and the risk schedule.",
    steps: [
      "Open a document in Review.",
      "Toggle the Source / Template switch above the source pane.",
      "Read the CRS-ordered, presentation-ready layout.",
    ],
    cta: "Open the Template view",
    resolve: (docs) => reviewHref(pickRecon(docs)),
  },
  {
    Icon: IconSparkles,
    title: "Ask the copilot",
    blurb:
      "A grounded assistant answers questions over your real documents and can act on the app — and you can attach images or files and ask about them directly in the chat.",
    steps: [
      "Open the Assistant and ask “Which documents have reconciliation exceptions?”",
      "It runs tools over the live data and cites documents by name.",
      "Attach an image or a document to the message and ask about it.",
    ],
    cta: "Open the Assistant",
    resolve: () => ({ kind: "chat" }),
  },
  {
    Icon: IconMappings,
    title: "Learns each sender",
    blurb:
      "When you correct how a sender's column maps to a canonical field, Reva remembers it — the next document from that broker maps itself. Those learned rules live in Mappings.",
    steps: [
      "Open Mappings to see the per-sender dictionary.",
      "Each rule maps a source header to a canonical reinsurance field.",
      "Corrections made in Review are learned and applied automatically.",
    ],
    cta: "Open Mappings",
    resolve: () => ({ kind: "route", href: "/mappings" }),
  },
  {
    Icon: IconExport,
    title: "Export on your terms",
    blurb:
      "Turn reviewed data into the formats your market needs — CSV, Excel, or JSON — driven by reusable templates, including a Lloyd's CRS-oriented layout.",
    steps: [
      "Open Export to see the built-in templates.",
      "Create a custom template with your own columns.",
      "Download any document in one click.",
    ],
    cta: "Open Export",
    resolve: () => ({ kind: "route", href: "/export" }),
  },
  {
    Icon: IconBook,
    title: "Knowledge, grounded",
    blurb:
      "A built-in knowledge base explains how Reva works and the reinsurance standards it applies — and the copilot answers from it, so the methodology is never hidden.",
    steps: [
      "Open Knowledge to browse guides and industry standards.",
      "Search any topic — bordereaux, CRS v5.2, reconciliation math.",
      "Ask the copilot a methodology question and watch it cite this hub.",
    ],
    cta: "Open Knowledge",
    resolve: () => ({ kind: "route", href: "/knowledge" }),
  },
];

export function Showcase({
  open,
  onOpenChange,
  onOpenChat,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onOpenChat: () => void;
}) {
  const router = useRouter();
  const [docs, setDocs] = useState<DocumentSummary[]>([]);
  const [index, setIndex] = useState(0);
  const [seeding, setSeeding] = useState(false);
  const [note, setNote] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      return;
    }
    setIndex(0);
    setNote(null);
    const controller = new AbortController();
    api
      .listDocuments(controller.signal)
      .then(setDocs)
      .catch(() => setDocs([]));
    return () => controller.abort();
  }, [open]);

  const loadDataset = async () => {
    setSeeding(true);
    setNote(null);
    try {
      const result = await api.reseedDemo();
      const fresh = await api.listDocuments();
      setDocs(fresh);
      setNote(
        result.seeded
          ? `Loaded ${fresh.length} real demo scenarios — ready to present.`
          : `Workspace already has ${fresh.length} documents — ready to present.`,
      );
    } catch {
      setNote("Could not load the demo dataset. Is the API running?");
    } finally {
      setSeeding(false);
    }
  };

  const chapter = CHAPTERS[index];
  const total = CHAPTERS.length;

  const run = useMemo(
    () => () => {
      const target = chapter.resolve(docs);
      onOpenChange(false);
      if (target.kind === "chat") {
        onOpenChat();
        return;
      }
      router.push(target.href);
    },
    [chapter, docs, onOpenChange, onOpenChat, router],
  );

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent size="lg" className="max-w-3xl">
        <DialogHeader
          title={
            <span className="flex items-center gap-2">
              <IconSparkles width={15} height={15} className="text-primary" />
              Reva showcase
            </span>
          }
          description="A guided tour of every capability — open each one live, with real documents."
        />

        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border bg-surface-2/30 px-5 py-2.5">
          <div className="flex min-w-0 items-center gap-2 text-[12px]">
            <span className="shrink-0 rounded-full bg-accent-soft px-2 py-0.5 font-mono text-[11px] font-medium text-accent">
              {docs.length} ready
            </span>
            <span className="min-w-0 text-muted-foreground">
              {note ?? "Real scenarios are bundled in the app — PDF, scans, Excel, CSV, email. No files needed."}
            </span>
          </div>
          <Button size="sm" variant="outline" onClick={loadDataset} disabled={seeding} className="shrink-0">
            {seeding ? "Loading…" : "Load demo scenarios"}
          </Button>
        </div>

        <DialogBody className="grid grid-cols-1 gap-0 p-0 sm:grid-cols-[13rem_1fr]">
          <ol className="hidden border-r border-border bg-surface-2/30 py-2 sm:block">
            {CHAPTERS.map((item, position) => {
              const active = position === index;
              return (
                <li key={item.title}>
                  <button
                    type="button"
                    onClick={() => setIndex(position)}
                    className={cn(
                      "flex w-full items-center gap-2.5 px-3 py-2 text-left text-[12.5px] transition-colors",
                      active ? "bg-surface font-medium text-foreground" : "text-muted-foreground hover:bg-surface-2/60",
                    )}
                  >
                    <span
                      className={cn(
                        "grid size-5 shrink-0 place-items-center rounded-full text-[10px] font-semibold",
                        active ? "bg-primary text-primary-foreground" : "bg-surface-2 text-subtle-foreground",
                      )}
                    >
                      {position + 1}
                    </span>
                    <span className="min-w-0 truncate">{item.title}</span>
                  </button>
                </li>
              );
            })}
          </ol>

          <div className="flex flex-col gap-3 px-5 py-4">
            <div className="flex items-center gap-3">
              <span className="grid size-10 shrink-0 place-items-center rounded-lg border border-primary-border bg-primary-soft text-primary">
                <chapter.Icon width={20} height={20} />
              </span>
              <div className="min-w-0">
                <p className="text-[10px] font-semibold uppercase tracking-[0.1em] text-subtle-foreground">
                  Step {index + 1} of {total}
                </p>
                <h3 className="text-base font-semibold tracking-tight text-foreground">{chapter.title}</h3>
              </div>
            </div>

            <p className="text-sm leading-relaxed text-foreground/90">{chapter.blurb}</p>

            <ol className="flex flex-col gap-2">
              {chapter.steps.map((step, position) => (
                <li key={position} className="flex gap-2.5 text-[13px] leading-relaxed text-muted-foreground">
                  <span className="mt-0.5 grid size-4 shrink-0 place-items-center rounded-full bg-accent-soft text-accent">
                    <IconCheck width={11} height={11} />
                  </span>
                  <span>{step}</span>
                </li>
              ))}
            </ol>

            <div className="mt-auto pt-1">
              <Button size="sm" onClick={run} className="gap-1.5">
                {chapter.cta}
                <IconChevronRight width={14} height={14} />
              </Button>
            </div>
          </div>
        </DialogBody>

        <DialogFooter>
          <div className="mr-auto flex items-center gap-1">
            {CHAPTERS.map((item, position) => (
              <span
                key={item.title}
                aria-hidden="true"
                className={cn(
                  "h-1 rounded-full transition-all",
                  position === index ? "w-5 bg-primary" : "w-1.5 bg-border-strong",
                )}
              />
            ))}
          </div>
          <Button
            variant="outline"
            size="sm"
            disabled={index === 0}
            onClick={() => setIndex((current) => Math.max(0, current - 1))}
          >
            <IconChevronLeft width={14} height={14} />
            Back
          </Button>
          {index < total - 1 ? (
            <Button variant="subtle" size="sm" onClick={() => setIndex((current) => Math.min(total - 1, current + 1))}>
              Next
              <IconChevronRight width={14} height={14} />
            </Button>
          ) : (
            <Button variant="subtle" size="sm" onClick={() => onOpenChange(false)}>
              Done
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
