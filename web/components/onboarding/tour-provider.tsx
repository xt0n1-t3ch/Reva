"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { config } from "@/lib/config";
import { CoachMark } from "@/components/onboarding/coach-mark";
import { tourSteps, type TourStep } from "@/components/onboarding/steps";

interface TourContextValue {
  startTour: () => void;
}

const TourContext = createContext<TourContextValue | null>(null);

const sameRoute = (pathname: string, target: string) =>
  target === "/" ? pathname === "/" : pathname.startsWith(target);

export function useTour() {
  const value = useContext(TourContext);
  if (!value) {
    throw new Error("useTour must be used inside TourProvider");
  }
  return value;
}

export function TourProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [active, setActive] = useState(false);
  const [stepIndex, setStepIndex] = useState(0);
  const [targetRect, setTargetRect] = useState<DOMRect | null>(null);
  const step = active ? tourSteps[stepIndex] : null;

  const completeTour = useCallback(() => {
    window.localStorage.setItem(config.onboardingStorageKey, "true");
    setActive(false);
    setTargetRect(null);
  }, []);

  const startTour = useCallback(() => {
    setStepIndex(0);
    setActive(true);
  }, []);

  const goNext = useCallback(() => {
    setStepIndex((current) => {
      if (current >= tourSteps.length - 1) {
        completeTour();
        return current;
      }
      return current + 1;
    });
  }, [completeTour]);

  const goBack = useCallback(() => {
    setStepIndex((current) => Math.max(0, current - 1));
  }, []);

  const resolveRoute = useCallback((currentStep: TourStep) => {
    if (currentStep.route !== "first-review") {
      return currentStep.route;
    }

    if (pathname.startsWith("/review") && document.querySelector(currentStep.target)) {
      return pathname;
    }

    const queueRow = document.querySelector<HTMLAnchorElement>('[data-tour="queue-row"]');
    return queueRow?.getAttribute("href") ?? "/review";
  }, [pathname]);

  useEffect(() => {
    if (window.localStorage.getItem(config.onboardingStorageKey) === "true") {
      return;
    }

    const timer = window.setTimeout(() => setActive(true), 0);
    return () => window.clearTimeout(timer);
  }, []);

  useEffect(() => {
    if (!active || !step) {
      return;
    }

    const targetRoute = resolveRoute(step);
    if (!sameRoute(pathname, targetRoute)) {
      router.push(targetRoute);
    }
  }, [active, pathname, resolveRoute, router, step]);

  useEffect(() => {
    if (!active || !step) {
      return;
    }

    let frame = 0;
    let skipTimer = 0;
    let targetWasCentered = false;

    const updateRect = () => {
      const target = document.querySelector<HTMLElement>(step.target);
      if (target) {
        if (!targetWasCentered) {
          target.scrollIntoView({ block: "center", inline: "nearest" });
          targetWasCentered = true;
        }
        setTargetRect(target.getBoundingClientRect());
        window.clearTimeout(skipTimer);
      } else {
        setTargetRect(null);
        window.clearTimeout(skipTimer);
        skipTimer = window.setTimeout(goNext, 800);
      }
    };

    frame = window.requestAnimationFrame(updateRect);
    window.addEventListener("resize", updateRect);
    window.addEventListener("scroll", updateRect, true);

    return () => {
      window.cancelAnimationFrame(frame);
      window.clearTimeout(skipTimer);
      window.removeEventListener("resize", updateRect);
      window.removeEventListener("scroll", updateRect, true);
    };
  }, [active, goNext, pathname, step]);

  useEffect(() => {
    if (!active) {
      return;
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        completeTour();
      } else if (event.key === "ArrowRight" || event.key === "Enter") {
        event.preventDefault();
        goNext();
      } else if (event.key === "ArrowLeft") {
        event.preventDefault();
        goBack();
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [active, completeTour, goBack, goNext]);

  const value = useMemo(() => ({ startTour }), [startTour]);

  return (
    <TourContext.Provider value={value}>
      {children}
      {active && step && targetRect && (
        <CoachMark
          step={step}
          targetRect={targetRect}
          stepIndex={stepIndex}
          totalSteps={tourSteps.length}
          onBack={goBack}
          onNext={goNext}
          onSkip={completeTour}
        />
      )}
    </TourContext.Provider>
  );
}
