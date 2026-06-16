import { expect, test } from "@playwright/test";

test("settings load and a field is editable", async ({ page }) => {
  await page.goto("/settings");
  const productName = page.getByLabel("Product name");
  await expect(productName).toBeVisible();
  const currentValue = await productName.inputValue();
  await productName.fill(`${currentValue} Test`);
  await expect(productName).toHaveValue(`${currentValue} Test`);
});
