"use client";

import { useEffect, useMemo, useRef, type KeyboardEvent } from "react";
import { cn } from "@/lib/cn";
import type { TourStep } from "@/components/onboarding/steps";
import { Button, Card } from "@/components/ui/primitives";

const clamp = (value: number, min: number, max: number) => Math.min(Math.max(value, min), max);

export function CoachMark({
  step,
  targetRect,
  stepIndex,
  totalSteps,
  onBack,
  onNext,
  onSkip,
}: {
  step: TourStep;
  targetRect: DOMRect;
  stepIndex: number;
  totalSteps: number;
  onBack: () => void;
  onNext: () => void;
  onSkip: () => void;
}) {
  const dialogRef = useRef<HTMLDivElement>(null);
  const titleId = `tour-title-${step.id}`;
  const descriptionId = `tour-description-${step.id}`;
  const isFirst = stepIndex === 0;
  const isLast = stepIndex === totalSteps - 1;

  const position = useMemo(() => {
    if (typeof window === "undefined") {
      return { left: 12, top: 12 };
    }

    const tooltipWidth = Math.min(window.innerWidth - 24, 320);
    const belowSpace = window.innerHeight - targetRect.bottom;
    const top = belowSpace >= 190 ? targetRect.bottom + 12 : Math.max(12, targetRect.top - 190);
    const left = clamp(targetRect.left + targetRect.width / 2 - tooltipWidth / 2, 12, window.innerWidth - tooltipWidth - 12);
    return { left, top };
  }, [targetRect]);

  useEffect(() => {
    const dialog = dialogRef.current;
    const focusTarget = dialog?.querySelector<HTMLButtonElement>("button:not([disabled])");
    focusTarget?.focus();
  }, [step.id]);

  const trapFocus = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key !== "Tab") {
      return;
    }

    const dialog = dialogRef.current;
    const focusables = Array.from(
      dialog?.querySelectorAll<HTMLElement>('button:not([disabled]), [href], input, select, textarea, [tabindex]:not([tabindex="-1"])') ?? [],
    );

    if (focusables.length === 0) {
      return;
    }

    const first = focusables[0];
    const last = focusables[focusables.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  };

  return (
    <div className="fixed inset-0 z-[80] pointer-events-none" aria-live="polite">
      <div className="absolute inset-0 bg-foreground/35" />
      <div
        aria-hidden="true"
        className="absolute rounded-lg border-2 border-primary bg-transparent shadow-pop transition-all duration-150"
        style={{
          left: targetRect.left - 6,
          top: targetRect.top - 6,
          width: targetRect.width + 12,
          height: targetRect.height + 12,
        }}
      />
      <Card
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={descriptionId}
        onKeyDown={trapFocus}
        className={cn("pointer-events-auto absolute w-[min(calc(100vw-1.5rem),20rem)] p-4 animate-rise")}
        style={position}
      >
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <p className="text-[11px] font-semibold uppercase tracking-wider text-subtle-foreground">
              Step {stepIndex + 1} of {totalSteps}
            </p>
            <h2 id={titleId} className="mt-1 text-base font-semibold tracking-tight">
              {step.title}
            </h2>
          </div>
          <Button type="button" variant="ghost" size="sm" onClick={onSkip}>
            Skip
          </Button>
        </div>
        <p id={descriptionId} className="mt-2 text-sm leading-relaxed text-muted-foreground">
          {step.description}
        </p>
        <div className="mt-4 flex items-center justify-between gap-2">
          <Button type="button" variant="outline" size="sm" onClick={onBack} disabled={isFirst}>
            Back
          </Button>
          <Button type="button" variant="primary" size="sm" onClick={onNext}>
            {isLast ? "Finish" : "Next"}
          </Button>
        </div>
      </Card>
    </div>
  );
}
