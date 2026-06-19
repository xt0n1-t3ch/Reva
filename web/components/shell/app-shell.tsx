"use client";

import { useCallback, useEffect, useState } from "react";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/cn";
import { config } from "@/lib/config";
import { NavList, NavRail } from "@/components/shell/nav-rail";
import { TopBar } from "@/components/shell/top-bar";
import { CommandPalette } from "@/components/shell/command-palette";
import { TourProvider, useTour } from "@/components/onboarding/tour-provider";
import { ChatPanel } from "@/components/chat/chat-panel";
import { IconClose, IconSparkles } from "@/components/ui/icons";

const CHAT_WIDTH_STORAGE_KEY = "reva-chat-width";
const CHAT_MIN_WIDTH = 360;
const CHAT_MAX_WIDTH = 720;
const CHAT_DEFAULT_WIDTH = 384;

const clampChatWidth = (value: number): number =>
  Math.min(CHAT_MAX_WIDTH, Math.max(CHAT_MIN_WIDTH, Math.round(value)));

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

const readStoredWidth = (): number => {
  try {
    const raw = localStorage.getItem(CHAT_WIDTH_STORAGE_KEY);
    const parsed = raw == null ? NaN : Number(raw);
    return Number.isFinite(parsed) ? clampChatWidth(parsed) : CHAT_DEFAULT_WIDTH;
  } catch {
    return CHAT_DEFAULT_WIDTH;
  }
};

function AppShellContent({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const [navOpen, setNavOpen] = useState(false);
  const [chatOpen, setChatOpen] = useState(false);
  const [chatMinimized, setChatMinimized] = useState(false);
  const [chatStorageLoaded, setChatStorageLoaded] = useState(false);
  const [chatWidth, setChatWidth] = useState(CHAT_DEFAULT_WIDTH);
  const [resizing, setResizing] = useState(false);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const { startTour } = useTour();

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setChatOpen(readStoredFlag(config.chatOpenStorageKey));
      setChatMinimized(readStoredFlag(config.chatMinimizedStorageKey));
      setChatWidth(readStoredWidth());
      setChatStorageLoaded(true);
    });
    return () => window.clearTimeout(timer);
  }, []);

  useEffect(() => {
    if (!chatStorageLoaded) return;
    try {
      localStorage.setItem(CHAT_WIDTH_STORAGE_KEY, String(chatWidth));
    } catch {
      // Ignore persistence failures; width still applies for this session.
    }
  }, [chatWidth, chatStorageLoaded]);

  useEffect(() => {
    if (!resizing) return;
    const previousUserSelect = document.body.style.userSelect;
    const previousCursor = document.body.style.cursor;
    document.body.style.userSelect = "none";
    document.body.style.cursor = "col-resize";
    return () => {
      document.body.style.userSelect = previousUserSelect;
      document.body.style.cursor = previousCursor;
    };
  }, [resizing]);

  const startResize = useCallback((event: React.PointerEvent<HTMLDivElement>) => {
    event.preventDefault();
    const startX = event.clientX;
    let startWidth = CHAT_DEFAULT_WIDTH;
    setChatWidth((current) => {
      startWidth = current;
      return current;
    });
    setResizing(true);

    const onMove = (move: PointerEvent) => {
      // Panel is anchored to the right edge, so dragging left widens it.
      setChatWidth(clampChatWidth(startWidth + (startX - move.clientX)));
    };
    const onUp = () => {
      setResizing(false);
      window.removeEventListener("pointermove", onMove);
      window.removeEventListener("pointerup", onUp);
    };
    window.addEventListener("pointermove", onMove);
    window.addEventListener("pointerup", onUp);
  }, []);

  const onResizeKey = useCallback((event: React.KeyboardEvent<HTMLDivElement>) => {
    const step = event.shiftKey ? 48 : 16;
    if (event.key === "ArrowLeft") {
      event.preventDefault();
      setChatWidth((current) => clampChatWidth(current + step));
    } else if (event.key === "ArrowRight") {
      event.preventDefault();
      setChatWidth((current) => clampChatWidth(current - step));
    }
  }, []);

  useEffect(() => {
    if (!chatStorageLoaded) return;
    void writeStoredFlag(config.chatOpenStorageKey, chatOpen);
  }, [chatOpen, chatStorageLoaded]);

  useEffect(() => {
    if (!chatStorageLoaded) return;
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
    <div className="flex h-[100dvh] flex-col overflow-hidden bg-background">
      <TopBar
        onOpenNav={() => setNavOpen(true)}
        onToggleChat={toggleChat}
        chatOpen={chatOpen}
        onStartTour={startTour}
        onOpenPalette={() => setPaletteOpen(true)}
      />

      <div className="flex min-h-0 flex-1">
        <NavRail />

        {navOpen && (
          <div className="fixed inset-0 z-50 md:hidden">
            <button
              aria-label="Close navigation"
              onClick={() => setNavOpen(false)}
              className="absolute inset-0 bg-black/50"
            />
            <div className="absolute inset-y-0 left-0 flex w-64 flex-col border-r border-border bg-background animate-rise">
              <div className="flex h-12 items-center justify-between border-b border-border px-3">
                <span className="text-sm font-semibold tracking-tight">Reva</span>
                <button
                  aria-label="Close navigation"
                  onClick={() => setNavOpen(false)}
                  className="grid size-8 place-items-center rounded-md text-muted-foreground hover:bg-surface-2"
                >
                  <IconClose width={16} height={16} />
                </button>
              </div>
              <div className="flex-1 overflow-y-auto p-3">
                <NavList pathname={pathname} onNavigate={() => setNavOpen(false)} />
              </div>
            </div>
          </div>
        )}

        <main className="min-w-0 flex-1 overflow-y-auto bg-dotgrid">{children}</main>

        {chatOpen && (
          <button
            aria-label="Close assistant"
            onClick={closeChat}
            className="fixed inset-0 z-30 bg-black/40 lg:hidden"
          />
        )}

        <div
          className={cn(
            "group/chat relative z-40 flex shrink-0 flex-col border-border bg-background",
            !resizing && "transition-[transform,width] duration-200",
            "fixed inset-y-0 right-0 w-full max-w-md border-l shadow-pop",
            chatOpen ? "translate-x-0" : "translate-x-full",
            // Desktop: dock as a static column in the app-shell row and cap the
            // width so the main content reflows to the left (never overlaps).
            "lg:static lg:max-w-none lg:translate-x-0 lg:shadow-none",
            chatOpen
              ? chatMinimized
                ? "lg:w-12 lg:border-l"
                : "lg:w-[var(--chat-w,384px)] lg:border-l"
              : "lg:w-0 lg:overflow-hidden lg:border-l-0",
          )}
          style={
            chatOpen && !chatMinimized
              ? ({ "--chat-w": `${chatWidth}px` } as React.CSSProperties)
              : undefined
          }
          aria-hidden={!chatOpen}
          inert={!chatOpen}
        >
          {chatOpen && !chatMinimized && (
            <div
              role="separator"
              aria-orientation="vertical"
              aria-label="Resize assistant panel"
              aria-valuenow={chatWidth}
              aria-valuemin={CHAT_MIN_WIDTH}
              aria-valuemax={CHAT_MAX_WIDTH}
              tabIndex={0}
              onPointerDown={startResize}
              onKeyDown={onResizeKey}
              className="absolute inset-y-0 left-0 z-50 hidden w-1.5 -translate-x-1/2 cursor-col-resize touch-none lg:block focus-visible:outline-none"
            >
              <span
                className={cn(
                  "absolute inset-y-0 left-1/2 w-px -translate-x-1/2 bg-border transition-colors",
                  "group-hover/chat:bg-border-strong",
                  resizing && "bg-primary",
                )}
              />
            </div>
          )}
          {chatMinimized && (
            <button
              type="button"
              aria-label="Expand assistant"
              onClick={openChat}
              className="hidden h-full w-12 flex-col items-center gap-3 py-4 text-muted-foreground transition-colors hover:bg-surface-2 hover:text-foreground lg:flex"
            >
              <span className="grid size-8 place-items-center rounded-md bg-primary-soft text-primary">
                <IconSparkles width={15} height={15} />
              </span>
              <span className="[writing-mode:vertical-rl] rotate-180 text-[11px] font-semibold uppercase tracking-wider">
                Assistant
              </span>
            </button>
          )}
          <div className={cn("flex h-full w-full flex-col", chatMinimized && "lg:hidden")}>
            <div className="flex items-center justify-end px-2 pt-2 lg:hidden">
              <button
                aria-label="Close assistant"
                onClick={closeChat}
                className="grid size-8 place-items-center rounded-md text-muted-foreground hover:bg-surface-2"
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

      <CommandPalette open={paletteOpen} onOpenChange={setPaletteOpen} onToggleChat={toggleChat} />
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
