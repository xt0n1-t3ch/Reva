"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { config } from "@/lib/config";

export type ThemeMode = "light" | "dark" | "system";
type Resolved = "light" | "dark";

interface ThemeContextValue {
  mode: ThemeMode;
  resolved: Resolved;
  setMode: (mode: ThemeMode) => void;
}

interface ThemeState {
  mode: ThemeMode;
  resolved: Resolved;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

const systemPrefersDark = () =>
  typeof window !== "undefined" && window.matchMedia("(prefers-color-scheme: dark)").matches;

const applyMode = (mode: ThemeMode): Resolved => {
  const dark = mode === "dark" || (mode === "system" && systemPrefersDark());
  document.documentElement.classList.toggle("dark", dark);
  return dark ? "dark" : "light";
};

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<ThemeState>({ mode: "system", resolved: "light" });

  useEffect(() => {
    const stored = (localStorage.getItem(config.themeStorageKey) as ThemeMode | null) ?? "system";
    // eslint-disable-next-line react-hooks/set-state-in-effect -- one-time read of the persisted theme from localStorage
    setState({ mode: stored, resolved: applyMode(stored) });
  }, []);

  useEffect(() => {
    if (state.mode !== "system") {
      return;
    }
    const media = window.matchMedia("(prefers-color-scheme: dark)");
    const handler = () => setState({ mode: "system", resolved: applyMode("system") });
    media.addEventListener("change", handler);
    return () => media.removeEventListener("change", handler);
  }, [state.mode]);

  const setMode = useCallback((next: ThemeMode) => {
    localStorage.setItem(config.themeStorageKey, next);
    setState({ mode: next, resolved: applyMode(next) });
  }, []);

  const value = useMemo<ThemeContextValue>(
    () => ({ mode: state.mode, resolved: state.resolved, setMode }),
    [state.mode, state.resolved, setMode],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export const useTheme = (): ThemeContextValue => {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error("useTheme must be used within ThemeProvider");
  }
  return context;
};
