"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/cn";
import { navItems } from "@/components/shell/nav";

const isActive = (pathname: string, href: string) =>
  href === "/" ? pathname === "/" : pathname.startsWith(href);

export function NavList({
  pathname,
  onNavigate,
}: {
  pathname: string;
  onNavigate?: () => void;
}) {
  return (
    <nav className="flex flex-col gap-px" aria-label="Primary">
      <p className="px-3 pb-1.5 pt-1 text-[11px] font-medium uppercase tracking-[0.08em] text-subtle-foreground">
        Operate
      </p>
      {navItems.map(({ href, label, Icon }) => {
        const active = isActive(pathname, href);
        return (
          <Link
            key={href}
            href={href}
            onClick={onNavigate}
            aria-current={active ? "page" : undefined}
            className={cn(
              "flex items-center gap-2.5 rounded-md px-3 py-1.5 text-[13px] transition-colors",
              active
                ? "bg-surface-2 font-medium text-foreground"
                : "text-muted-foreground hover:bg-surface-2 hover:text-foreground",
            )}
          >
            <Icon width={16} height={16} className="shrink-0 opacity-90" />
            <span>{label}</span>
          </Link>
        );
      })}
    </nav>
  );
}

export function NavRail() {
  const pathname = usePathname();
  return (
    <aside className="hidden h-full w-60 shrink-0 flex-col border-r border-border bg-background md:flex">
      <div className="flex-1 overflow-y-auto p-3">
        <NavList pathname={pathname} />
      </div>
      <div className="border-t border-border px-3 py-3">
        <div className="group relative overflow-hidden rounded-md border border-border bg-surface-2/40 px-2.5 py-2">
          <span
            aria-hidden="true"
            className="pointer-events-none absolute inset-x-0 -top-px h-px bg-gradient-to-r from-transparent via-accent/70 to-transparent"
          />
          <div className="flex items-center gap-2">
            <span className="relative flex size-2 shrink-0">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-accent/60" />
              <span className="relative inline-flex size-2 rounded-full bg-accent" />
            </span>
            <span className="text-[11px] font-medium leading-tight text-foreground">Engine online</span>
          </div>
          <p className="mt-0.5 font-mono text-[10px] leading-tight text-subtle-foreground">
            Deterministic · source-cited · auditable
          </p>
        </div>
      </div>
    </aside>
  );
}
