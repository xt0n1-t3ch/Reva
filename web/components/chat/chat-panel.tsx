"use client";

import { useEffect, useRef, useState } from "react";
import { useChat } from "@ai-sdk/react";
import { DefaultChatTransport } from "ai";
import { cn } from "@/lib/cn";
import { config } from "@/lib/config";
import { MessageParts } from "@/components/chat/message-parts";
import { Button } from "@/components/ui/primitives";
import { IconSend, IconSparkles } from "@/components/ui/icons";

const suggestions = [
  "Which documents have reconciliation exceptions?",
  "Summarize the latest bordereau and its confidence.",
  "Explain where the gross premium total came from.",
];

export function ChatPanel() {
  const { messages, sendMessage, status, error, stop } = useChat({
    transport: new DefaultChatTransport({ api: "/api/agent" }),
  });
  const [input, setInput] = useState("");
  const scrollRef = useRef<HTMLDivElement>(null);
  const busy = status === "submitted" || status === "streaming";

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [messages, status]);

  const submit = (text: string) => {
    const trimmed = text.trim();
    if (!trimmed || busy) {
      return;
    }
    setInput("");
    void sendMessage({ text: trimmed });
  };

  return (
    <div className="flex h-full flex-col bg-surface">
      <div className="flex h-14 shrink-0 items-center gap-2.5 border-b border-border px-4">
        <span className="flex size-7 items-center justify-center rounded-md bg-primary-soft text-primary">
          <IconSparkles width={15} height={15} />
        </span>
        <div className="flex min-w-0 flex-col">
          <span className="text-sm font-semibold leading-tight">Assistant</span>
          <span className="truncate text-[11px] text-muted-foreground">
            {config.ollamaModel} · local · keyless
          </span>
        </div>
      </div>

      <div ref={scrollRef} className="flex-1 space-y-4 overflow-y-auto px-4 py-4">
        {messages.length === 0 && (
          <div className="flex flex-col gap-3 pt-2">
            <p className="text-sm leading-relaxed text-muted-foreground">
              Ask about any ingested bordereau — fields, classification, exceptions, reconciliation,
              or where a value came from. Answers are grounded in the deterministic engine.
            </p>
            <div className="flex flex-col gap-1.5">
              {suggestions.map((prompt) => (
                <button
                  key={prompt}
                  onClick={() => submit(prompt)}
                  className="rounded-md border border-border bg-surface-2/60 px-3 py-2 text-left text-xs text-foreground transition-colors hover:border-primary-border hover:bg-primary-soft"
                >
                  {prompt}
                </button>
              ))}
            </div>
          </div>
        )}

        {messages.map((message) => (
          <div
            key={message.id}
            className={cn("flex flex-col gap-1", message.role === "user" ? "items-end" : "items-start")}
          >
            <span className="px-1 text-[10px] font-semibold uppercase tracking-wider text-subtle-foreground">
              {message.role === "user" ? "You" : "Assistant"}
            </span>
            <div
              className={cn(
                "max-w-[92%] rounded-lg px-3 py-2 text-sm",
                message.role === "user"
                  ? "bg-primary text-primary-foreground"
                  : "border border-border bg-surface-2/50 text-foreground",
              )}
            >
              <MessageParts message={message} />
            </div>
          </div>
        ))}

        {status === "submitted" && (
          <div className="flex items-center gap-1.5 px-1 text-xs text-muted-foreground">
            <span className="size-1.5 animate-pulse-dot rounded-full bg-primary" />
            <span>Thinking…</span>
          </div>
        )}

        {error && (
          <div className="rounded-md border border-danger/30 bg-danger-soft px-3 py-2 text-xs text-danger">
            {error.message || "The assistant is unavailable. Confirm Ollama is running."}
          </div>
        )}
      </div>

      <form
        onSubmit={(event) => {
          event.preventDefault();
          submit(input);
        }}
        className="shrink-0 border-t border-border p-3"
      >
        <div className="flex items-end gap-2 rounded-lg border border-border bg-surface-2/50 p-1.5 focus-within:border-primary-border">
          <textarea
            value={input}
            onChange={(event) => setInput(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                submit(input);
              }
            }}
            rows={1}
            placeholder="Ask about a document…"
            aria-label="Message the assistant"
            className="max-h-32 min-h-[2.25rem] flex-1 resize-none bg-transparent px-2 py-1.5 text-sm outline-none placeholder:text-subtle-foreground"
          />
          {busy ? (
            <Button type="button" variant="subtle" size="icon" onClick={stop} aria-label="Stop">
              <span className="size-3 rounded-[2px] bg-current" />
            </Button>
          ) : (
            <Button type="submit" variant="primary" size="icon" disabled={!input.trim()} aria-label="Send">
              <IconSend width={16} height={16} />
            </Button>
          )}
        </div>
      </form>
    </div>
  );
}
