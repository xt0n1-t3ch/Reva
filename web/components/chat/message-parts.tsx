"use client";

import type { UIMessage } from "ai";
import Image from "next/image";
import { cn } from "@/lib/cn";
import { Spinner } from "@/components/ui/primitives";
import { IconTool } from "@/components/ui/icons";

type Part = UIMessage["parts"][number];

const toolLabel = (type: string): string =>
  type
    .replace(/^tool-/, "")
    .replace(/^dynamic-tool$/, "tool")
    .replace(/_/g, " ");

function ToolChip({ part }: { part: Extract<Part, { type: string }> & Record<string, unknown> }) {
  const state = typeof part.state === "string" ? part.state : "input-available";
  const running = state === "input-streaming" || state === "input-available";
  const failed = state === "output-error";

  return (
    <div className="my-1.5 rounded-md border border-border bg-surface-2/60 px-2.5 py-1.5 text-xs">
      <div className="flex items-center gap-1.5 font-medium text-muted-foreground">
        <IconTool width={13} height={13} className={cn(failed && "text-danger")} />
        <span className="capitalize">{toolLabel(part.type)}</span>
        {running ? (
          <Spinner className="ml-auto size-3 text-primary" />
        ) : (
          <span
            className={cn(
              "ml-auto rounded px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide",
              failed ? "bg-danger-soft text-danger" : "bg-success-soft text-success",
            )}
          >
            {failed ? "error" : "done"}
          </span>
        )}
      </div>
    </div>
  );
}

export function MessageParts({ message }: { message: UIMessage }) {
  return (
    <>
      {message.parts.map((part, index) => {
        if (part.type === "text") {
          return (
            <p key={index} className="whitespace-pre-wrap break-words leading-relaxed">
              {part.text}
            </p>
          );
        }
        if (part.type === "reasoning") {
          return null;
        }
        if (part.type === "file") {
          if (part.mediaType.startsWith("image/")) {
            return (
              <figure key={index} className="my-1.5 overflow-hidden rounded-md border border-border bg-surface-2">
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
            <div key={index} className="my-1.5 rounded-md border border-border bg-surface-2/60 px-2.5 py-1.5 text-xs text-muted-foreground">
              {part.filename ?? "Attached file"}
            </div>
          );
        }
        if (part.type.startsWith("tool-") || part.type === "dynamic-tool") {
          return <ToolChip key={index} part={part as never} />;
        }
        return null;
      })}
    </>
  );
}
