"use client";

import { cn } from "@/lib/cn";
import { IconMonitor, IconMoon, IconSun } from "@/components/ui/icons";
import { type ThemeMode, useTheme } from "@/components/shell/theme-provider";

const options: Array<{ mode: ThemeMode; label: string; Icon: typeof IconSun }> = [
  { mode: "light", label: "Light", Icon: IconSun },
  { mode: "system", label: "System", Icon: IconMonitor },
  { mode: "dark", label: "Dark", Icon: IconMoon },
];

export function ThemeToggle() {
  const { mode, setMode } = useTheme();
  return (
    <div
      role="radiogroup"
      aria-label="Color theme"
      className="inline-flex items-center gap-0.5 rounded-md border border-border bg-surface-2 p-0.5"
    >
      {options.map(({ mode: value, label, Icon }) => (
        <button
          key={value}
          role="radio"
          aria-checked={mode === value}
          aria-label={label}
          title={label}
          onClick={() => setMode(value)}
          className={cn(
            "inline-flex size-7 items-center justify-center rounded transition-colors",
            mode === value
              ? "bg-surface text-foreground shadow-soft"
              : "text-muted-foreground hover:text-foreground",
          )}
        >
          <Icon width={15} height={15} />
        </button>
      ))}
    </div>
  );
}
