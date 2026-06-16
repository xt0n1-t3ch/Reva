import { expect, test } from "@playwright/test";
import { openFirstReviewDocument } from "./helpers";

test("field hover activates a source citation", async ({ page }) => {
  await openFirstReviewDocument(page);
  await expect(page.locator('[data-tour="review-split-view"]')).toBeVisible();
  const field = page.locator('[data-citation-row="field"]').first();
  await expect(field).toBeVisible();
  await field.hover();
  await expect(page.locator('mark[data-active="true"]').first()).toBeVisible();
});
