const trimTrailingSlash = (value: string): string => value.replace(/\/+$/, "");

export const config = {
  apiBaseUrl: trimTrailingSlash(process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5187"),
  agentMaxSteps: 6,
  agentTemperature: 0,
  agentNumCtx: 16384,
  themeStorageKey: "reva-theme",
  onboardingStorageKey: "reva-onboarding-done",
  chatOpenStorageKey: "reva-chat-open",
  chatMinimizedStorageKey: "reva-chat-minimized",
  chatMaxImageAttachments: 4,
  chatMaxDocumentAttachments: 4,
  chatMaxImageBytes: 10 * 1024 * 1024,
  chatMaxDocumentBytes: 25 * 1024 * 1024,
  chatImageAccept: "image/*",
  chatDocumentAccept: ".pdf,.csv,.xlsx,.eml,.msg",
  chatDocumentExtensions: [".pdf", ".csv", ".xlsx", ".eml", ".msg"],
  productName: "Reva",
} as const;

export const confidenceThresholds = {
  lowMax: 0.6,
  mediumMax: 0.85,
} as const;
