import { expect, test } from "./fixtures";
import { AxeBuilder } from "@axe-core/playwright";

const routes = ["/", "/review", "/export", "/settings"];

for (const route of routes) {
  test(`has no serious or critical axe violations on ${route}`, async ({ page }) => {
    await page.goto(route);
    const results = await new AxeBuilder({ page }).analyze();
    const blockingViolations = results.violations.filter((violation) =>
      violation.impact === "serious" || violation.impact === "critical",
    );
    expect(blockingViolations).toEqual([]);
  });
}
