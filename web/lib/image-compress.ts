/**
 * Client-side image compression for chat attachments. The /api/agent request
 * inlines images as base64, and the backend rejects requests over ~1,000,000
 * bytes. Base64 inflates bytes ~4/3, so we downscale + re-encode each image to
 * a small target before it ever reaches the wire — the raw "request too large"
 * error should never surface to the user.
 */

/** Hard request ceiling enforced by the backend, in raw bytes. */
export const REQUEST_BYTE_LIMIT = 1_000_000;

/**
 * Per-image raw-byte target. Several images share the request budget, and
 * base64 inflates by ~4/3, so we keep each image well under a third of the
 * limit. Tunable down further when multiple images are attached.
 */
export const DEFAULT_IMAGE_TARGET_BYTES = 600_000;

/** base64 inflates payload by 4/3; estimate the wire cost of raw bytes. */
export const base64Size = (bytes: number): number => Math.ceil(bytes / 3) * 4;

export type CompressionResult =
  | { ok: true; file: File; originalBytes: number; compressedBytes: number; changed: boolean }
  | { ok: false; reason: string; originalBytes: number };

const QUALITY_STEPS = [0.82, 0.72, 0.62, 0.52, 0.42];
const MAX_DIMS = [1280, 1024, 896, 768, 640, 512];

const canUseCanvasPipeline = (): boolean =>
  typeof window !== "undefined" &&
  typeof createImageBitmap === "function" &&
  typeof document !== "undefined";

const drawToBlob = async (
  bitmap: ImageBitmap,
  maxDim: number,
  quality: number,
  mimeType: string,
): Promise<Blob | null> => {
  const longest = Math.max(bitmap.width, bitmap.height);
  const scale = longest > maxDim ? maxDim / longest : 1;
  const width = Math.max(1, Math.round(bitmap.width * scale));
  const height = Math.max(1, Math.round(bitmap.height * scale));

  const canvas = document.createElement("canvas");
  canvas.width = width;
  canvas.height = height;
  const ctx = canvas.getContext("2d");
  if (!ctx) {
    return null;
  }
  // White matte so transparency doesn't turn black when flattening to JPEG.
  ctx.fillStyle = "#ffffff";
  ctx.fillRect(0, 0, width, height);
  ctx.drawImage(bitmap, 0, 0, width, height);

  return await new Promise<Blob | null>((resolve) => {
    canvas.toBlob((blob) => resolve(blob), mimeType, quality);
  });
};

/**
 * Compress one image file to at most `targetBytes`, iterating quality then
 * dimension. Returns the smallest blob produced even if it still exceeds the
 * target (the caller decides whether the budget fits); ok:false only when the
 * image can't be decoded/encoded at all.
 */
export const compressImage = async (
  file: File,
  targetBytes: number = DEFAULT_IMAGE_TARGET_BYTES,
): Promise<CompressionResult> => {
  const originalBytes = file.size;

  // Small enough already, and a format the backend handles — leave it.
  if (originalBytes <= targetBytes) {
    return { ok: true, file, originalBytes, compressedBytes: originalBytes, changed: false };
  }

  if (!canUseCanvasPipeline()) {
    return { ok: false, reason: "compression-unavailable", originalBytes };
  }

  let bitmap: ImageBitmap;
  try {
    bitmap = await createImageBitmap(file);
  } catch {
    return { ok: false, reason: "decode-failed", originalBytes };
  }

  // Prefer WebP when supported (smaller), else JPEG.
  const mimeType = "image/webp";
  const extension = ".webp";

  let best: Blob | null = null;
  try {
    for (const maxDim of MAX_DIMS) {
      for (const quality of QUALITY_STEPS) {
        const blob = await drawToBlob(bitmap, maxDim, quality, mimeType);
        if (!blob) {
          continue;
        }
        if (!best || blob.size < best.size) {
          best = blob;
        }
        if (blob.size <= targetBytes) {
          best = blob;
          break;
        }
      }
      if (best && best.size <= targetBytes) {
        break;
      }
    }
  } finally {
    bitmap.close();
  }

  if (!best) {
    return { ok: false, reason: "encode-failed", originalBytes };
  }

  const baseName = file.name.replace(/\.[^.]+$/, "");
  const compressed = new File([best], `${baseName}${extension}`, {
    type: best.type || mimeType,
    lastModified: Date.now(),
  });

  return {
    ok: true,
    file: compressed,
    originalBytes,
    compressedBytes: compressed.size,
    changed: true,
  };
};

/** Compact size label for chips, e.g. "3.2 MB", "480 KB". */
export const formatBytes = (bytes: number): string => {
  if (bytes < 1024) {
    return `${bytes} B`;
  }
  if (bytes < 1024 * 1024) {
    return `${Math.round(bytes / 1024)} KB`;
  }
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
};
