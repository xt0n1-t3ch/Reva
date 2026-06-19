"use client";

import { useEffect, useState } from "react";
import { useTheme } from "@/components/shell/theme-provider";

/**
 * Recharts colors are resolved in JS at render time, not via Tailwind classes,
 * so we read the live values of the design-system CSS variables off the document
 * root and hand recharts concrete colors. Re-reading on every theme flip keeps
 * the charts byte-faithful to the tokens in both dark and light without ever
 * hardcoding a hex.
 */
export interface ChartTheme {
  accent: string;
  accentSoft: string;
  foreground: string;
  mutedForeground: string;
  subtleForeground: string;
  border: string;
  borderStrong: string;
  surface: string;
  surface2: string;
  success: string;
  warning: string;
  danger: string;
  grid: string;
}

const readVar = (style: CSSStyleDeclaration, name: string, fallback: string): string => {
  const value = style.getPropertyValue(name).trim();
  return value.length > 0 ? value : fallback;
};

const resolveTheme = (): ChartTheme => {
  if (typeof window === "undefined") {
    // Server render: opaque placeholders; the client effect immediately overwrites.
    return {
      accent: "#3b6ef5",
      accentSoft: "rgba(59,110,245,0.12)",
      foreground: "#111",
      mutedForeground: "#777",
      subtleForeground: "#999",
      border: "#e5e5e5",
      borderStrong: "#cfcfcf",
      surface: "#fff",
      surface2: "#fafafa",
      success: "#1f9d55",
      warning: "#c98a1e",
      danger: "#d23b3b",
      grid: "#ededed",
    };
  }

  const style = getComputedStyle(document.documentElement);
  const accent = readVar(style, "--accent", "#3b6ef5");
  return {
    accent,
    accentSoft: `color-mix(in oklab, ${accent} 16%, transparent)`,
    foreground: readVar(style, "--foreground", "#111"),
    mutedForeground: readVar(style, "--muted-foreground", "#777"),
    subtleForeground: readVar(style, "--subtle-foreground", "#999"),
    border: readVar(style, "--border", "#e5e5e5"),
    borderStrong: readVar(style, "--border-strong", "#cfcfcf"),
    surface: readVar(style, "--surface", "#fff"),
    surface2: readVar(style, "--surface-2", "#fafafa"),
    success: readVar(style, "--success", "#1f9d55"),
    warning: readVar(style, "--warning", "#c98a1e"),
    danger: readVar(style, "--danger", "#d23b3b"),
    grid: `color-mix(in oklab, ${readVar(style, "--border", "#e5e5e5")} 70%, transparent)`,
  };
};

export function useChartTheme(): ChartTheme {
  const { resolved } = useTheme();
  const [theme, setTheme] = useState<ChartTheme>(resolveTheme);

  useEffect(() => {
    // Resolved flips when the .dark class toggles; re-read the live token values.
    setTheme(resolveTheme());
  }, [resolved]);

  return theme;
}
