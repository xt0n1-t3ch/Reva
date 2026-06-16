export type TourRoute = "/" | "/export" | "first-review";

export interface TourStep {
  id: string;
  target: string;
  title: string;
  description: string;
  route: TourRoute;
}

export const tourSteps: TourStep[] = [
  {
    id: "upload",
    target: '[data-tour="upload-zone"]',
    title: "Upload a document",
    description: "Drop a PDF, spreadsheet, image, or email here. Reva ingests it locally and adds it to the work queue.",
    route: "/",
  },
  {
    id: "queue",
    target: '[data-tour="queue-row"]',
    title: "Open work from the queue",
    description: "Each row links to a reviewable document with status, confidence, exceptions, and review state at a glance.",
    route: "/",
  },
  {
    id: "review",
    target: '[data-tour="review-split-view"]',
    title: "Review source-cited fields",
    description: "The split-view keeps extracted fields beside the source document. Hover or focus a field to highlight its citation.",
    route: "first-review",
  },
  {
    id: "reconciliation",
    target: '[data-tour="reconciliation-panel"]',
    title: "Compare detected vs expected",
    description: "Reconciliation rows show the detected value, expected value, tolerance, and exception status without hiding the source.",
    route: "first-review",
  },
  {
    id: "export",
    target: '[data-tour="export-panel"]',
    title: "Export clean records",
    description: "Apply a template or download canonical fields as CSV or JSON after review.",
    route: "/export",
  },
];
