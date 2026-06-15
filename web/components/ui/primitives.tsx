import type { ButtonHTMLAttributes, HTMLAttributes, ReactNode } from "react";
import { cn } from "@/lib/cn";
import type { ConfidenceTier, Tone } from "@/lib/format";
import { formatPercent } from "@/lib/format";

const toneClasses: Record<Tone, string> = {
  neutral: "bg-surface-2 text-muted-foreground border-border",
  primary: "bg-primary-soft text-primary border-primary-border",
  success: "bg-success-soft text-success border-success/30",
  warning: "bg-warning-soft text-warning-foreground border-warning/40",
  danger: "bg-danger-soft text-danger border-danger/30",
};

export function Badge({
  tone = "neutral",
  className,
  children,
}: {
  tone?: Tone;
  className?: string;
  children: ReactNode;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 text-xs font-medium leading-none whitespace-nowrap",
        toneClasses[tone],
        className,
      )}
    >
      {children}
    </span>
  );
}

export function Dot({ tone = "neutral", className }: { tone?: Tone; className?: string }) {
  const map: Record<Tone, string> = {
    neutral: "bg-muted-foreground",
    primary: "bg-primary",
    success: "bg-success",
    warning: "bg-warning",
    danger: "bg-danger",
  };
  return <span className={cn("size-1.5 rounded-full", map[tone], className)} aria-hidden="true" />;
}

type ButtonVariant = "primary" | "ghost" | "outline" | "subtle" | "danger";
type ButtonSize = "sm" | "md" | "icon";

const variantClasses: Record<ButtonVariant, string> = {
  primary: "bg-primary text-primary-foreground hover:opacity-90 shadow-soft",
  outline: "border border-border-strong bg-surface text-foreground hover:bg-surface-2",
  ghost: "text-muted-foreground hover:bg-surface-2 hover:text-foreground",
  subtle: "bg-surface-2 text-foreground hover:bg-surface-3",
  danger: "bg-danger text-danger-foreground hover:opacity-90",
};

const sizeClasses: Record<ButtonSize, string> = {
  sm: "h-7 px-2.5 text-xs gap-1.5",
  md: "h-9 px-3.5 text-sm gap-2",
  icon: "size-9 justify-center",
};

export function Button({
  variant = "outline",
  size = "md",
  className,
  children,
  ...rest
}: {
  variant?: ButtonVariant;
  size?: ButtonSize;
} & ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      className={cn(
        "inline-flex items-center rounded-md font-medium transition-[background-color,opacity,color] disabled:pointer-events-none disabled:opacity-50",
        variantClasses[variant],
        sizeClasses[size],
        className,
      )}
      {...rest}
    >
      {children}
    </button>
  );
}

export function Card({ className, children, ...rest }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("rounded-lg border border-border bg-surface shadow-soft", className)}
      {...rest}
    >
      {children}
    </div>
  );
}

export function Spinner({ className }: { className?: string }) {
  return (
    <span
      role="status"
      aria-label="Loading"
      className={cn(
        "inline-block size-4 animate-spin rounded-full border-2 border-current border-t-transparent",
        className,
      )}
    />
  );
}

const tierTone: Record<ConfidenceTier, Tone> = {
  high: "success",
  medium: "warning",
  low: "danger",
};

const tierColor: Record<ConfidenceTier, string> = {
  high: "bg-confidence-high",
  medium: "bg-confidence-medium",
  low: "bg-confidence-low",
};

export function ConfidenceMeter({
  score,
  tier,
  showValue = true,
  className,
}: {
  score: number;
  tier: ConfidenceTier;
  showValue?: boolean;
  className?: string;
}) {
  return (
    <span className={cn("inline-flex items-center gap-2", className)}>
      <span
        className="relative h-1.5 w-14 overflow-hidden rounded-full bg-surface-3"
        role="meter"
        aria-valuenow={Math.round(score * 100)}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={`Confidence ${formatPercent(score)}`}
      >
        <span
          className={cn("absolute inset-y-0 left-0 rounded-full", tierColor[tier])}
          style={{ width: `${Math.max(4, Math.min(100, score * 100))}%` }}
        />
      </span>
      {showValue && (
        <span className="font-mono text-xs tabular text-muted-foreground">{formatPercent(score)}</span>
      )}
    </span>
  );
}

export { tierTone };
