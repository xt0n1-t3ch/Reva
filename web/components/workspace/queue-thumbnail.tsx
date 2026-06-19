"use client";

import { useState } from "react";
import { cn } from "@/lib/cn";
import { api } from "@/lib/api/client";
import type { ReinsuranceDocumentType } from "@/lib/api/types";
import { IconDocument, IconMail, IconScale, IconReview } from "@/components/ui/icons";

// A compact, document-shaped glyph chosen by document type so digital documents
// (e-mail, CSV, plain text) that never render a page still read as intentional, not broken.
const glyphFor = (documentType?: ReinsuranceDocumentType) => {
  switch (documentType) {
    case "ClaimNotice":
      return IconMail;
    case "LossRun":
    case "StatementOfAccount":
    case "Bordereau":
      return IconReview;
    case "Treaty":
    case "FacultativeSlip":
    case "Endorsement":
      return IconScale;
    default:
      return IconDocument;
  }
};

// Lazily probes the document's first page image. Digital documents (e-mail, CSV,
// plain text) have no rendered page, so the request 404s and the type glyph stays.
// Only these formats render a page image; requesting one for any other type
// (CSV, Excel, e-mail, plain text) just 404s and noises up the console.
const PAGE_IMAGE_EXTENSIONS = [".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff"];

const rendersPage = (fileName?: string): boolean => {
  const lower = (fileName ?? "").toLowerCase();
  return PAGE_IMAGE_EXTENSIONS.some((extension) => lower.endsWith(extension));
};

export function QueueThumbnail({
  documentId,
  documentType,
  fileName,
}: {
  documentId: string;
  documentType?: ReinsuranceDocumentType;
  fileName?: string;
}) {
  const [hasImage, setHasImage] = useState(false);
  const Glyph = glyphFor(documentType);
  const canRenderPage = rendersPage(fileName);

  return (
    <span className="relative flex h-12 w-[2.375rem] shrink-0 items-center justify-center overflow-hidden rounded-md border border-border bg-surface-2 text-subtle-foreground shadow-soft ring-1 ring-inset ring-border/40 transition-[border-color,box-shadow] group-hover:border-border-strong group-hover:shadow-pop">
      {!hasImage && (
        <span className="bg-dotgrid flex h-full w-full items-center justify-center bg-surface-2">
          <Glyph width={15} height={15} />
        </span>
      )}
      {canRenderPage && (
        <>
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={api.pageImageUrl(documentId, 1)}
            alt=""
            aria-hidden="true"
            loading="lazy"
            onLoad={() => setHasImage(true)}
            onError={() => setHasImage(false)}
            className={cn(
              "absolute inset-0 h-full w-full object-cover object-top transition-opacity duration-300",
              hasImage ? "opacity-100" : "opacity-0",
            )}
          />
        </>
      )}
      {/* A faint top sheen sells the thumbnail as a real page edge, not a flat box. */}
      {hasImage && (
        <span
          aria-hidden="true"
          className="pointer-events-none absolute inset-x-0 top-0 h-1/3 bg-gradient-to-b from-black/[0.06] to-transparent dark:from-white/[0.06]"
        />
      )}
    </span>
  );
}
