import { expect, test } from "@playwright/test";

test("first-run tour can be completed and stays dismissed", async ({ page }) => {
  await page.addInitScript(() => window.localStorage.removeItem("reva-onboarding-done"));
  await page.goto("/");
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();

  for (let step = 0; step < 5; step += 1) {
    const finish = page.getByRole("button", { name: "Finish" });
    if (await finish.isVisible().catch(() => false)) {
      await finish.click();
      break;
    }
    await page.getByRole("button", { name: "Next" }).click();
  }

  await expect(dialog).toBeHidden();
  await expect.poll(() => page.evaluate(() => window.localStorage.getItem("reva-onboarding-done"))).toBe("true");
  await page.reload();
  await expect(page.getByRole("dialog")).toHaveCount(0);
});
