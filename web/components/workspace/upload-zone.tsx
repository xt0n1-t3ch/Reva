"use client";

import { useRef, useState } from "react";
import { cn } from "@/lib/cn";
import { api } from "@/lib/api/client";
import { Spinner } from "@/components/ui/primitives";
import { IconUpload } from "@/components/ui/icons";
import { ProcessingTheater } from "@/components/workspace/processing-theater";

const ACCEPT = ".pdf,.eml,.msg,.png,.jpg,.jpeg,.tif,.tiff,.csv,.xlsx";

export function UploadZone({ onUploaded }: { onUploaded: () => void }) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragging, setDragging] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [theater, setTheater] = useState<{ id: string; name: string } | null>(null);

  const upload = async (files: FileList | null) => {
    if (!files || files.length === 0 || busy) {
      return;
    }
    setBusy(true);
    setError(null);
    let firstUploaded: { id: string; name: string } | null = null;
    try {
      for (const file of Array.from(files)) {
        const result = await api.uploadDocument(file);
        firstUploaded ??= { id: result.id, name: result.fileName };
      }
      onUploaded();
      if (firstUploaded) {
        setTheater(firstUploaded);
      }
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Upload failed");
    } finally {
      setBusy(false);
      if (inputRef.current) {
        inputRef.current.value = "";
      }
    }
  };

  return (
    <div className="flex flex-col gap-2">
      <button
        type="button"
        data-tour="upload-zone"
        onClick={() => inputRef.current?.click()}
        onDragOver={(event) => {
          event.preventDefault();
          setDragging(true);
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={(event) => {
          event.preventDefault();
          setDragging(false);
          void upload(event.dataTransfer.files);
        }}
        aria-busy={busy}
        className={cn(
          "flex w-full items-center gap-3 rounded-lg border border-dashed px-4 py-3.5 text-left transition-colors duration-200",
          dragging
            ? "border-primary bg-primary-soft"
            : "border-border bg-surface-2/40 hover:border-primary-border hover:bg-surface-2/70",
        )}
      >
        <span
          className={cn(
            "flex size-9 shrink-0 items-center justify-center rounded-md",
            dragging ? "bg-primary text-primary-foreground" : "bg-surface-2 text-muted-foreground",
          )}
        >
          {busy ? <Spinner className="text-primary" /> : <IconUpload width={18} height={18} />}
        </span>
        <span className="flex min-w-0 flex-col">
          <span className="text-sm font-medium">
            {busy ? "Processing…" : "Drop documents or click to upload"}
          </span>
          <span className="truncate text-[11px] text-muted-foreground">
            PDF, scanned images, Excel/CSV, or .eml/.msg email
          </span>
        </span>
      </button>
      <input
        ref={inputRef}
        type="file"
        multiple
        accept={ACCEPT}
        aria-label="Upload reinsurance documents"
        className="sr-only"
        onChange={(event) => void upload(event.target.files)}
      />
      {error && <p className="text-xs text-danger">{error}</p>}

      <ProcessingTheater
        documentId={theater?.id ?? null}
        fileName={theater?.name ?? ""}
        open={theater !== null}
        onOpenChange={(next) => {
          if (!next) setTheater(null);
        }}
        onComplete={onUploaded}
      />
    </div>
  );
}
