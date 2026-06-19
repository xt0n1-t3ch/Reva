"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { Command } from "cmdk";
import { Search } from "lucide-react";
import { navItems } from "@/components/shell/nav";

const groupClass =
  "[&_[cmdk-group-heading]]:px-2 [&_[cmdk-group-heading]]:pb-1 [&_[cmdk-group-heading]]:pt-2 [&_[cmdk-group-heading]]:text-[11px] [&_[cmdk-group-heading]]:font-medium [&_[cmdk-group-heading]]:uppercase [&_[cmdk-group-heading]]:tracking-[0.08em] [&_[cmdk-group-heading]]:text-subtle-foreground";
const itemClass =
  "flex cursor-pointer items-center gap-2.5 rounded-md px-2 py-2 text-[13px] text-foreground outline-none data-[selected=true]:bg-surface-2";

export function CommandPalette({
  open,
  onOpenChange,
  onToggleChat,
}: {
  open: boolean;
  onOpenChange: (value: boolean) => void;
  onToggleChat: () => void;
}) {
  const router = useRouter();

  useEffect(() => {
    const down = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        onOpenChange(!open);
      }
    };
    document.addEventListener("keydown", down);
    return () => document.removeEventListener("keydown", down);
  }, [open, onOpenChange]);

  const run = (action: () => void) => {
    onOpenChange(false);
    action();
  };

  return (
    <Command.Dialog
      open={open}
      onOpenChange={onOpenChange}
      label="Command palette"
      shouldFilter
      overlayClassName="fixed inset-0 z-[100] bg-black/55 backdrop-blur-[1px]"
      contentClassName="fixed left-1/2 top-[14vh] z-[101] w-[calc(100%-2rem)] max-w-lg -translate-x-1/2 overflow-hidden rounded-xl border border-border-strong bg-surface shadow-pop"
    >
      <div className="flex items-center gap-2 border-b border-border px-3.5">
        <Search className="size-4 shrink-0 text-muted-foreground" />
        <Command.Input
          autoFocus
          placeholder="Search pages and actions..."
          className="h-11 flex-1 bg-transparent text-sm text-foreground outline-none placeholder:text-muted-foreground"
        />
      </div>
      <Command.List className="max-h-[340px] overflow-y-auto p-1.5">
        <Command.Empty className="px-3 py-8 text-center text-sm text-muted-foreground">
          No results found.
        </Command.Empty>
        <Command.Group heading="Pages" className={groupClass}>
          {navItems.map(({ href, label, description, Icon }) => (
            <Command.Item
              key={href}
              value={`${label} ${description}`}
              onSelect={() => run(() => router.push(href))}
              className={itemClass}
            >
              <Icon width={16} height={16} className="shrink-0 text-muted-foreground" />
              <span>{label}</span>
              <span className="ml-auto truncate pl-3 text-xs text-subtle-foreground">{description}</span>
            </Command.Item>
          ))}
        </Command.Group>
        <Command.Group heading="Actions" className={groupClass}>
          <Command.Item value="toggle assistant copilot chat" onSelect={() => run(onToggleChat)} className={itemClass}>
            Toggle assistant
          </Command.Item>
        </Command.Group>
      </Command.List>
    </Command.Dialog>
  );
}
