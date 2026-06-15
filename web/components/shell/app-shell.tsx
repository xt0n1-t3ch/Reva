"use client";

import { useState } from "react";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/cn";
import { BrandMark, NavLinks, NavRail } from "@/components/shell/nav-rail";
import { TopBar } from "@/components/shell/top-bar";
import { ChatPanel } from "@/components/chat/chat-panel";
import { IconClose } from "@/components/ui/icons";

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const [navOpen, setNavOpen] = useState(false);
  const [chatOpen, setChatOpen] = useState(false);

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
          onToggleChat={() => setChatOpen((value) => !value)}
          chatOpen={chatOpen}
        />
        <main className="flex-1 overflow-y-auto">{children}</main>
      </div>

      {chatOpen && (
        <button
          aria-label="Close assistant"
          onClick={() => setChatOpen(false)}
          className="fixed inset-0 z-30 bg-black/40 backdrop-blur-[2px] lg:hidden"
        />
      )}

      <div
        className={cn(
          "z-40 flex shrink-0 flex-col border-border bg-surface transition-transform duration-200",
          "fixed inset-y-0 right-0 w-full max-w-md border-l shadow-pop",
          chatOpen ? "translate-x-0" : "translate-x-full",
          "lg:static lg:max-w-none lg:translate-x-0 lg:shadow-none",
          chatOpen ? "lg:w-[380px] lg:border-l" : "lg:w-0 lg:overflow-hidden lg:border-l-0",
        )}
        aria-hidden={!chatOpen}
      >
        <div className="flex h-full w-full flex-col lg:w-[380px]">
          <div className="flex items-center justify-end px-2 pt-2 lg:hidden">
            <button
              aria-label="Close assistant"
              onClick={() => setChatOpen(false)}
              className="inline-flex size-8 items-center justify-center rounded-md text-muted-foreground hover:bg-surface-2"
            >
              <IconClose width={16} height={16} />
            </button>
          </div>
          <div className="min-h-0 flex-1">
            <ChatPanel />
          </div>
        </div>
      </div>
    </div>
  );
}
