import { config } from "@/lib/config";
import type {
  AppSettings,
  BdxReviewPayload,
  DocumentDetail,
  DocumentSummary,
  DocumentUploadResult,
  ExportTemplate,
  ExportTemplateDraft,
  InboundSourceStatus,
  ReconciliationCheck,
  ReviewDecision,
  SchemaMapping,
} from "@/lib/api/types";

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly statusText: string,
    readonly body: string,
  ) {
    super(`API ${status} ${statusText}: ${body.slice(0, 200)}`);
    this.name = "ApiError";
  }
}

export const apiUrl = (path: string): string =>
  `${config.apiBaseUrl}${path.startsWith("/") ? path : `/${path}`}`;

const request = async <T>(path: string, init?: RequestInit): Promise<T> => {
  const response = await fetch(apiUrl(path), {
    ...init,
    headers: { Accept: "application/json", ...init?.headers },
    cache: "no-store",
  });
  if (!response.ok) {
    throw new ApiError(response.status, response.statusText, await response.text().catch(() => ""));
  }
  if (response.status === 204) {
    return undefined as T;
  }
  return (await response.json()) as T;
};

export const api = {
  listDocuments: (signal?: AbortSignal) =>
    request<DocumentSummary[]>("/api/documents", { signal }),

  getDocument: (id: string, signal?: AbortSignal) =>
    request<DocumentDetail>(`/api/documents/${id}`, { signal }),

  getReviewPayload: (id: string, signal?: AbortSignal) =>
    request<BdxReviewPayload>(`/api/documents/${id}/review-payload`, { signal }),

  getReconciliation: (id: string, signal?: AbortSignal) =>
    request<ReconciliationCheck[]>(`/api/reconciliation/${id}`, { signal }),

  reviewDocument: (id: string, decision: ReviewDecision) =>
    request<DocumentDetail>(`/api/documents/${id}/review`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(decision),
    }),

  uploadDocument: (file: File) => {
    const form = new FormData();
    form.append("file", file);
    return request<DocumentUploadResult>("/api/documents", { method: "POST", body: form });
  },

  getSettings: (signal?: AbortSignal) => request<AppSettings>("/api/settings", { signal }),

  saveSettings: (settings: AppSettings) =>
    request<AppSettings>("/api/settings", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(settings),
    }),

  listSchemaMappings: (signal?: AbortSignal) =>
    request<SchemaMapping[]>("/api/schema-mappings", { signal }),

  listInboundSources: (signal?: AbortSignal) =>
    request<InboundSourceStatus[]>("/api/inbound-sources", { signal }),

  listTemplates: (signal?: AbortSignal) =>
    request<ExportTemplate[]>("/api/templates", { signal }),

  createTemplate: (draft: ExportTemplateDraft) =>
    request<ExportTemplate>("/api/templates", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(draft),
    }),

  updateTemplate: (id: string, draft: ExportTemplateDraft) =>
    request<ExportTemplate>(`/api/templates/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(draft),
    }),

  duplicateTemplate: (id: string) =>
    request<ExportTemplate>(`/api/templates/${id}/duplicate`, { method: "POST" }),

  deleteTemplate: (id: string) =>
    request<void>(`/api/templates/${id}`, { method: "DELETE" }),

  pageImageUrl: (documentId: string, page: number) =>
    apiUrl(`/api/documents/${documentId}/pages/${page}.png`),

  exportUrl: (documentId: string, format: string, templateId?: string) => {
    const params = new URLSearchParams({ format });
    if (templateId) {
      params.set("templateId", templateId);
    }
    return apiUrl(`/api/documents/${documentId}/export?${params.toString()}`);
  },
};
