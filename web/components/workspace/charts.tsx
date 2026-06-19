"use client";

import { useMemo } from "react";
import type { ReactNode } from "react";
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { DocumentSummary } from "@/lib/api/types";
import {
  buildConfidenceDistribution,
  buildExceptionsByType,
  buildThroughput,
} from "@/lib/chart-data";
import { useChartTheme, type ChartTheme } from "@/lib/chart-theme";
import { EmptyState, Skeleton } from "@/components/ui/states";
import { IconAlert, IconDashboard, IconScale } from "@/components/ui/icons";

const axisTick = (theme: ChartTheme) => ({
  fontSize: 10.5,
  fontFamily: "var(--font-mono)",
  fill: theme.subtleForeground,
});

const chartMargin = { top: 8, right: 8, bottom: 0, left: -18 };

/** Monochrome, token-driven tooltip — the only "card" recharts is allowed to draw. */
function ChartTooltip({
  active,
  payload,
  label,
  theme,
  rows,
}: {
  active?: boolean;
  payload?: { value?: number | string; name?: string; color?: string }[];
  label?: string | number;
  theme: ChartTheme;
  rows: { name: string; key: string; color: string }[];
}) {
  if (!active || !payload || payload.length === 0) {
    return null;
  }
  const byName = new Map(payload.map((entry) => [entry.name, entry.value]));
  const visible = rows.filter((row) => byName.get(row.key) !== undefined);
  return (
    <div
      className="rounded-md border border-border-strong bg-surface px-2.5 py-1.5 shadow-pop"
      style={{ minWidth: 116 }}
    >
      {label !== undefined && (
        <p className="mb-1 font-mono text-[10px] uppercase tracking-[0.08em] text-subtle-foreground">
          {label}
        </p>
      )}
      <div className="flex flex-col gap-1">
        {(visible.length > 0 ? visible : rows).map((row) => (
          <div key={row.key} className="flex items-center justify-between gap-4">
            <span className="flex items-center gap-1.5 text-[11px] text-muted-foreground">
              <span
                aria-hidden="true"
                className="size-1.5 rounded-[1px]"
                style={{ backgroundColor: row.color }}
              />
              {row.name}
            </span>
            <span className="font-mono text-[11px] font-semibold tabular text-foreground">
              {byName.get(row.key) ?? 0}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}

function ChartFrame({
  children,
  empty,
  loading,
  emptyIcon,
  emptyTitle,
  emptyDescription,
}: {
  children: ReactNode;
  empty: boolean;
  loading: boolean;
  emptyIcon: ReactNode;
  emptyTitle: string;
  emptyDescription: string;
}) {
  if (loading) {
    return <Skeleton className="m-4 h-[180px]" />;
  }
  if (empty) {
    return (
      <div className="flex h-[212px] items-center justify-center p-4">
        <EmptyState icon={emptyIcon} title={emptyTitle} description={emptyDescription} />
      </div>
    );
  }
  return <div className="h-[212px] px-2 pb-2 pt-3">{children}</div>;
}

export function ThroughputChart({
  documents,
  loading,
}: {
  documents: DocumentSummary[];
  loading: boolean;
}) {
  const theme = useChartTheme();
  const data = useMemo(() => buildThroughput(documents), [documents]);
  const empty = data.every((point) => point.total === 0);
  const rows = [
    { name: "Reviewed", key: "reviewed", color: theme.accent },
    { name: "Pending", key: "pending", color: theme.warning },
  ];

  return (
    <ChartFrame
      loading={loading}
      empty={data.length === 0 || empty}
      emptyIcon={<IconDashboard width={20} height={20} />}
      emptyTitle="No throughput yet"
      emptyDescription="Documents you ingest will chart here, split by reviewed and pending."
    >
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={data} margin={chartMargin}>
          <defs>
            <linearGradient id="reva-area-reviewed" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={theme.accent} stopOpacity={0.28} />
              <stop offset="100%" stopColor={theme.accent} stopOpacity={0.02} />
            </linearGradient>
            <linearGradient id="reva-area-pending" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={theme.warning} stopOpacity={0.22} />
              <stop offset="100%" stopColor={theme.warning} stopOpacity={0.02} />
            </linearGradient>
          </defs>
          <CartesianGrid stroke={theme.grid} strokeDasharray="2 4" vertical={false} />
          <XAxis
            dataKey="label"
            tick={axisTick(theme)}
            tickLine={false}
            axisLine={{ stroke: theme.border }}
            interval="preserveStartEnd"
            minTickGap={18}
          />
          <YAxis
            allowDecimals={false}
            width={34}
            tick={axisTick(theme)}
            tickLine={false}
            axisLine={false}
          />
          <Tooltip
            cursor={{ stroke: theme.borderStrong, strokeWidth: 1 }}
            content={<ChartTooltip theme={theme} rows={rows} />}
          />
          <Area
            type="monotone"
            dataKey="reviewed"
            name="reviewed"
            stackId="1"
            stroke={theme.accent}
            strokeWidth={1.5}
            fill="url(#reva-area-reviewed)"
            isAnimationActive={false}
            dot={false}
            activeDot={{ r: 2.5, strokeWidth: 0, fill: theme.accent }}
          />
          <Area
            type="monotone"
            dataKey="pending"
            name="pending"
            stackId="1"
            stroke={theme.warning}
            strokeWidth={1.5}
            fill="url(#reva-area-pending)"
            isAnimationActive={false}
            dot={false}
            activeDot={{ r: 2.5, strokeWidth: 0, fill: theme.warning }}
          />
        </AreaChart>
      </ResponsiveContainer>
    </ChartFrame>
  );
}

export function ExceptionsChart({
  documents,
  loading,
}: {
  documents: DocumentSummary[];
  loading: boolean;
}) {
  const theme = useChartTheme();
  const data = useMemo(() => buildExceptionsByType(documents), [documents]);
  const rows = [{ name: "Exceptions", key: "value", color: theme.danger }];

  return (
    <ChartFrame
      loading={loading}
      empty={data.length === 0}
      emptyIcon={<IconAlert width={20} height={20} />}
      emptyTitle="No open exceptions"
      emptyDescription="Reconciliation flags grouped by document type will appear here when raised."
    >
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={data} layout="vertical" margin={{ top: 4, right: 14, bottom: 0, left: 4 }}>
          <CartesianGrid stroke={theme.grid} strokeDasharray="2 4" horizontal={false} />
          <XAxis
            type="number"
            allowDecimals={false}
            tick={axisTick(theme)}
            tickLine={false}
            axisLine={{ stroke: theme.border }}
          />
          <YAxis
            type="category"
            dataKey="label"
            width={92}
            tick={{ ...axisTick(theme), fontFamily: "var(--font-sans)", fontSize: 11 }}
            tickLine={false}
            axisLine={false}
          />
          <Tooltip
            cursor={{ fill: theme.accentSoft }}
            content={<ChartTooltip theme={theme} rows={rows} />}
          />
          <Bar
            dataKey="value"
            name="value"
            fill={theme.danger}
            radius={[0, 3, 3, 0]}
            barSize={14}
            isAnimationActive={false}
          />
        </BarChart>
      </ResponsiveContainer>
    </ChartFrame>
  );
}

export function ConfidenceChart({
  documents,
  loading,
}: {
  documents: DocumentSummary[];
  loading: boolean;
}) {
  const theme = useChartTheme();
  const data = useMemo(() => buildConfidenceDistribution(documents), [documents]);
  const tierColor: Record<"high" | "medium" | "low", string> = {
    high: theme.success,
    medium: theme.warning,
    low: theme.danger,
  };
  const empty = data.every((bucket) => bucket.count === 0);
  const rows = [{ name: "Documents", key: "count", color: theme.accent }];

  return (
    <ChartFrame
      loading={loading}
      empty={empty}
      emptyIcon={<IconScale width={20} height={20} />}
      emptyTitle="No extractions yet"
      emptyDescription="Once documents are extracted, their confidence spread charts across three tiers."
    >
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={data} margin={chartMargin}>
          <CartesianGrid stroke={theme.grid} strokeDasharray="2 4" vertical={false} />
          <XAxis
            dataKey="label"
            tick={{ ...axisTick(theme), fontFamily: "var(--font-sans)", fontSize: 11 }}
            tickLine={false}
            axisLine={{ stroke: theme.border }}
          />
          <YAxis
            allowDecimals={false}
            width={34}
            tick={axisTick(theme)}
            tickLine={false}
            axisLine={false}
          />
          <Tooltip
            cursor={{ fill: theme.accentSoft }}
            content={<ChartTooltip theme={theme} rows={rows} />}
          />
          <Bar dataKey="count" name="count" radius={[3, 3, 0, 0]} barSize={46} isAnimationActive={false}>
            {data.map((bucket) => (
              <Cell key={bucket.tier} fill={tierColor[bucket.tier]} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </ChartFrame>
  );
}
