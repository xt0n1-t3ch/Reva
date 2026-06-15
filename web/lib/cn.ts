export const cn = (...values: Array<string | false | null | undefined>): string =>
  values.filter(Boolean).join(" ");
