import { type ReactNode } from "react";

// Minimal, dependency-free markdown renderer for the knowledge base: headings,
// bold, inline code, and ordered/unordered lists, styled to the Geist tokens.
const renderInline = (text: string): ReactNode[] => {
  const nodes: ReactNode[] = [];
  const regex = /(\*\*([^*]+)\*\*|`([^`]+)`)/g;
  let last = 0;
  let key = 0;
  let match: RegExpExecArray | null;
  while ((match = regex.exec(text)) !== null) {
    if (match.index > last) {
      nodes.push(text.slice(last, match.index));
    }
    if (match[2] !== undefined) {
      nodes.push(
        <strong key={key++} className="font-semibold text-foreground">
          {match[2]}
        </strong>,
      );
    } else if (match[3] !== undefined) {
      nodes.push(
        <code
          key={key++}
          className="rounded bg-surface-2 px-1 py-0.5 font-mono text-[0.85em] text-foreground"
        >
          {match[3]}
        </code>,
      );
    }
    last = match.index + match[0].length;
  }
  if (last < text.length) {
    nodes.push(text.slice(last));
  }
  return nodes;
};

export function Markdown({ content }: { content: string }) {
  const lines = content.split("\n");
  const blocks: ReactNode[] = [];
  let bullets: string[] = [];
  let numbers: string[] = [];
  let codeLines: string[] | null = null;
  let key = 0;

  const flushCode = () => {
    if (codeLines === null) return;
    blocks.push(
      <pre
        key={key++}
        className="my-3 overflow-x-auto rounded-md border border-border bg-surface-2 px-3 py-2.5"
      >
        <code className="font-mono text-[12.5px] leading-relaxed text-foreground">{codeLines.join("\n")}</code>
      </pre>,
    );
    codeLines = null;
  };

  const flushBullets = () => {
    if (bullets.length === 0) return;
    blocks.push(
      <ul key={key++} className="my-3 flex flex-col gap-1.5">
        {bullets.map((item, index) => (
          <li key={index} className="flex gap-2.5 text-sm leading-relaxed text-foreground/90">
            <span aria-hidden="true" className="mt-[0.5em] size-1 shrink-0 rounded-full bg-border-strong" />
            <span>{renderInline(item)}</span>
          </li>
        ))}
      </ul>,
    );
    bullets = [];
  };

  const flushNumbers = () => {
    if (numbers.length === 0) return;
    blocks.push(
      <ol key={key++} className="my-3 flex flex-col gap-1.5">
        {numbers.map((item, index) => (
          <li key={index} className="flex gap-2.5 text-sm leading-relaxed text-foreground/90">
            <span className="mt-px font-mono text-xs tabular text-subtle-foreground">{index + 1}.</span>
            <span>{renderInline(item)}</span>
          </li>
        ))}
      </ol>,
    );
    numbers = [];
  };

  const flush = () => {
    flushBullets();
    flushNumbers();
  };

  for (const raw of lines) {
    const line = raw.trimEnd();
    if (line.trim().startsWith("```")) {
      if (codeLines === null) {
        flush();
        codeLines = [];
      } else {
        flushCode();
      }
      continue;
    }
    if (codeLines !== null) {
      codeLines.push(raw);
      continue;
    }
    if (/^#\s/.test(line)) {
      flush();
      blocks.push(
        <h1 key={key++} className="mb-3 mt-1 text-xl font-semibold tracking-tight text-foreground">
          {renderInline(line.slice(2))}
        </h1>,
      );
    } else if (/^##\s/.test(line)) {
      flush();
      blocks.push(
        <h2 key={key++} className="mb-2 mt-6 text-base font-semibold tracking-tight text-foreground">
          {renderInline(line.slice(3))}
        </h2>,
      );
    } else if (/^###\s/.test(line)) {
      flush();
      blocks.push(
        <h3 key={key++} className="mb-1.5 mt-4 text-sm font-semibold text-foreground">
          {renderInline(line.slice(4))}
        </h3>,
      );
    } else if (/^\s*[-*]\s/.test(line)) {
      flushNumbers();
      bullets.push(line.replace(/^\s*[-*]\s+/, ""));
    } else if (/^\s*\d+\.\s/.test(line)) {
      flushBullets();
      numbers.push(line.replace(/^\s*\d+\.\s+/, ""));
    } else if (line.trim() === "") {
      flush();
    } else {
      flush();
      blocks.push(
        <p key={key++} className="my-2 text-sm leading-relaxed text-foreground/90">
          {renderInline(line)}
        </p>,
      );
    }
  }
  flushCode();
  flush();

  return <div className="max-w-2xl">{blocks}</div>;
}
