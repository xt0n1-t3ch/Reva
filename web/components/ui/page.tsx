import type { HTMLAttributes, ReactNode } from "react";
import { cn } from "@/lib/cn";

export function PageContainer({
  children,
  className,
  fill = false,
}: {
  children: ReactNode;
  className?: string;
  /** Stretch to the full height of the scroll viewport so short pages don't leave a void. */
  fill?: boolean;
}) {
  return (
    <div
      className={cn(
        "mx-auto w-full max-w-[1320px] px-5 py-6 sm:px-7",
        fill && "flex min-h-full flex-col",
        className,
      )}
    >
      {children}
    </div>
  );
}

export function PageHeader({
  title,
  subtitle,
  actions,
  className,
}: {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("flex flex-wrap items-start justify-between gap-3 pb-6", className)}>
      <div className="flex min-w-0 flex-col gap-1.5">
        <h2 className="text-[1.75rem] font-semibold leading-none tracking-[-0.02em]">{title}</h2>
        {subtitle && (
          <p className="max-w-2xl text-sm leading-relaxed text-muted-foreground">{subtitle}</p>
        )}
      </div>
      {actions && <div className="flex shrink-0 items-center gap-2">{actions}</div>}
    </div>
  );
}

export function SectionCard({
  title,
  meta,
  children,
  className,
  bodyClassName,
  /** Let the card grow and scroll internally inside a flex-fill PageContainer. */
  fill = false,
  ...rest
}: {
  title?: string;
  meta?: ReactNode;
  children: ReactNode;
  className?: string;
  bodyClassName?: string;
  fill?: boolean;
} & Omit<HTMLAttributes<HTMLElement>, "title">) {
  return (
    <section
      className={cn(
        "group/card overflow-hidden rounded-lg border border-border bg-background transition-colors duration-200 hover:border-border-strong",
        fill && "flex min-h-0 flex-1 flex-col",
        className,
      )}
      {...rest}
    >
      {title && (
        <header className="flex shrink-0 items-center justify-between gap-3 border-b border-border bg-surface-2/30 px-4 py-2.5">
          <h3 className="flex items-center gap-2 text-[13px] font-semibold tracking-tight">
            <span aria-hidden="true" className="h-3.5 w-px bg-border-strong" />
            {title}
          </h3>
          {meta && (
            <div className="flex items-center gap-2 text-[11px] text-subtle-foreground">{meta}</div>
          )}
        </header>
      )}
      <div className={cn(fill && "min-h-0 flex-1 overflow-y-auto", bodyClassName)}>{children}</div>
    </section>
  );
}
