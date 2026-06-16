import { expect, test } from "./fixtures";

test("assistant opens and accepts a message", async ({ page }) => {
  await page.goto("/");

  await page.getByRole("button", { name: /assistant/i }).first().click();

  const input = page.getByRole("textbox").first();
  await expect(input).toBeVisible();

  const prompt = "Which documents need review first?";
  await input.fill(prompt);
  await page.getByRole("button", { name: "Send" }).click();

  await expect(page.getByText(prompt)).toBeVisible();
});
