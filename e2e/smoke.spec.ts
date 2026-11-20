import { expect, test } from "./fixtures";

test.describe("Smoke: đăng nhập qua fixture", () => {
  test("khách hàng đăng nhập thấy trang chủ với menu người dùng", async ({
    customerPage,
  }) => {
    await customerPage.goto("/");

    await expect(customerPage.getByTestId("header-user-menu")).toBeVisible();
    await expect(
      customerPage.getByRole("link", { name: "Đăng nhập" }),
    ).toHaveCount(0);
    await expect(customerPage).toHaveURL(/\/$/);
  });

  test("quản trị viên đăng nhập thấy trang tổng quan quản trị", async ({
    adminPage,
  }) => {
    await adminPage.goto("/admin/dashboard");

    await expect(adminPage).toHaveURL(/\/admin\/dashboard$/);
    await expect(
      adminPage.getByRole("heading", { name: "Tổng quan", level: 1 }),
    ).toBeVisible();
  });
});
