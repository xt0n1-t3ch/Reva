"use client";

import Image from "next/image";
import Link from "next/link";
import { PanelLeft, Search, Sparkles } from "lucide-react";
import { ThemeToggle } from "@/components/shell/theme-toggle";

function BrandLogo() {
  return (
    <span className="relative grid size-7 shrink-0 place-items-center">
      <Image
        src="/reva-logo-dark.png"
        alt=""
        aria-hidden="true"
        width={28}
        height={28}
        priority
        className="size-7 object-contain dark:hidden"
      />
      <Image
        src="/reva-logo-light.png"
        alt=""
        aria-hidden="true"
        width={28}
        height={28}
        priority
        className="hidden size-7 object-contain dark:block"
      />
    </span>
  );
}

export function TopBar({
  onOpenNav,
  onToggleChat,
  chatOpen,
  onOpenPalette,
}: {
  onOpenNav: () => void;
  onToggleChat: () => void;
  chatOpen: boolean;
  onStartTour: () => void;
  onOpenPalette: () => void;
}) {
  return (
    <header className="flex h-14 shrink-0 items-center gap-3 border-b border-border bg-background px-3 sm:px-4">
      <button
        onClick={onOpenNav}
        aria-label="Open navigation"
        className="grid size-8 place-items-center rounded-md text-muted-foreground hover:bg-surface-2 hover:text-foreground md:hidden"
      >
        <PanelLeft className="size-4" />
      </button>

      <Link href="/" className="flex items-center gap-2 pr-2">
        <BrandLogo />
        <span className="text-[15px] font-semibold tracking-tight text-foreground">Reva</span>
      </Link>

      <button
        type="button"
        onClick={onOpenPalette}
        className="group hidden h-9 w-full max-w-[360px] items-center gap-2 rounded-md border border-border bg-surface px-2.5 text-sm text-muted-foreground transition-colors hover:border-border-strong sm:flex"
      >
        <Search className="size-4 shrink-0" />
        <span className="flex-1 text-left">Search Reva</span>
        <kbd className="rounded border border-border bg-surface-2 px-1.5 py-0.5 font-mono text-[11px] leading-none text-subtle-foreground">
          ⌘K
        </kbd>
      </button>

      <div className="ml-auto flex items-center gap-1">
        <button
          type="button"
          onClick={onOpenPalette}
          aria-label="Search"
          className="grid size-8 place-items-center rounded-md text-muted-foreground hover:bg-surface-2 hover:text-foreground sm:hidden"
        >
          <Search className="size-4" />
        </button>
        <ThemeToggle />
        <button
          type="button"
          onClick={onToggleChat}
          aria-pressed={chatOpen}
          aria-label="Toggle assistant"
          className={
            chatOpen
              ? "inline-flex h-8 items-center gap-1.5 rounded-md bg-primary px-2.5 text-sm font-medium text-primary-foreground"
              : "inline-flex h-8 items-center gap-1.5 rounded-md border border-border bg-surface px-2.5 text-sm font-medium text-foreground hover:border-border-strong"
          }
        >
          <Sparkles className="size-4" />
          <span className="hidden md:inline">Assistant</span>
        </button>
      </div>
    </header>
  );
}
