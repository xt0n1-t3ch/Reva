import { expect, test } from "@playwright/test";

test("first-run tour can be completed and stays dismissed", async ({ page }) => {
  await page.addInitScript(() => window.localStorage.removeItem("reva-onboarding-done"));
  await page.goto("/");
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();

  const isDone = () => page.evaluate(() => window.localStorage.getItem("reva-onboarding-done") === "true");

  // The tour advances across routes and auto-skips a step whose target is not yet present,
  // so the dialog flickers between steps. Drive it to true completion — finish when offered,
  // otherwise advance — using the persisted completion flag as the only stop signal.
  for (let i = 0; i < 20 && !(await isDone()); i += 1) {
    const finish = page.getByRole("button", { name: "Finish" });
    if (await finish.isVisible().catch(() => false)) {
      await finish.click();
      continue;
    }
    const next = page.getByRole("button", { name: "Next" });
    if (await next.isVisible().catch(() => false)) {
      await next.click();
      continue;
    }
    await page.waitForTimeout(250);
  }

  await expect.poll(() => page.evaluate(() => window.localStorage.getItem("reva-onboarding-done"))).toBe("true");
  await expect(dialog).toBeHidden();
  await page.reload();
  await expect(page.getByRole("dialog")).toHaveCount(0);
});
