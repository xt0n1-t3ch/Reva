export type DocumentStatus = "Uploaded" | "Parsed" | "Extracted" | "Unsupported" | "Failed";
export type ReviewState = "Pending" | "Approved" | "Rejected" | "NeedsCorrection";
export type ReinsuranceDocumentType =
  | "Unknown"
  | "Treaty"
  | "FacultativeSlip"
  | "Bordereau"
  | "StatementOfAccount"
  | "LossRun"
  | "Endorsement"
  | "ClaimNotice";
export type ExceptionSeverity = "Info" | "Warning" | "Critical";
export type ExportFormat = "Csv" | "Excel" | "Json";
export type AppTheme = "Light" | "Dark" | "System";

export interface DocumentUploadResult {
  id: string;
  fileName: string;
  sha256Hash: string;
  status: DocumentStatus;
  createdAt: string;
}

export interface DocumentSummary {
  id: string;
  fileName: string;
  status: DocumentStatus;
  reviewState: ReviewState;
  documentType: ReinsuranceDocumentType;
  confidence: number;
  exceptionCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ExtractedField {
  name: string;
  value: string;
  confidence: number;
  source: string;
  isCorrected: boolean;
}

export interface ExtractedTable {
  name: string;
  headers: string[];
  rows: Record<string, string>[];
}

export interface SchemaMapping {
  senderKey: string;
  sourceHeader: string;
  canonicalField: string;
  normalizedValue: string;
  confidence: number;
  source: string;
  isLearned: boolean;
  isCorrected: boolean;
}

export interface ExtractionIssue {
  severity: ExceptionSeverity;
  message: string;
  fieldName: string | null;
  detected: string | null;
  expected: string | null;
  confidence: number;
  isReconciliation: boolean;
}

export interface DocumentDetail {
  id: string;
  fileName: string;
  sha256Hash: string;
  status: DocumentStatus;
  reviewState: ReviewState;
  documentType: ReinsuranceDocumentType;
  confidence: number;
  parsedMarkdown: string;
  parserProfile: string;
  fields: ExtractedField[];
  tables: ExtractedTable[];
  exceptions: ExtractionIssue[];
  schemaMappings: SchemaMapping[];
  createdAt: string;
  updatedAt: string;
}

export interface FieldCorrection {
  name: string;
  value: string;
}

export interface SchemaMappingCorrection {
  sourceHeader: string;
  canonicalField: string;
}

export interface ReviewDecision {
  decision: string;
  reviewer: string;
  notes: string | null;
  fieldCorrections: FieldCorrection[];
  mappingCorrections: SchemaMappingCorrection[];
}

export interface SourceBox {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface SourcePoint {
  x: number;
  y: number;
}

export interface SourceSpan {
  id: string;
  documentId: string;
  page: number;
  pageWidth: number;
  pageHeight: number;
  rotation: number;
  bbox: SourceBox;
  polygon: SourcePoint[] | null;
  text: string;
  ocrConfidence: number | null;
  blockId: string | null;
  tableId: string | null;
  rowIndex: number | null;
  columnIndex: number | null;
}

export interface Citation {
  sourceSpanId: string;
  page: number;
  bbox: SourceBox;
  quote: string | null;
  role: string;
}

export interface FieldProvenance {
  method: string;
  stepId: string;
  model: string | null;
  promptVersion: string | null;
  citations: Citation[];
}

export interface FieldValue {
  key: string;
  label: string;
  value: string;
  rawText: string | null;
  status: string;
  confidence: number;
  provenance: FieldProvenance;
}

export interface ReconciliationCheck {
  id: string;
  name: string;
  expected: FieldValue;
  detected: FieldValue;
  delta: number;
  tolerance: number;
  status: string;
  explanation: string;
  citations: Citation[];
}

export interface BdxPage {
  page: number;
  imageUrl: string;
  width: number;
  height: number;
  rotation: number;
}

export interface BdxDocument {
  id: string;
  filename: string;
  pages: BdxPage[];
}

export interface LineItemValue {
  id: string;
  rowNumber: number;
  fields: FieldValue[];
  rowCitationIds: string[];
}

export interface BdxReviewPayload {
  document: BdxDocument;
  sourceSpans: SourceSpan[];
  fields: FieldValue[];
  lineItems: LineItemValue[];
  reconciliation: ReconciliationCheck[];
}

export interface InboundSourceStatus {
  name: string;
  enabled: boolean;
  detail: string;
}

export interface AppSettings {
  theme: AppTheme;
  accentColor: string;
  productName: string;
  confidenceLowMax: number;
  confidenceMediumMax: number;
  defaultTemplateId: string | null;
}

export interface ExportColumn {
  header: string;
  source: string;
}

export interface ExportTemplate {
  id: string;
  name: string;
  format: ExportFormat;
  columns: ExportColumn[];
  isBuiltIn: boolean;
}

export interface ExportTemplateDraft {
  name: string;
  format: ExportFormat;
  columns: ExportColumn[];
}

export interface ExportRecord {
  documentId: string;
  documentType: ReinsuranceDocumentType;
  reviewState: ReviewState;
  fields: Record<string, string>;
  exportedAt: string;
}
