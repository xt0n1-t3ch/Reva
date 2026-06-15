import { convertToModelMessages, stepCountIs, streamText, tool, type UIMessage } from "ai";
import { createOllama } from "ollama-ai-provider-v2";
import { z } from "zod";
import { config } from "@/lib/config";
import { api } from "@/lib/api/client";

export const maxDuration = 120;

const ollama = createOllama({ baseURL: config.ollamaBaseUrl });

const SYSTEM_PROMPT = [
  `You are the assistant inside ${config.productName}, a reinsurance bordereaux (BDX) ingestion and reconciliation console.`,
  "Operators ask about uploaded documents: extracted fields, classification, exceptions, reconciliation of stated control totals against line items, and source citations.",
  "Always ground answers in tool results. The deterministic .NET engine is the source of truth — never invent figures, monetary amounts, or document ids.",
  "When you reference a value, name the document and field it came from. If a tool returns nothing, say so plainly instead of guessing.",
  "Be concise and precise. Prefer short, scannable answers for an expert audience.",
].join(" ");

const errorResult = (cause: unknown) => ({
  error: cause instanceof Error ? cause.message : "Tool call failed",
});

const tools = {
  list_documents: tool({
    description: "List ingested documents with status, classified type, overall confidence, and exception count.",
    inputSchema: z.object({}),
    execute: async () => {
      try {
        const documents = await api.listDocuments();
        return {
          count: documents.length,
          documents: documents.map((document) => ({
            id: document.id,
            fileName: document.fileName,
            status: document.status,
            type: document.documentType,
            confidence: Number(document.confidence.toFixed(3)),
            exceptions: document.exceptionCount,
            reviewState: document.reviewState,
          })),
        };
      } catch (cause) {
        return errorResult(cause);
      }
    },
  }),

  get_document: tool({
    description: "Get extracted fields and exceptions for one document by its id.",
    inputSchema: z.object({ documentId: z.string().describe("Document GUID from list_documents") }),
    execute: async ({ documentId }) => {
      try {
        const detail = await api.getDocument(documentId);
        return {
          id: detail.id,
          fileName: detail.fileName,
          type: detail.documentType,
          status: detail.status,
          confidence: Number(detail.confidence.toFixed(3)),
          fields: detail.fields.map((field) => ({
            name: field.name,
            value: field.value,
            confidence: Number(field.confidence.toFixed(3)),
          })),
          exceptions: detail.exceptions.map((issue) => ({
            severity: issue.severity,
            message: issue.message,
            field: issue.fieldName,
          })),
        };
      } catch (cause) {
        return errorResult(cause);
      }
    },
  }),

  reconcile: tool({
    description: "Run reconciliation for a document: compares each stated control total (Detected) against the value computed from line items (Expected).",
    inputSchema: z.object({ documentId: z.string().describe("Document GUID") }),
    execute: async ({ documentId }) => {
      try {
        const checks = await api.getReconciliation(documentId);
        return {
          count: checks.length,
          checks: checks.map((check) => ({
            name: check.name,
            detected: check.detected.value,
            expected: check.expected.value,
            delta: check.delta,
            status: check.status,
            explanation: check.explanation,
          })),
        };
      } catch (cause) {
        return errorResult(cause);
      }
    },
  }),

  explain_field: tool({
    description: "Explain where a single extracted field's value came from, with its source citations (page and quote).",
    inputSchema: z.object({
      documentId: z.string().describe("Document GUID"),
      field: z.string().describe("Canonical field key or label, e.g. GrossPremium"),
    }),
    execute: async ({ documentId, field }) => {
      try {
        const payload = await api.getReviewPayload(documentId);
        const match = payload.fields.find(
          (item) =>
            item.key.toLowerCase() === field.toLowerCase() ||
            item.label.toLowerCase() === field.toLowerCase(),
        );
        if (!match) {
          return { found: false, available: payload.fields.map((item) => item.key) };
        }
        return {
          found: true,
          key: match.key,
          label: match.label,
          value: match.value,
          confidence: Number(match.confidence.toFixed(3)),
          method: match.provenance.method,
          citations: match.provenance.citations.map((citation) => ({
            page: citation.page,
            quote: citation.quote,
            role: citation.role,
          })),
        };
      } catch (cause) {
        return errorResult(cause);
      }
    },
  }),
} as const;

export async function POST(request: Request) {
  const { messages }: { messages: UIMessage[] } = await request.json();
  const modelMessages = await convertToModelMessages(messages);

  const result = streamText({
    model: ollama(config.ollamaModel),
    system: SYSTEM_PROMPT,
    messages: modelMessages,
    tools,
    stopWhen: stepCountIs(config.agentMaxSteps),
    temperature: config.agentTemperature,
  });

  return result.toUIMessageStreamResponse({
    headers: { "x-agui-protocol": "ui-message-stream-v1" },
  });
}
