import { expect, test } from "@playwright/test";

test("queue renders and upload control is present", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("button", { name: /drop bordereaux or click to upload/i })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Work queue" })).toBeVisible();
  await expect(page.locator('[data-tour="queue-row"]').first()).toBeVisible();
});
