import { expect, type Page } from "@playwright/test";

export async function openFirstReviewDocument(page: Page) {
  await page.goto("/");
  const firstRow = page.locator('[data-tour="queue-row"]').first();
  await expect(firstRow).toBeVisible();
  const href = await firstRow.getAttribute("href");
  expect(href).toBeTruthy();
  await page.goto(href!);
}
