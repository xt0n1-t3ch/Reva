"use client";

import { useCallback, useEffect, useState } from "react";

/**
 * Persist a small piece of UI state to localStorage. SSR-safe: the first render
 * returns the supplied fallback, then the stored value is hydrated in an effect
 * so server and client markup agree.
 */
export function useLocalStorageState<T>(
  key: string,
  fallback: T,
  parse: (raw: string) => T,
  serialize: (value: T) => string,
): [T, (value: T | ((current: T) => T)) => void] {
  const [value, setValue] = useState<T>(fallback);

  useEffect(() => {
    // Defer hydration off the synchronous effect body so the first paint uses
    // the SSR-safe fallback, then the stored value is applied without a
    // cascading-render warning.
    const timer = window.setTimeout(() => {
      try {
        const raw = localStorage.getItem(key);
        if (raw != null) {
          setValue(parse(raw));
        }
      } catch {
        // localStorage unavailable (private mode / quota) — keep the fallback.
      }
    });
    return () => window.clearTimeout(timer);
    // Parsers are defined inline by callers; only re-hydrate when the key moves.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key]);

  const update = useCallback(
    (next: T | ((current: T) => T)) => {
      setValue((current) => {
        const resolved = typeof next === "function" ? (next as (c: T) => T)(current) : next;
        try {
          localStorage.setItem(key, serialize(resolved));
        } catch {
          // Ignore persistence failures; in-memory state still updates.
        }
        return resolved;
      });
    },
    [key, serialize],
  );

  return [value, update];
}

export function useBooleanPreference(
  key: string,
  fallback: boolean,
): [boolean, (value: boolean | ((current: boolean) => boolean)) => void] {
  return useLocalStorageState<boolean>(
    key,
    fallback,
    (raw) => raw === "true",
    (value) => String(value),
  );
}

export function useNumberPreference(
  key: string,
  fallback: number,
  clamp: (value: number) => number,
): [number, (value: number | ((current: number) => number)) => void] {
  return useLocalStorageState<number>(
    key,
    fallback,
    (raw) => {
      const parsed = Number(raw);
      return Number.isFinite(parsed) ? clamp(parsed) : fallback;
    },
    (value) => String(Math.round(value)),
  );
}
