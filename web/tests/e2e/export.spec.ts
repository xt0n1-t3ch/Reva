import { expect, test } from "./fixtures";

test("templates and download links render", async ({ page }) => {
  await page.goto("/export");
  await expect(page.getByRole("heading", { name: "Templates" })).toBeVisible();
  await expect(page.locator('[data-tour="export-panel"]')).toBeVisible();
  await expect(page.locator('a[href*="/api/documents/"][href*="/export?"]').first()).toBeVisible();
});
