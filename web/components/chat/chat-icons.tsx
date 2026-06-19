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

export const IconChevron = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="m6 9 6 6 6-6" />
  </svg>
);

export const IconClock = (p: IconProps) => (
  <svg {...base(p)}>
    <circle cx="12" cy="12" r="9" />
    <path d="M12 7v5l3 2" />
  </svg>
);

export const IconBrain = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M9.5 4a2.5 2.5 0 0 0-2.5 2.5A2.5 2.5 0 0 0 5 9a2.5 2.5 0 0 0 1 2 2.5 2.5 0 0 0 .5 4 2.5 2.5 0 0 0 3 2.5V4Z" />
    <path d="M14.5 4A2.5 2.5 0 0 1 17 6.5 2.5 2.5 0 0 1 19 9a2.5 2.5 0 0 1-1 2 2.5 2.5 0 0 1-.5 4 2.5 2.5 0 0 1-3 2.5V4Z" />
  </svg>
);

export const IconCheckSmall = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="m5 12 4 4L19 7" />
  </svg>
);

export const IconActivity = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M3 12h4l2 6 4-14 2 8h6" />
  </svg>
);

// Universal step-kind icons for the agent activity log.
export const IconRead = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M12 6.5C9.5 4.5 6.5 4.5 4 5.5v12c2.5-1 5.5-1 8 1 2.5-2 5.5-2 8-1v-12c-2.5-1-5.5-1-8 1Z" />
    <path d="M12 6.5v12" />
  </svg>
);

export const IconSearch = (p: IconProps) => (
  <svg {...base(p)}>
    <circle cx="11" cy="11" r="7" />
    <path d="m20 20-3.5-3.5" />
  </svg>
);

export const IconWrite = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M12 20h9" />
    <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z" />
  </svg>
);

export const IconNavigate = (p: IconProps) => (
  <svg {...base(p)}>
    <circle cx="12" cy="12" r="9" />
    <path d="m15.5 8.5-2 5-5 2 2-5Z" />
  </svg>
);

export const IconTool = (p: IconProps) => (
  <svg {...base(p)}>
    <path d="M14.5 5.5a3.5 3.5 0 0 0 4.6 4.6l-9.8 9.8a2 2 0 0 1-2.8-2.8Z" />
    <path d="m14.5 5.5 4 4" />
  </svg>
);
