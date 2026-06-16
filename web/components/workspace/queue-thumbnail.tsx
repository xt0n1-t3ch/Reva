"use client";

import { useState } from "react";
import { cn } from "@/lib/cn";
import { api } from "@/lib/api/client";
import { IconDocument } from "@/components/ui/icons";

// Lazily probes the document's first page image. Digital documents (e-mail, CSV,
// plain text) have no rendered page, so the request 404s and the generic icon stays.
export function QueueThumbnail({ documentId }: { documentId: string }) {
  const [hasImage, setHasImage] = useState(false);

  return (
    <span className="relative flex h-11 w-9 shrink-0 items-center justify-center overflow-hidden rounded-md border border-border bg-surface-2 text-muted-foreground">
      {!hasImage && <IconDocument width={16} height={16} />}
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img
        src={api.pageImageUrl(documentId, 1)}
        alt=""
        aria-hidden="true"
        loading="lazy"
        onLoad={() => setHasImage(true)}
        onError={() => setHasImage(false)}
        className={cn(
          "absolute inset-0 h-full w-full object-cover object-top transition-opacity",
          hasImage ? "opacity-100" : "opacity-0",
        )}
      />
    </span>
  );
}
