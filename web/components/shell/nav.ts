import {
  IconBook,
  IconDashboard,
  IconExport,
  IconMappings,
  IconReview,
  IconSettings,
  type IconDashboard as IconType,
} from "@/components/ui/icons";

export interface NavItem {
  href: string;
  label: string;
  description: string;
  Icon: typeof IconType;
}

export const navItems: NavItem[] = [
  {
    href: "/",
    label: "Workspace",
    description: "Ingest, extract, and triage bordereaux",
    Icon: IconDashboard,
  },
  {
    href: "/review",
    label: "Review",
    description: "Source-cited split-view reconciliation",
    Icon: IconReview,
  },
  {
    href: "/export",
    label: "Export",
    description: "Templates and downloads",
    Icon: IconExport,
  },
  {
    href: "/mappings",
    label: "Mappings",
    description: "Per-sender schema learning",
    Icon: IconMappings,
  },
  {
    href: "/knowledge",
    label: "Knowledge",
    description: "How to use Reva and the industry standards it applies",
    Icon: IconBook,
  },
  {
    href: "/settings",
    label: "Settings",
    description: "Theme, thresholds, and sources",
    Icon: IconSettings,
  },
];
