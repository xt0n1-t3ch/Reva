"use client";

import { useEffect, useRef, useState } from "react";
import { useChat } from "@ai-sdk/react";
import { DefaultChatTransport } from "ai";
import Image from "next/image";
import { cn } from "@/lib/cn";
import { config } from "@/lib/config";
import { api } from "@/lib/api/client";
import { MessageParts } from "@/components/chat/message-parts";
import { Badge, Button } from "@/components/ui/primitives";
import { ErrorBanner } from "@/components/ui/states";
import { IconClose, IconDocument, IconSend, IconSparkles, IconUpload } from "@/components/ui/icons";

const suggestions = [
  "Which documents have reconciliation exceptions?",
  "Summarize the latest bordereau and its confidence.",
  "Explain where the gross premium total came from.",
];

type ImageAttachment = {
  id: string;
  file: File;
  previewUrl: string;
};

type DocumentAttachment = {
  id: string;
  fileName: string;
  status: "uploading" | "ready" | "error";
  documentId?: string;
  message?: string;
};

const fileSizeLabel = (bytes: number): string => `${(bytes / 1024 / 1024).toFixed(1)} MB`;

const newAttachmentId = (): string => crypto.randomUUID();

const filesFromInput = (files: FileList | null): File[] => (files ? Array.from(files) : []);

const isAcceptedDocument = (file: File): boolean => {
  const fileName = file.name.toLowerCase();
  return config.chatDocumentExtensions.some((extension) => fileName.endsWith(extension));
};

const createFileList = (attachments: ImageAttachment[]): FileList => {
  const transfer = new DataTransfer();
  for (const attachment of attachments) {
    transfer.items.add(attachment.file);
  }
  return transfer.files;
};

export function ChatPanel({ onClose, onMinimize }: { onClose?: () => void; onMinimize?: () => void }) {
  const { messages, sendMessage, setMessages, status, error, stop } = useChat({
    transport: new DefaultChatTransport({ api: `${config.apiBaseUrl}/api/agent` }),
  });
  const [input, setInput] = useState("");
  const [imageAttachments, setImageAttachments] = useState<ImageAttachment[]>([]);
  const [documentAttachments, setDocumentAttachments] = useState<DocumentAttachment[]>([]);
  const [attachmentMessage, setAttachmentMessage] = useState<string | null>(null);
  const scrollRef = useRef<HTMLDivElement>(null);
  const imageInputRef = useRef<HTMLInputElement>(null);
  const documentInputRef = useRef<HTMLInputElement>(null);
  const imageAttachmentsRef = useRef<ImageAttachment[]>([]);
  const busy = status === "submitted" || status === "streaming";
  const documentsUploading = documentAttachments.some((attachment) => attachment.status === "uploading");
  const readyDocuments = documentAttachments.filter((attachment) => attachment.status === "ready" && attachment.documentId);
  const canSend = !busy && !documentsUploading && (input.trim().length > 0 || imageAttachments.length > 0 || readyDocuments.length > 0);

  useEffect(() => {
    imageAttachmentsRef.current = imageAttachments;
  }, [imageAttachments]);

  useEffect(() => {
    return () => {
      for (const attachment of imageAttachmentsRef.current) {
        URL.revokeObjectURL(attachment.previewUrl);
      }
    };
  }, []);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [messages, status]);

  const clearImageAttachments = () => {
    setImageAttachments((current) => {
      for (const attachment of current) {
        URL.revokeObjectURL(attachment.previewUrl);
      }
      return [];
    });
  };

  const clearAttachments = () => {
    clearImageAttachments();
    setDocumentAttachments([]);
    setAttachmentMessage(null);
    if (imageInputRef.current) {
      imageInputRef.current.value = "";
    }
    if (documentInputRef.current) {
      documentInputRef.current.value = "";
    }
  };

  const showAttachmentMessage = (message: string) => {
    setAttachmentMessage(message);
  };

  const removeImageAttachment = (id: string) => {
    setImageAttachments((current) => {
      const removed = current.find((attachment) => attachment.id === id);
      if (removed) {
        URL.revokeObjectURL(removed.previewUrl);
      }
      return current.filter((attachment) => attachment.id !== id);
    });
  };

  const removeDocumentAttachment = (id: string) => {
    setDocumentAttachments((current) => current.filter((attachment) => attachment.id !== id));
  };

  const attachImages = (files: File[]) => {
    if (files.length === 0) {
      return;
    }
    if (busy) {
      showAttachmentMessage("Wait for the current assistant response before attaching files.");
      return;
    }
    if (imageAttachments.length + files.length > config.chatMaxImageAttachments) {
      showAttachmentMessage(`Attach up to ${config.chatMaxImageAttachments} images per message.`);
      return;
    }
    const invalid = files.find((file) => !file.type.startsWith("image/"));
    if (invalid) {
      showAttachmentMessage(`${invalid.name} is not an image file.`);
      return;
    }
    const oversized = files.find((file) => file.size > config.chatMaxImageBytes);
    if (oversized) {
      showAttachmentMessage(`${oversized.name} is larger than ${fileSizeLabel(config.chatMaxImageBytes)}.`);
      return;
    }
    const attachments = files.map((file) => ({
      id: newAttachmentId(),
      file,
      previewUrl: URL.createObjectURL(file),
    }));
    setImageAttachments((current) => [...current, ...attachments]);
    setAttachmentMessage(null);
  };

  const attachDocuments = (files: File[]) => {
    if (files.length === 0) {
      return;
    }
    if (busy) {
      showAttachmentMessage("Wait for the current assistant response before attaching files.");
      return;
    }
    if (documentAttachments.length + files.length > config.chatMaxDocumentAttachments) {
      showAttachmentMessage(`Attach up to ${config.chatMaxDocumentAttachments} documents per message.`);
      return;
    }
    const invalid = files.find((file) => !isAcceptedDocument(file));
    if (invalid) {
      showAttachmentMessage(`${invalid.name} must be a PDF, CSV, XLSX, EML, or MSG file.`);
      return;
    }
    const oversized = files.find((file) => file.size > config.chatMaxDocumentBytes);
    if (oversized) {
      showAttachmentMessage(`${oversized.name} is larger than ${fileSizeLabel(config.chatMaxDocumentBytes)}.`);
      return;
    }
    for (const file of files) {
      const id = newAttachmentId();
      setDocumentAttachments((current) => [
        ...current,
        { id, fileName: file.name, status: "uploading" },
      ]);
      void api
        .uploadDocument(file)
        .then((result) => {
          setDocumentAttachments((current) =>
            current.map((attachment) =>
              attachment.id === id
                ? { id, fileName: result.fileName, status: "ready", documentId: result.id }
                : attachment,
            ),
          );
        })
        .catch((cause: unknown) => {
          const message = cause instanceof Error ? cause.message : "Upload failed";
          setDocumentAttachments((current) =>
            current.map((attachment) =>
              attachment.id === id ? { ...attachment, status: "error", message } : attachment,
            ),
          );
          showAttachmentMessage(`${file.name} could not be ingested.`);
        });
    }
    setAttachmentMessage(null);
  };

  const documentContext = () =>
    readyDocuments
      .map((attachment) => `Attached document ${attachment.fileName} (id ${attachment.documentId}) was ingested.`)
      .join("\n");

  const submit = async (text: string) => {
    const trimmed = text.trim();
    if (busy) {
      showAttachmentMessage("Wait for the current assistant response before sending another message.");
      return;
    }
    if (documentsUploading) {
      showAttachmentMessage("Wait for document ingestion to finish before sending.");
      return;
    }
    if (!trimmed && imageAttachments.length === 0 && readyDocuments.length === 0) {
      showAttachmentMessage("Add a message or attachment before sending.");
      return;
    }
    const context = documentContext();
    const messageText = [trimmed, context].filter(Boolean).join("\n\n");
    const files = imageAttachments.length > 0 ? createFileList(imageAttachments) : undefined;
    setInput("");
    clearAttachments();
    try {
      if (files && messageText) {
        await sendMessage({ text: messageText, files });
      } else if (files) {
        await sendMessage({ files });
      } else {
        await sendMessage({ text: messageText });
      }
    } catch (cause) {
      showAttachmentMessage(cause instanceof Error ? cause.message : "The assistant message could not be sent.");
    }
  };

  const startNewChat = () => {
    if (busy) {
      stop();
    }
    setMessages([]);
    setInput("");
    clearAttachments();
  };

  return (
    <div className="flex h-full flex-col bg-surface">
      <div className="flex min-h-14 shrink-0 items-center gap-2.5 border-b border-border px-4 py-2">
        <span className="flex size-7 items-center justify-center rounded-md bg-primary-soft text-primary">
          <IconSparkles width={15} height={15} />
        </span>
        <div className="flex min-w-0 flex-1 flex-col">
          <span className="text-sm font-semibold leading-tight">Assistant</span>
          <span className="truncate text-[11px] text-muted-foreground">
            {config.ollamaModel} · local · keyless
          </span>
        </div>
        <div className="flex items-center gap-1">
          <Button type="button" variant="ghost" size="sm" onClick={startNewChat} aria-label="Start a new conversation">
            New
          </Button>
          {onMinimize && (
            <Button type="button" variant="ghost" size="icon" onClick={onMinimize} aria-label="Minimize assistant" className="hidden lg:inline-flex">
              <span className="h-0.5 w-4 rounded-full bg-current" />
            </Button>
          )}
          {onClose && (
            <Button type="button" variant="ghost" size="icon" onClick={onClose} aria-label="Close assistant">
              <IconClose width={16} height={16} />
            </Button>
          )}
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
                  type="button"
                  onClick={() => void submit(prompt)}
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
          <ErrorBanner message={error.message || "The assistant is unavailable. Confirm Ollama is running."} />
        )}
      </div>

      <form
        onSubmit={(event) => {
          event.preventDefault();
          void submit(input);
        }}
        className="shrink-0 border-t border-border p-3"
      >
        {(imageAttachments.length > 0 || documentAttachments.length > 0 || attachmentMessage) && (
          <div className="mb-2 flex flex-col gap-2">
            {imageAttachments.length > 0 && (
              <div className="flex gap-2 overflow-x-auto pb-1">
                {imageAttachments.map((attachment) => (
                  <div key={attachment.id} className="relative size-16 shrink-0 overflow-hidden rounded-md border border-border bg-surface-2">
                    <Image
                      src={attachment.previewUrl}
                      alt={attachment.file.name}
                      fill
                      unoptimized
                      sizes="4rem"
                      className="object-cover"
                    />
                    <button
                      type="button"
                      aria-label={`Remove ${attachment.file.name}`}
                      onClick={() => removeImageAttachment(attachment.id)}
                      className="absolute right-1 top-1 inline-flex size-6 items-center justify-center rounded-full bg-surface text-muted-foreground shadow-soft hover:text-foreground"
                    >
                      <IconClose width={13} height={13} />
                    </button>
                  </div>
                ))}
              </div>
            )}
            {documentAttachments.length > 0 && (
              <div className="flex flex-wrap gap-1.5">
                {documentAttachments.map((attachment) => (
                  <span key={attachment.id} className="inline-flex max-w-full items-center gap-1.5 rounded-full border border-border bg-surface-2 px-2 py-1 text-xs">
                    <IconDocument width={13} height={13} className="shrink-0 text-muted-foreground" />
                    <span className="min-w-0 truncate">{attachment.fileName}</span>
                    <Badge tone={attachment.status === "ready" ? "success" : attachment.status === "error" ? "danger" : "primary"}>
                      {attachment.status}
                    </Badge>
                    <button
                      type="button"
                      aria-label={`Remove ${attachment.fileName}`}
                      onClick={() => removeDocumentAttachment(attachment.id)}
                      className="inline-flex size-5 shrink-0 items-center justify-center rounded-full text-muted-foreground hover:bg-surface-3 hover:text-foreground"
                    >
                      <IconClose width={12} height={12} />
                    </button>
                  </span>
                ))}
              </div>
            )}
            {attachmentMessage && <p className="text-xs text-danger">{attachmentMessage}</p>}
          </div>
        )}
        <div className="flex items-end gap-2 rounded-lg border border-border bg-surface-2/50 p-1.5 focus-within:border-primary-border">
          <input
            ref={imageInputRef}
            type="file"
            accept={config.chatImageAccept}
            multiple
            className="hidden"
            onChange={(event) => {
              attachImages(filesFromInput(event.currentTarget.files));
              event.currentTarget.value = "";
            }}
          />
          <input
            ref={documentInputRef}
            type="file"
            accept={config.chatDocumentAccept}
            multiple
            className="hidden"
            onChange={(event) => {
              attachDocuments(filesFromInput(event.currentTarget.files));
              event.currentTarget.value = "";
            }}
          />
          <Button
            type="button"
            variant="ghost"
            size="icon"
            disabled={busy}
            onClick={() => imageInputRef.current?.click()}
            aria-label="Attach images"
          >
            <IconUpload width={16} height={16} />
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            disabled={busy}
            onClick={() => documentInputRef.current?.click()}
            aria-label="Attach documents"
          >
            <IconDocument width={16} height={16} />
          </Button>
          <textarea
            value={input}
            onChange={(event) => setInput(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                void submit(input);
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
            <Button type="submit" variant="primary" size="icon" disabled={!canSend} aria-label="Send">
              <IconSend width={16} height={16} />
            </Button>
          )}
        </div>
      </form>
    </div>
  );
}
