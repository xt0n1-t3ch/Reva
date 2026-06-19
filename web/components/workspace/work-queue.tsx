import Link from "next/link";
import type { DocumentSummary } from "@/lib/api/types";
import {
  confidenceTier,
  documentTypeLabel,
  formatRelative,
  humanizeEnum,
  reviewStateTone,
  statusTone,
} from "@/lib/format";
import { Badge, ConfidenceMeter, Dot } from "@/components/ui/primitives";
import { EmptyState, Skeleton } from "@/components/ui/states";
import { IconChevronRight, IconDocument } from "@/components/ui/icons";
import { QueueThumbnail } from "@/components/workspace/queue-thumbnail";

function QueueRow({ document }: { document: DocumentSummary }) {
  const tier = confidenceTier(document.confidence);
  const clean = document.exceptionCount === 0;
  return (
    <Link
      href={`/review?doc=${document.id}`}
      data-tour="queue-row"
      className="group relative grid grid-cols-[1fr_auto] items-center gap-x-4 gap-y-2 border-b border-border px-4 py-3 outline-none transition-colors last:border-0 hover:bg-surface-2/50 focus-visible:bg-surface-2/50 md:grid-cols-[minmax(0,1fr)_136px_152px_96px_132px_28px]"
    >
      <span
        aria-hidden="true"
        className="absolute inset-y-0 left-0 w-0.5 origin-center scale-y-0 bg-primary transition-transform duration-200 ease-out group-hover:scale-y-100 group-focus-visible:scale-y-100"
      />
      <div className="flex min-w-0 items-center gap-3.5">
        <QueueThumbnail documentId={document.id} documentType={document.documentType} />
        <span className="flex min-w-0 flex-col gap-1">
          <span className="truncate text-sm font-medium tracking-tight text-foreground/90 transition-colors group-hover:text-foreground">
            {document.fileName}
          </span>
          <span className="flex items-center gap-1.5 text-[11px] text-subtle-foreground">
            {/* Type echoes here on narrow screens where the dedicated column is hidden. */}
            <span className="font-medium text-muted-foreground md:hidden">
              {documentTypeLabel(document.documentType)}
            </span>
            <span aria-hidden="true" className="h-2.5 w-px bg-border md:hidden" />
            <span className="font-mono tabular">{formatRelative(document.updatedAt)}</span>
          </span>
        </span>
      </div>

      <div className="hidden items-center md:flex">
        <Badge tone="neutral">{documentTypeLabel(document.documentType)}</Badge>
      </div>

      <div className="hidden items-center md:flex">
        {document.confidence > 0 ? (
          <ConfidenceMeter score={document.confidence} tier={tier} />
        ) : (
          <span className="font-mono text-xs tabular text-subtle-foreground">—</span>
        )}
      </div>

      <div className="hidden items-center md:flex">
        {clean ? (
          <span className="inline-flex items-center gap-1.5 text-[11px] font-medium text-success">
            <Dot tone="success" />
            Clear
          </span>
        ) : (
          <span className="inline-flex items-center gap-1.5 rounded-full border border-danger/25 bg-danger-soft px-2 py-0.5 text-[11px] font-semibold tabular text-danger">
            <Dot tone="danger" />
            {document.exceptionCount}
            <span className="font-medium text-danger/80">
              {document.exceptionCount === 1 ? "issue" : "issues"}
            </span>
          </span>
        )}
      </div>

      <div className="flex items-center justify-end gap-1.5 md:justify-start">
        <Badge tone={statusTone[document.status]} className="md:hidden">
          {humanizeEnum(document.status)}
        </Badge>
        <Badge tone={reviewStateTone[document.reviewState]}>
          {humanizeEnum(document.reviewState)}
        </Badge>
      </div>

      <span className="hidden items-center justify-end text-subtle-foreground md:flex">
        <IconChevronRight
          width={16}
          height={16}
          className="-translate-x-1 opacity-0 transition-all duration-200 ease-out group-hover:translate-x-0 group-hover:opacity-100 group-focus-visible:translate-x-0 group-focus-visible:opacity-100"
        />
      </span>
    </Link>
  );
}

export function WorkQueue({
  documents,
  loading,
}: {
  documents: DocumentSummary[];
  loading: boolean;
}) {
  if (loading) {
    return (
      <div className="flex flex-col gap-2 p-4">
        {Array.from({ length: 5 }).map((_, index) => (
          <Skeleton key={index} className="h-[3.5rem]" />
        ))}
      </div>
    );
  }

  if (documents.length === 0) {
    return (
      <div className="flex h-full min-h-72 items-center justify-center p-4">
        <EmptyState
          icon={<IconDocument width={20} height={20} />}
          title="No documents yet"
          description="Upload a bordereau, statement, or loss run to start extracting and reconciling. Everything runs locally."
        />
      </div>
    );
  }

  return (
    <div>
      <div className="hidden grid-cols-[minmax(0,1fr)_136px_152px_96px_132px_28px] gap-x-4 border-b border-border bg-surface-2/30 px-4 py-2.5 text-[10.5px] font-semibold uppercase tracking-[0.09em] text-subtle-foreground md:grid">
        <span>Document</span>
        <span>Type</span>
        <span>Confidence</span>
        <span>Issues</span>
        <span>Review</span>
        <span />
      </div>
      <div role="list">
        {documents.map((document) => (
          <div role="listitem" key={document.id}>
            <QueueRow document={document} />
          </div>
        ))}
      </div>
    </div>
  );
}
