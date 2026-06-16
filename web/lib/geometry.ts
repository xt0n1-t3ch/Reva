import type { SourceBox, SourceSpan } from "@/lib/api/types";

export interface Rect {
  left: number;
  top: number;
  width: number;
  height: number;
}

// Spans may arrive normalized (0..1) or in page-pixel coordinates. Normalize
// defensively against the page dimensions so overlays land regardless of source.
const fraction = (value: number, dimension: number): number => {
  const normalized = dimension > 0 && value > 1.5 ? value / dimension : value;
  return Math.max(0, Math.min(1, normalized));
};

export const boxToRect = (box: SourceBox, pageWidth: number, pageHeight: number): Rect => ({
  left: fraction(box.x, pageWidth),
  top: fraction(box.y, pageHeight),
  width: fraction(box.width, pageWidth),
  height: fraction(box.height, pageHeight),
});

export const spanRect = (span: SourceSpan): Rect =>
  boxToRect(span.bbox, span.pageWidth || 1, span.pageHeight || 1);

export const rectStyle = (rect: Rect): React.CSSProperties => ({
  left: `${rect.left * 100}%`,
  top: `${rect.top * 100}%`,
  width: `${Math.max(rect.width, 0.004) * 100}%`,
  height: `${Math.max(rect.height, 0.004) * 100}%`,
});
