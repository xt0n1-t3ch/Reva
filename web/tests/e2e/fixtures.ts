import { test as base } from "@playwright/test";

// Shared fixture for every spec except the onboarding flow itself: suppress the
// first-run tour so each test starts from a deterministic, unobstructed page.
export const test = base.extend({
  page: async ({ page }, use) => {
    await page.addInitScript(() => window.localStorage.setItem("reva-onboarding-done", "true"));
    // eslint-disable-next-line react-hooks/rules-of-hooks -- Playwright fixture API, not a React hook
    await use(page);
  },
});

export { expect } from "@playwright/test";
