"use client";

import { usePathname } from "next/navigation";
import { cn } from "@/lib/cn";
import { navItems } from "@/components/shell/nav";
import { BrandMark } from "@/components/shell/nav-rail";
import { ThemeToggle } from "@/components/shell/theme-toggle";
import { InboundStatus } from "@/components/shell/inbound-status";
import { Button } from "@/components/ui/primitives";
import { IconHelp, IconSparkles } from "@/components/ui/icons";

const MenuIcon = () => (
  <svg width={18} height={18} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.75} strokeLinecap="round" aria-hidden="true">
    <path d="M3 6h18M3 12h18M3 18h18" />
  </svg>
);

const activeItem = (pathname: string) =>
  navItems.find((item) => (item.href === "/" ? pathname === "/" : pathname.startsWith(item.href))) ??
  navItems[0];

export function TopBar({
  onOpenNav,
  onToggleChat,
  chatOpen,
  onStartTour,
}: {
  onOpenNav: () => void;
  onToggleChat: () => void;
  chatOpen: boolean;
  onStartTour: () => void;
}) {
  const pathname = usePathname();
  const current = activeItem(pathname);

  return (
    <header className="flex h-14 shrink-0 items-center gap-3 border-b border-border bg-surface/80 px-3 backdrop-blur-sm sm:px-4">
      <button
        onClick={onOpenNav}
        aria-label="Open navigation"
        className="inline-flex size-9 items-center justify-center rounded-md text-muted-foreground hover:bg-surface-2 hover:text-foreground md:hidden"
      >
        <MenuIcon />
      </button>

      <div className="md:hidden">
        <BrandMark collapsed />
      </div>

      <div className="hidden min-w-0 flex-col md:flex">
        <h1 className="truncate text-sm font-semibold leading-tight">{current.label}</h1>
        <p className="truncate text-[11px] text-muted-foreground">{current.description}</p>
      </div>

      <div className="ml-auto flex items-center gap-2">
        <div className="hidden lg:block">
          <InboundStatus />
        </div>
        <ThemeToggle />
        <Button variant="outline" size="sm" onClick={onStartTour} aria-label="Replay tour" className="gap-1.5">
          <IconHelp width={15} height={15} />
          <span className="hidden sm:inline">Tour</span>
        </Button>
        <Button
          variant={chatOpen ? "primary" : "outline"}
          size="sm"
          onClick={onToggleChat}
          aria-pressed={chatOpen}
          className={cn("gap-1.5")}
        >
          <IconSparkles width={15} height={15} />
          <span className="hidden sm:inline">Assistant</span>
        </Button>
      </div>
    </header>
  );
}
