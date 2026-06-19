import {
  forwardRef,
  useId,
  type InputHTMLAttributes,
  type ReactNode,
  type SelectHTMLAttributes,
  type TextareaHTMLAttributes,
} from "react";
import { cn } from "@/lib/cn";
import { IconChevronRight } from "@/components/ui/icons";

const controlBase =
  "w-full rounded-md border border-input bg-surface text-sm text-foreground transition-colors placeholder:text-subtle-foreground hover:border-border-strong focus-visible:border-primary-border focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/40 disabled:cursor-not-allowed disabled:opacity-50";

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(
  function Input({ className, ...rest }, ref) {
    return <input ref={ref} className={cn(controlBase, "h-9 px-3", className)} {...rest} />;
  },
);

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement>>(
  function Textarea({ className, ...rest }, ref) {
    return (
      <textarea ref={ref} className={cn(controlBase, "min-h-[5rem] px-3 py-2 leading-relaxed", className)} {...rest} />
    );
  },
);

export const Select = forwardRef<HTMLSelectElement, SelectHTMLAttributes<HTMLSelectElement>>(
  function Select({ className, children, ...rest }, ref) {
    return (
      <div className="relative">
        <select
          ref={ref}
          className={cn(controlBase, "h-9 appearance-none px-3 pr-9", className)}
          {...rest}
        >
          {children}
        </select>
        <IconChevronRight
          width={14}
          height={14}
          className="pointer-events-none absolute right-2.5 top-1/2 -translate-y-1/2 rotate-90 text-subtle-foreground"
        />
      </div>
    );
  },
);

export function Field({
  label,
  hint,
  htmlFor,
  required,
  children,
  className,
}: {
  label: string;
  hint?: ReactNode;
  htmlFor?: string;
  required?: boolean;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("flex flex-col gap-1.5", className)}>
      <label htmlFor={htmlFor} className="flex items-center gap-1 text-sm font-medium text-foreground">
        {label}
        {required && (
          <span className="text-danger" aria-hidden="true">
            *
          </span>
        )}
      </label>
      {children}
      {hint && <span className="text-[11px] leading-snug text-subtle-foreground">{hint}</span>}
    </div>
  );
}

export function useFieldId(prefix: string): string {
  const id = useId();
  return `${prefix}-${id}`;
}
