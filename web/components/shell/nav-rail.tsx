"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/cn";
import { config } from "@/lib/config";
import { navItems } from "@/components/shell/nav";
import { IconSparkles } from "@/components/ui/icons";

const isActive = (pathname: string, href: string) =>
  href === "/" ? pathname === "/" : pathname.startsWith(href);

export function BrandMark({ collapsed = false }: { collapsed?: boolean }) {
  return (
    <div className="flex items-center gap-2.5">
      <span className="flex size-8 shrink-0 items-center justify-center rounded-md bg-primary text-primary-foreground shadow-soft">
        <IconSparkles width={17} height={17} />
      </span>
      {!collapsed && (
        <span className="flex flex-col leading-tight">
          <span className="text-sm font-semibold tracking-tight">{config.productName}</span>
          <span className="text-[11px] text-muted-foreground">Bordereaux intelligence</span>
        </span>
      )}
    </div>
  );
}

export function NavLinks({
  pathname,
  onNavigate,
  collapsed = false,
}: {
  pathname: string;
  onNavigate?: () => void;
  collapsed?: boolean;
}) {
  return (
    <nav className="flex flex-col gap-0.5" aria-label="Primary">
      {navItems.map(({ href, label, description, Icon }) => {
        const active = isActive(pathname, href);
        return (
          <Link
            key={href}
            href={href}
            onClick={onNavigate}
            aria-current={active ? "page" : undefined}
            title={collapsed ? label : undefined}
            className={cn(
              "group relative flex items-center gap-3 rounded-md px-2.5 py-2 text-sm transition-colors",
              collapsed && "justify-center px-0",
              active
                ? "bg-primary-soft text-primary"
                : "text-muted-foreground hover:bg-surface-2 hover:text-foreground",
            )}
          >
            {active && (
              <span className="absolute left-0 top-1/2 h-5 w-0.5 -translate-y-1/2 rounded-full bg-primary" />
            )}
            <Icon width={18} height={18} className="shrink-0" />
            {!collapsed && (
              <span className="flex min-w-0 flex-col">
                <span className="font-medium leading-tight">{label}</span>
                <span className="truncate text-[11px] text-subtle-foreground">{description}</span>
              </span>
            )}
          </Link>
        );
      })}
    </nav>
  );
}

export function NavRail() {
  const pathname = usePathname();
  return (
    <aside className="hidden h-full w-16 shrink-0 flex-col border-r border-border bg-surface md:flex lg:w-60">
      <div className="flex h-14 items-center border-b border-border px-3 lg:px-4">
        <span className="hidden lg:block">
          <BrandMark />
        </span>
        <span className="lg:hidden">
          <BrandMark collapsed />
        </span>
      </div>
      <div className="flex-1 overflow-y-auto p-2 lg:p-3">
        <p className="hidden px-2.5 pb-1.5 text-[11px] font-semibold uppercase tracking-wider text-subtle-foreground lg:block">
          Operate
        </p>
        <span className="hidden lg:block">
          <NavLinks pathname={pathname} />
        </span>
        <span className="lg:hidden">
          <NavLinks pathname={pathname} collapsed />
        </span>
      </div>
      <div className="border-t border-border p-3">
        <p className="hidden text-[11px] leading-relaxed text-subtle-foreground lg:block">
          Local-first · keyless AI · offline-ready
        </p>
      </div>
    </aside>
  );
}
