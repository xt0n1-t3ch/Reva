export interface TextSegment {
  text: string;
  active: boolean;
}

const escapeRegExp = (value: string): string => value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");

// Split text into segments, marking spans that match any of the active values
// (case-insensitive, longest-first so the most specific value wins).
export const highlightSegments = (text: string, values: string[]): TextSegment[] => {
  const terms = values
    .map((value) => value.trim())
    .filter((value) => value.length >= 2)
    .sort((a, b) => b.length - a.length);

  if (terms.length === 0) {
    return [{ text, active: false }];
  }

  const pattern = new RegExp(terms.map(escapeRegExp).join("|"), "gi");
  const segments: TextSegment[] = [];
  let cursor = 0;

  for (let match = pattern.exec(text); match !== null; match = pattern.exec(text)) {
    if (match.index > cursor) {
      segments.push({ text: text.slice(cursor, match.index), active: false });
    }
    segments.push({ text: match[0], active: true });
    cursor = match.index + match[0].length;
    if (match[0].length === 0) {
      pattern.lastIndex += 1;
    }
  }

  if (cursor < text.length) {
    segments.push({ text: text.slice(cursor), active: false });
  }

  return segments;
};
