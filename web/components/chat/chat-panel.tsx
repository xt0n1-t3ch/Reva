"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useChat } from "@ai-sdk/react";
import { DefaultChatTransport, type UIMessage } from "ai";
import Image from "next/image";
import { cn } from "@/lib/cn";
import { config } from "@/lib/config";
import { api } from "@/lib/api/client";
import { useApi } from "@/lib/use-api";
import { resolveChatUsage } from "@/lib/chat-usage";
import {
  base64Size,
  compressImage,
  DEFAULT_IMAGE_TARGET_BYTES,
  formatBytes,
  REQUEST_BYTE_LIMIT,
  type CompressionResult,
} from "@/lib/image-compress";
import { useLocalStorageState } from "@/lib/use-local-storage-state";
import {
  DEFAULT_THINKING_LEVEL,
  parseThinkingLevel,
  readThinkingLevel,
  REASONING_HEADER,
  THINKING_LEVELS,
  THINKING_LEVEL_STORAGE_KEY,
  thinkingLevelLabel,
  thinkingShowsReasoning,
  type ThinkingLevel,
} from "@/lib/chat-thinking";
import {
  MessageActivity,
  MessageParts,
  MessageThinking,
  messageHasBody,
} from "@/components/chat/message-parts";
import { ComposerStats } from "@/components/chat/composer-stats";
import { IconBrain } from "@/components/chat/chat-icons";
import { Badge, Button, Spinner } from "@/components/ui/primitives";
import { ErrorBanner } from "@/components/ui/states";
import { IconClose, IconDocument, IconSend, IconUpload } from "@/components/ui/icons";

/** Pin-to-bottom unless the user scrolled away from the bottom by this slack. */
const SCROLL_PIN_SLACK = 80;

const identitySerialize = (value: ThinkingLevel): string => value;

const suggestions = [
  "Which documents have reconciliation exceptions?",
  "Summarize the latest bordereau and its confidence.",
  "Explain where the gross premium total came from.",
];

type ImageAttachment = {
  id: string;
  file: File;
  previewUrl: string;
  /** Bytes of the originally selected file, before any compression. */
  originalBytes: number;
  /** Compression state for the chip. */
  status: "compressing" | "ready" | "error";
  /** Human label shown on the chip, e.g. "compressed 3.2 MB → 480 KB". */
  note?: string;
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
  const [thinkingLevel, setThinkingLevel] = useLocalStorageState<ThinkingLevel>(
    THINKING_LEVEL_STORAGE_KEY,
    DEFAULT_THINKING_LEVEL,
    parseThinkingLevel,
    identitySerialize,
  );
  // Create the transport exactly once via a lazy state initializer. Its header
  // getter reads the persisted level from localStorage at REQUEST time (when
  // the transport calls it), so the chosen level rides every request without
  // re-creating the transport or holding the value in render-scope state.
  const [transport] = useState(
    () =>
      new DefaultChatTransport<UIMessage>({
        api: `${config.apiBaseUrl}/api/agent`,
        headers: () => ({ [REASONING_HEADER]: readThinkingLevel() }),
      }),
  );

  const { messages, sendMessage, setMessages, status, error, stop } = useChat({ transport });
  const settings = useApi((signal) => api.getSettings(signal));
  const ai = settings.data;
  const modelLabel = ai?.aiModel || "—";
  const providerLabel = ai?.aiProvider === "HuggingFace" ? "cloud" : "local";
  const keyLabel = ai?.aiApiKey ? "keyed" : "keyless";
  const [input, setInput] = useState("");
  const [imageAttachments, setImageAttachments] = useState<ImageAttachment[]>([]);
  const [documentAttachments, setDocumentAttachments] = useState<DocumentAttachment[]>([]);
  const [attachmentMessage, setAttachmentMessage] = useState<string | null>(null);
  const [levelMenuOpen, setLevelMenuOpen] = useState(false);
  const [durationMs, setDurationMs] = useState<number | null>(null);
  const turnStartRef = useRef<number | null>(null);
  const scrollRef = useRef<HTMLDivElement>(null);
  const pinnedRef = useRef(true);
  const imageInputRef = useRef<HTMLInputElement>(null);
  const documentInputRef = useRef<HTMLInputElement>(null);
  const imageAttachmentsRef = useRef<ImageAttachment[]>([]);
  const busy = status === "submitted" || status === "streaming";
  const documentsUploading = documentAttachments.some((attachment) => attachment.status === "uploading");
  const imagesCompressing = imageAttachments.some((attachment) => attachment.status === "compressing");
  const readyImages = imageAttachments.filter((attachment) => attachment.status === "ready");
  const readyDocuments = documentAttachments.filter((attachment) => attachment.status === "ready" && attachment.documentId);
  const canSend =
    !busy &&
    !documentsUploading &&
    !imagesCompressing &&
    (input.trim().length > 0 || readyImages.length > 0 || readyDocuments.length > 0);

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

  const usage = useMemo(() => resolveChatUsage(messages), [messages]);

  // Live timer for the active turn; freezes at the final elapsed once idle so
  // the composer can show "the last response took 2.4s". Works even when the
  // server does not report usage/timing on the message metadata.
  useEffect(() => {
    if (busy) {
      if (turnStartRef.current == null) {
        turnStartRef.current = performance.now();
        setDurationMs(0);
      }
      let frame = 0;
      const tick = () => {
        if (turnStartRef.current != null) {
          setDurationMs(performance.now() - turnStartRef.current);
        }
        frame = window.requestAnimationFrame(tick);
      };
      frame = window.requestAnimationFrame(tick);
      return () => window.cancelAnimationFrame(frame);
    }
    if (turnStartRef.current != null) {
      setDurationMs(performance.now() - turnStartRef.current);
      turnStartRef.current = null;
    }
  }, [busy]);

  // Autoscroll: stay pinned to the bottom while streaming, but yield the moment
  // the user scrolls up to read earlier content.
  useEffect(() => {
    const node = scrollRef.current;
    if (!node) return;
    const onScroll = () => {
      const distance = node.scrollHeight - node.scrollTop - node.clientHeight;
      pinnedRef.current = distance <= SCROLL_PIN_SLACK;
    };
    node.addEventListener("scroll", onScroll, { passive: true });
    return () => node.removeEventListener("scroll", onScroll);
  }, []);

  useEffect(() => {
    if (!pinnedRef.current) return;
    const node = scrollRef.current;
    if (!node) return;
    node.scrollTo({ top: node.scrollHeight, behavior: status === "streaming" ? "auto" : "smooth" });
  }, [messages, status]);

  useEffect(() => {
    const handler = () => settings.refresh();
    window.addEventListener("reva:settings-updated", handler);
    return () => window.removeEventListener("reva:settings-updated", handler);
  }, [settings.refresh]);

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
    setAttachmentMessage(null);

    // Account for several images sharing the request budget: each gets a
    // tighter target as more are attached, so the combined base64 payload
    // stays comfortably under the backend's request limit.
    const projectedCount = imageAttachments.length + files.length;
    const perImageTarget = Math.min(
      DEFAULT_IMAGE_TARGET_BYTES,
      Math.floor((REQUEST_BYTE_LIMIT * 0.7) / Math.max(1, projectedCount) / (4 / 3)),
    );

    for (const file of files) {
      const id = newAttachmentId();
      setImageAttachments((current) => [
        ...current,
        {
          id,
          file,
          previewUrl: URL.createObjectURL(file),
          originalBytes: file.size,
          status: "compressing",
        },
      ]);

      void compressImage(file, perImageTarget)
        .then((result: CompressionResult) => {
          setImageAttachments((current) =>
            current.map((attachment) => {
              if (attachment.id !== id) {
                return attachment;
              }
              if (!result.ok) {
                return {
                  ...attachment,
                  status: "error",
                  note: "could not process image",
                };
              }
              const note = result.changed
                ? `compressed ${formatBytes(result.originalBytes)} → ${formatBytes(result.compressedBytes)}`
                : formatBytes(result.compressedBytes);
              return { ...attachment, file: result.file, status: "ready", note };
            }),
          );
        })
        .catch(() => {
          setImageAttachments((current) =>
            current.map((attachment) =>
              attachment.id === id
                ? { ...attachment, status: "error", note: "could not process image" }
                : attachment,
            ),
          );
        });
    }
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
    if (imagesCompressing) {
      showAttachmentMessage("Optimizing your image — this will finish in a moment.");
      return;
    }
    if (!trimmed && readyImages.length === 0 && readyDocuments.length === 0) {
      showAttachmentMessage("Add a message or attachment before sending.");
      return;
    }

    // Guard the backend's ~1MB request ceiling locally so the raw
    // "request too large" error never reaches the user. Images are base64-
    // inlined, so estimate the wire cost and bail with a friendly message
    // when even the compressed set won't fit.
    if (readyImages.length > 0) {
      const imageWireBytes = readyImages.reduce(
        (sum, attachment) => sum + base64Size(attachment.file.size),
        0,
      );
      const textBytes = new TextEncoder().encode(trimmed).length;
      if (imageWireBytes + textBytes > REQUEST_BYTE_LIMIT * 0.9) {
        showAttachmentMessage(
          readyImages.length > 1
            ? "These images are too large together even after compression — remove one and try again."
            : "This image was compressed as much as possible but is still too large — try a smaller one.",
        );
        return;
      }
    }

    const context = documentContext();
    const messageText = [trimmed, context].filter(Boolean).join("\n\n");
    const files = readyImages.length > 0 ? createFileList(readyImages) : undefined;
    setInput("");
    clearAttachments();
    pinnedRef.current = true;
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
    setDurationMs(null);
    turnStartRef.current = null;
    pinnedRef.current = true;
  };

  return (
    <div className="flex h-full flex-col bg-surface">
      <div className="flex min-h-14 shrink-0 items-center gap-2.5 border-b border-border px-4 py-2">
        <span className="flex size-7 shrink-0 items-center justify-center rounded-md border border-border bg-surface-2">
          <Image
            src="/reva-logo-dark.png"
            alt=""
            aria-hidden="true"
            width={17}
            height={17}
            className="size-[17px] object-contain dark:hidden"
          />
          <Image
            src="/reva-logo-light.png"
            alt=""
            aria-hidden="true"
            width={17}
            height={17}
            className="hidden size-[17px] object-contain dark:block"
          />
        </span>
        <div className="flex min-w-0 flex-1 flex-col">
          <span className="text-sm font-semibold leading-tight">Assistant</span>
          <span className="truncate font-mono text-[11px] text-muted-foreground">
            {modelLabel} · {providerLabel} · {keyLabel} · think:{thinkingLevelLabel(thinkingLevel).toLowerCase()}
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
                  className="group rounded-md border border-border bg-surface-2/60 px-3 py-2 text-left text-xs leading-relaxed text-muted-foreground transition-colors hover:border-primary-border hover:bg-primary-soft hover:text-foreground"
                >
                  {prompt}
                </button>
              ))}
            </div>
          </div>
        )}

        {messages.map((message, index) => {
          const isLast = index === messages.length - 1;
          const streaming = busy && isLast && message.role === "assistant";
          const isAssistant = message.role === "assistant";
          const hasBody = messageHasBody(message);
          return (
            <div
              key={message.id}
              className={cn("flex flex-col gap-1.5", isAssistant ? "items-start" : "items-end")}
            >
              <span className="px-1 text-[10px] font-semibold uppercase tracking-wider text-subtle-foreground">
                {isAssistant ? "Assistant" : "You"}
              </span>

              {/* ChatGPT/Claude-style: reasoning + tool activity sit in the
                  message ROW above the bubble, not boxed inside the answer. */}
              {isAssistant && (
                <div className="flex w-full max-w-[92%] flex-col gap-1.5">
                  <MessageThinking
                    message={message}
                    streaming={streaming}
                    show={thinkingShowsReasoning(thinkingLevel)}
                  />
                  <MessageActivity message={message} streaming={streaming} />
                  {streaming && !hasBody && (
                    <div className="flex items-center gap-2 px-0.5 text-[11px] text-muted-foreground">
                      <Spinner className="size-3 shrink-0 text-primary" />
                      <span className="font-medium">Working…</span>
                      {durationMs != null && durationMs > 0 && (
                        <span className="font-mono tabular text-subtle-foreground">
                          {(durationMs / 1000).toFixed(1)}s
                        </span>
                      )}
                    </div>
                  )}
                </div>
              )}

              {(hasBody || !isAssistant) && (
                <div
                  className={cn(
                    "max-w-[92%] rounded-lg px-3 py-2 text-sm",
                    isAssistant
                      ? "rounded-bl-sm border border-border bg-surface-2/50 text-foreground"
                      : "rounded-br-sm bg-primary text-primary-foreground shadow-soft",
                  )}
                >
                  <MessageParts message={message} streaming={streaming} />
                </div>
              )}
            </div>
          );
        })}

        {/* Before the assistant message exists, show a live working affordance
            at every thinking level so the turn never looks dead. */}
        {status === "submitted" && messages[messages.length - 1]?.role !== "assistant" && (
          <div className="flex items-center gap-2 px-1 text-[11px] text-muted-foreground">
            <Spinner className="size-3 shrink-0 text-primary" />
            <span className="font-medium">Working…</span>
            {durationMs != null && durationMs > 0 && (
              <span className="font-mono tabular text-subtle-foreground">
                {(durationMs / 1000).toFixed(1)}s
              </span>
            )}
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
        {messages.length > 0 && (
          <ComposerStats usage={usage} durationMs={durationMs} running={busy} />
        )}
        {(imageAttachments.length > 0 || documentAttachments.length > 0 || attachmentMessage) && (
          <div className="mb-2 flex flex-col gap-2">
            {imageAttachments.length > 0 && (
              <div className="flex flex-col gap-1.5">
                {imageAttachments.map((attachment) => (
                  <div
                    key={attachment.id}
                    className="flex items-center gap-2.5 rounded-md border border-border bg-surface-2/60 p-1.5"
                  >
                    <div className="relative size-11 shrink-0 overflow-hidden rounded border border-border bg-surface-2">
                      <Image
                        src={attachment.previewUrl}
                        alt={attachment.file.name}
                        fill
                        unoptimized
                        sizes="2.75rem"
                        className="object-cover"
                      />
                      {attachment.status === "compressing" && (
                        <span className="absolute inset-0 grid place-items-center bg-surface/70">
                          <Spinner className="size-3.5 text-primary" />
                        </span>
                      )}
                    </div>
                    <div className="flex min-w-0 flex-1 flex-col leading-tight">
                      <span className="truncate text-xs font-medium text-foreground">
                        {attachment.file.name}
                      </span>
                      <span
                        className={cn(
                          "truncate font-mono text-[10px]",
                          attachment.status === "error" ? "text-danger" : "text-subtle-foreground",
                        )}
                      >
                        {attachment.status === "compressing"
                          ? "optimizing…"
                          : (attachment.note ?? formatBytes(attachment.originalBytes))}
                      </span>
                    </div>
                    <button
                      type="button"
                      aria-label={`Remove ${attachment.file.name}`}
                      onClick={() => removeImageAttachment(attachment.id)}
                      className="inline-flex size-6 shrink-0 items-center justify-center rounded-full text-muted-foreground hover:bg-surface-3 hover:text-foreground"
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
        <div className="flex items-end gap-2 rounded-lg border border-border bg-surface-2/50 p-1.5 transition-colors focus-within:border-primary-border focus-within:bg-surface-2/80">
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
          <div className="relative">
            <Button
              type="button"
              variant="ghost"
              size="icon"
              onClick={() => setLevelMenuOpen((open) => !open)}
              aria-haspopup="menu"
              aria-expanded={levelMenuOpen}
              aria-label={`Reasoning level: ${thinkingLevelLabel(thinkingLevel)}`}
              title={`Thinking · ${thinkingLevelLabel(thinkingLevel)}`}
              className={cn(
                thinkingShowsReasoning(thinkingLevel) && "bg-primary-soft text-primary hover:bg-primary-soft",
              )}
            >
              <IconBrain width={16} height={16} />
            </Button>
            {levelMenuOpen && (
              <>
                <button
                  type="button"
                  aria-hidden="true"
                  tabIndex={-1}
                  onClick={() => setLevelMenuOpen(false)}
                  className="fixed inset-0 z-40 cursor-default"
                />
                <div
                  role="menu"
                  aria-label="Reasoning level"
                  className="absolute bottom-full left-0 z-50 mb-2 w-40 overflow-hidden rounded-md border border-border bg-surface shadow-pop"
                >
                  <p className="border-b border-border px-2.5 py-1.5 text-[10px] font-semibold uppercase tracking-wider text-subtle-foreground">
                    Thinking
                  </p>
                  {THINKING_LEVELS.map((level) => {
                    const active = level === thinkingLevel;
                    return (
                      <button
                        key={level}
                        type="button"
                        role="menuitemradio"
                        aria-checked={active}
                        onClick={() => {
                          setThinkingLevel(level);
                          setLevelMenuOpen(false);
                        }}
                        className={cn(
                          "flex w-full items-center justify-between px-2.5 py-1.5 text-left text-xs transition-colors hover:bg-surface-2",
                          active ? "font-medium text-foreground" : "text-muted-foreground",
                        )}
                      >
                        <span>{thinkingLevelLabel(level)}</span>
                        {active && <span className="size-1.5 rounded-full bg-primary" />}
                      </button>
                    );
                  })}
                </div>
              </>
            )}
          </div>
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
