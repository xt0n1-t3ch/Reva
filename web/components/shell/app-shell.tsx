"use client";

import { useEffect, useState } from "react";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/cn";
import { config } from "@/lib/config";
import { BrandMark, NavLinks, NavRail } from "@/components/shell/nav-rail";
import { TopBar } from "@/components/shell/top-bar";
import { TourProvider, useTour } from "@/components/onboarding/tour-provider";
import { ChatPanel } from "@/components/chat/chat-panel";
import { IconClose, IconSparkles } from "@/components/ui/icons";

const readStoredFlag = (key: string): boolean => {
  try {
    return localStorage.getItem(key) === "true";
  } catch {
    return false;
  }
};

const writeStoredFlag = (key: string, value: boolean): boolean => {
  try {
    localStorage.setItem(key, String(value));
    return true;
  } catch {
    return false;
  }
};

function AppShellContent({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const [navOpen, setNavOpen] = useState(false);
  const [chatOpen, setChatOpen] = useState(false);
  const [chatMinimized, setChatMinimized] = useState(false);
  const [chatStorageLoaded, setChatStorageLoaded] = useState(false);
  const { startTour } = useTour();

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setChatOpen(readStoredFlag(config.chatOpenStorageKey));
      setChatMinimized(readStoredFlag(config.chatMinimizedStorageKey));
      setChatStorageLoaded(true);
    });
    return () => window.clearTimeout(timer);
  }, []);

  useEffect(() => {
    if (!chatStorageLoaded) {
      return;
    }
    void writeStoredFlag(config.chatOpenStorageKey, chatOpen);
  }, [chatOpen, chatStorageLoaded]);

  useEffect(() => {
    if (!chatStorageLoaded) {
      return;
    }
    void writeStoredFlag(config.chatMinimizedStorageKey, chatMinimized);
  }, [chatMinimized, chatStorageLoaded]);

  const openChat = () => {
    setChatOpen(true);
    setChatMinimized(false);
  };

  const closeChat = () => {
    setChatOpen(false);
    setChatMinimized(false);
  };

  const toggleChat = () => {
    if (!chatOpen || chatMinimized) {
      openChat();
      return;
    }
    closeChat();
  };

  return (
    <div className="flex h-[100dvh] overflow-hidden bg-background">
      <NavRail />

      {navOpen && (
        <div className="fixed inset-0 z-50 md:hidden">
          <button
            aria-label="Close navigation"
            onClick={() => setNavOpen(false)}
            className="absolute inset-0 bg-black/40 backdrop-blur-[2px]"
          />
          <div className="absolute inset-y-0 left-0 flex w-72 flex-col border-r border-border bg-surface animate-rise">
            <div className="flex h-14 items-center justify-between border-b border-border px-4">
              <BrandMark />
              <button
                aria-label="Close navigation"
                onClick={() => setNavOpen(false)}
                className="inline-flex size-8 items-center justify-center rounded-md text-muted-foreground hover:bg-surface-2"
              >
                <IconClose width={16} height={16} />
              </button>
            </div>
            <div className="flex-1 overflow-y-auto p-3">
              <NavLinks pathname={pathname} onNavigate={() => setNavOpen(false)} />
            </div>
          </div>
        </div>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        <TopBar
          onOpenNav={() => setNavOpen(true)}
          onToggleChat={toggleChat}
          chatOpen={chatOpen}
          onStartTour={startTour}
        />
        <main className="flex-1 overflow-y-auto">{children}</main>
      </div>

      {chatOpen && (
        <button
          aria-label="Close assistant"
          onClick={closeChat}
          className="fixed inset-0 z-30 bg-black/40 backdrop-blur-[2px] lg:hidden"
        />
      )}

      <div
        className={cn(
          "z-40 flex shrink-0 flex-col border-border bg-surface transition-transform duration-200",
          "fixed inset-y-0 right-0 w-full max-w-md border-l shadow-pop",
          chatOpen ? "translate-x-0" : "translate-x-full",
          "lg:static lg:max-w-none lg:translate-x-0 lg:shadow-none",
          chatOpen
            ? chatMinimized
              ? "lg:w-14 lg:border-l"
              : "lg:w-[380px] lg:border-l"
            : "lg:w-0 lg:overflow-hidden lg:border-l-0",
        )}
        aria-hidden={!chatOpen}
        inert={!chatOpen}
      >
        {chatMinimized && (
          <button
            type="button"
            aria-label="Expand assistant"
            onClick={openChat}
            className="hidden h-full w-14 flex-col items-center gap-3 px-2 py-4 text-muted-foreground transition-colors hover:bg-surface-2 hover:text-foreground lg:flex"
          >
            <span className="flex size-9 items-center justify-center rounded-md bg-primary-soft text-primary">
              <IconSparkles width={16} height={16} />
            </span>
            <span className="[writing-mode:vertical-rl] rotate-180 text-xs font-semibold uppercase tracking-wider">
              Assistant
            </span>
          </button>
        )}
        <div className={cn("flex h-full w-full flex-col lg:w-[380px]", chatMinimized && "lg:hidden")}>
          <div className="flex items-center justify-end px-2 pt-2 lg:hidden">
            <button
              aria-label="Close assistant"
              onClick={closeChat}
              className="inline-flex size-8 items-center justify-center rounded-md text-muted-foreground hover:bg-surface-2"
            >
              <IconClose width={16} height={16} />
            </button>
          </div>
          <div className="min-h-0 flex-1">
            <ChatPanel onClose={closeChat} onMinimize={() => setChatMinimized(true)} />
          </div>
        </div>
      </div>
    </div>
  );
}

export function AppShell({ children }: { children: React.ReactNode }) {
  return (
    <TourProvider>
      <AppShellContent>{children}</AppShellContent>
    </TourProvider>
  );
}
