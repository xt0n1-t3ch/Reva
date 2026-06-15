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

function QueueRow({ document }: { document: DocumentSummary }) {
  const tier = confidenceTier(document.confidence);
  return (
    <Link
      href={`/review?doc=${document.id}`}
      className="group grid grid-cols-[1fr_auto] items-center gap-x-3 gap-y-2 border-b border-border px-3 py-2.5 transition-colors last:border-0 hover:bg-surface-2/50 md:grid-cols-[minmax(0,1fr)_130px_150px_88px_128px_28px] md:py-2"
    >
      <div className="flex min-w-0 items-center gap-2.5">
        <span className="flex size-8 shrink-0 items-center justify-center rounded-md bg-surface-2 text-muted-foreground">
          <IconDocument width={16} height={16} />
        </span>
        <span className="flex min-w-0 flex-col">
          <span className="truncate text-sm font-medium">{document.fileName}</span>
          <span className="truncate text-[11px] text-muted-foreground">
            Updated {formatRelative(document.updatedAt)}
          </span>
        </span>
      </div>

      <div className="hidden md:block">
        <Badge tone="neutral">{documentTypeLabel(document.documentType)}</Badge>
      </div>

      <div className="hidden md:block">
        {document.confidence > 0 ? (
          <ConfidenceMeter score={document.confidence} tier={tier} />
        ) : (
          <span className="text-xs text-subtle-foreground">—</span>
        )}
      </div>

      <div className="hidden md:block">
        {document.exceptionCount > 0 ? (
          <span className="inline-flex items-center gap-1.5 text-xs font-medium text-danger">
            <Dot tone="danger" />
            {document.exceptionCount}
          </span>
        ) : (
          <span className="inline-flex items-center gap-1.5 text-xs text-success">
            <Dot tone="success" />
            clear
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

      <IconChevronRight
        width={16}
        height={16}
        className="hidden text-subtle-foreground transition-transform group-hover:translate-x-0.5 md:block"
      />
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
      <div className="flex flex-col gap-2 p-3">
        {Array.from({ length: 5 }).map((_, index) => (
          <Skeleton key={index} className="h-12" />
        ))}
      </div>
    );
  }

  if (documents.length === 0) {
    return (
      <div className="p-4">
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
      <div className="hidden grid-cols-[minmax(0,1fr)_130px_150px_88px_128px_28px] gap-x-3 border-b border-border px-3 py-2 text-[11px] font-semibold uppercase tracking-wider text-subtle-foreground md:grid">
        <span>Document</span>
        <span>Type</span>
        <span>Confidence</span>
        <span>Issues</span>
        <span>Review</span>
        <span />
      </div>
      <div role="list">
        {documents.map((document) => (
          <QueueRow key={document.id} document={document} />
        ))}
      </div>
    </div>
  );
}
