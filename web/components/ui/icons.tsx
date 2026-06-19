import type { SVGProps } from "react";

type IconProps = SVGProps<SVGSVGElement>;

const base = (props: IconProps) => ({
  width: 16,
  height: 16,
  viewBox: "0 0 24 24",
  fill: "none",
  stroke: "currentColor",
  strokeWidth: 1.75,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
  "aria-hidden": true,
  ...props,
});

export const IconDashboard = (p: IconProps) => (
  <svg {...base(p)}>
    <rect x="3" y="3" width="7" height="9" rx="1.5" />
    <rect x="14" y="3" width="7" height="5" rx="1.5" />
    <rect x="14" y="12" width="7" height="9" rx="1.5" />
    <rect x="3" y="16" width="7" height="5" rx="1.5" />
  </svg>
);

export const IconReview = (p: IconProps) => (
  <svg {...base(p)}>
    <rect x="3" y="4" width="18" height="16" rx="2" />
    <path d="M12 4v16" />
    <path d="M6.5 9h2.5M6.5 13h2.5" />
    <path d="M15 9h3M15 13h3" />
  </svg>
);

export const IconExport = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M12 3v12" />
    <path d="m8 11 4 4 4-4" />
    <path d="M5 19h14" />
  </svg>
);

export const IconMappings = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M4 7h7" />
    <path d="M13 7h7" />
    <path d="M4 17h7" />
    <path d="M13 17h7" />
    <path d="M9 7c2 0 4 10 6 10" />
    <path d="M15 7c-2 0-4 10-6 10" />
    <circle cx="4" cy="7" r="1.5" />
    <circle cx="20" cy="7" r="1.5" />
    <circle cx="4" cy="17" r="1.5" />
    <circle cx="20" cy="17" r="1.5" />
  </svg>
);

export const IconSettings = (p: IconProps) => (
  <svg {...base(p)}>
    <circle cx="12" cy="12" r="3" />
    <path d="M19.4 13.5a7.7 7.7 0 0 0 0-3l1.7-1.3-1.8-3.1-2 .8a7.5 7.5 0 0 0-2.6-1.5l-.3-2.1H8l-.3 2.1a7.5 7.5 0 0 0-2.6 1.5l-2-.8L1.3 9.2 3 10.5a7.7 7.7 0 0 0 0 3l-1.7 1.3 1.8 3.1 2-.8a7.5 7.5 0 0 0 2.6 1.5l.3 2.1h3.7l.3-2.1a7.5 7.5 0 0 0 2.6-1.5l2 .8 1.8-3.1Z" />
  </svg>
);

export const IconSparkles = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M12 3l1.6 4.4L18 9l-4.4 1.6L12 15l-1.6-4.4L6 9l4.4-1.6Z" />
    <path d="M19 14l.8 2.2L22 17l-2.2.8L19 20l-.8-2.2L16 17l2.2-.8Z" />
  </svg>
);

export const IconSpark = (p: IconProps) => (
  <svg {...base(p)} fill="currentColor" stroke="none">
    <path d="M12 2.6c.35 3.07.86 4.45 1.9 5.5 1.05 1.04 2.43 1.55 5.5 1.9-3.07.35-4.45.86-5.5 1.9-1.04 1.05-1.55 2.43-1.9 5.5-.35-3.07-.86-4.45-1.9-5.5-1.05-1.04-2.43-1.55-5.5-1.9 3.07-.35 4.45-.86 5.5-1.9 1.04-1.05 1.55-2.43 1.9-5.5Z" />
    <path d="M19 13.5c.16 1.2.36 1.74.84 2.16.46.42 1.05.62 2.16.84-1.11.22-1.7.42-2.16.84-.48.42-.68.96-.84 2.16-.16-1.2-.36-1.74-.84-2.16-.46-.42-1.05-.62-2.16-.84 1.11-.22 1.7-.42 2.16-.84.48-.42.68-.96.84-2.16Z" opacity="0.55" />
  </svg>
);

export const IconCpu = (p: IconProps) => (
  <svg {...base(p)}>
    <rect x="7" y="7" width="10" height="10" rx="1.5" />
    <path d="M9.5 10.5h5v3h-5z" />
    <path d="M10 3v2M14 3v2M10 19v2M14 19v2M3 10h2M3 14h2M19 10h2M19 14h2" />
  </svg>
);

export const IconCloud = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M7 18a4 4 0 0 1-.5-7.97A5.5 5.5 0 0 1 17 9.5a3.5 3.5 0 0 1 .5 6.97" />
    <path d="M7 18h10" />
  </svg>
);

export const IconKey = (p: IconProps) => (
  <svg {...base(p)}>
    <circle cx="8" cy="15" r="4" />
    <path d="m10.8 12.2 8.2-8.2M16 5l2.5 2.5M14 7l2.5 2.5" />
  </svg>
);

export const IconUpload = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M12 16V4" />
    <path d="m7 9 5-5 5 5" />
    <path d="M5 16v2a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-2" />
  </svg>
);

export const IconSun = (p: IconProps) => (
  <svg {...base(p)}>
    <circle cx="12" cy="12" r="4" />
    <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
  </svg>
);

export const IconMoon = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8Z" />
  </svg>
);

export const IconMonitor = (p: IconProps) => (
  <svg {...base(p)}>
    <rect x="3" y="4" width="18" height="12" rx="2" />
    <path d="M8 20h8M12 16v4" />
  </svg>
);

export const IconDocument = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M14 3v4a1 1 0 0 0 1 1h4" />
    <path d="M5 3h9l5 5v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2Z" />
    <path d="M8 13h8M8 17h6" />
  </svg>
);

export const IconCheck = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="m4 12 5 5L20 6" />
  </svg>
);

export const IconAlert = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M12 9v4M12 17h.01" />
    <path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" />
  </svg>
);

export const IconSend = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M22 2 11 13" />
    <path d="M22 2 15 22l-4-9-9-4Z" />
  </svg>
);

export const IconClose = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M18 6 6 18M6 6l12 12" />
  </svg>
);

export const IconChevronRight = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="m9 6 6 6-6 6" />
  </svg>
);

export const IconChevronLeft = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="m15 6-6 6 6 6" />
  </svg>
);

export const IconZoomIn = (p: IconProps) => (
  <svg {...base(p)}>
    <circle cx="11" cy="11" r="7" />
    <path d="m20 20-3.5-3.5M11 8v6M8 11h6" />
  </svg>
);

export const IconZoomOut = (p: IconProps) => (
  <svg {...base(p)}>
    <circle cx="11" cy="11" r="7" />
    <path d="m20 20-3.5-3.5M8 11h6" />
  </svg>
);

export const IconFit = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M4 9V5a1 1 0 0 1 1-1h4M15 4h4a1 1 0 0 1 1 1v4M20 15v4a1 1 0 0 1-1 1h-4M9 20H5a1 1 0 0 1-1-1v-4" />
  </svg>
);

export const IconSearch = (p: IconProps) => (
  <svg {...base(p)}>
    <circle cx="11" cy="11" r="7" />
    <path d="m20 20-3.5-3.5" />
  </svg>
);

export const IconMail = (p: IconProps) => (
  <svg {...base(p)}>
    <rect x="3" y="5" width="18" height="14" rx="2" />
    <path d="m3 7 9 6 9-6" />
  </svg>
);

export const IconTool = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M14.5 5.5a3.5 3.5 0 0 0 4.6 4.6l-9.8 9.8a2 2 0 0 1-2.8-2.8Z" />
    <path d="m14.5 5.5 4 4" />
  </svg>
);

export const IconScale = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M12 3v18M7 21h10" />
    <path d="M5 7h14l-3 6a3 3 0 0 1-8 0Z" />
    <path d="m5 7 3 6M19 7l-3 6" />
  </svg>
);
export const IconHelp = (p: IconProps) => (
  <svg {...base(p)}>
    <circle cx="12" cy="12" r="9" />
    <path d="M9.8 9a2.3 2.3 0 1 1 3.8 1.8c-.9.6-1.6 1.2-1.6 2.4" />
    <path d="M12 17h.01" />
  </svg>
);

export const IconBook = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M4 5a2 2 0 0 1 2-2h13v15H6a2 2 0 0 0-2 2Z" />
    <path d="M4 19a2 2 0 0 1 2-2h13" />
    <path d="M9 7h6M9 11h4" />
  </svg>
);

export const IconPlus = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M12 5v14M5 12h14" />
  </svg>
);

export const IconTrash = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M4 7h16" />
    <path d="M10 11v6M14 11v6" />
    <path d="M6 7l1 13a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1l1-13" />
    <path d="M9 7V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v3" />
  </svg>
);

export const IconCopy = (p: IconProps) => (
  <svg {...base(p)}>
    <rect x="9" y="9" width="11" height="11" rx="2" />
    <path d="M5 15V5a2 2 0 0 1 2-2h8" />
  </svg>
);

export const IconPencil = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M12 20h9" />
    <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z" />
  </svg>
);

export const IconMore = (p: IconProps) => (
  <svg {...base(p)}>
    <circle cx="5" cy="12" r="1.4" />
    <circle cx="12" cy="12" r="1.4" />
    <circle cx="19" cy="12" r="1.4" />
  </svg>
);

export const IconDrag = (p: IconProps) => (
  <svg {...base(p)}>
    <circle cx="9" cy="6" r="1.3" />
    <circle cx="9" cy="12" r="1.3" />
    <circle cx="9" cy="18" r="1.3" />
    <circle cx="15" cy="6" r="1.3" />
    <circle cx="15" cy="12" r="1.3" />
    <circle cx="15" cy="18" r="1.3" />
  </svg>
);

export const IconArrowUp = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M12 19V5M6 11l6-6 6 6" />
  </svg>
);

export const IconArrowDown = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M12 5v14M6 13l6 6 6-6" />
  </svg>
);

