import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import DashboardRecipesPage from "../page";
import * as api from "@/lib/my-recipes";
import type { MyRecipeSummary } from "@/lib/my-recipes";

jest.mock("next/navigation", () => ({
  useRouter: () => ({ replace: jest.fn(), push: jest.fn(), refresh: jest.fn() }),
}));
jest.mock("@/lib/my-recipes", () => ({
  ...jest.requireActual("@/lib/my-recipes"),
  getMyRecipes: jest.fn(),
  getMyRecipeCounts: jest.fn(),
  deleteRecipe: jest.fn(),
}));

/**
 * K18 NVDA lỗi 3 (K18_nvda_speech_log.txt mục E): mỗi hàng chỉ đọc "Sửa  visited  link" -> không biết sửa công thức nào.
 * Tên đầy đủ đặt ở aria-label, chữ nhìn thấy giữ nguyên và đứng đầu tên (WCAG 2.5.3).
 */
const m = api as jest.Mocked<typeof api>;
const row = (i: number, status: string): MyRecipeSummary => ({
  id: `r${i}`, title: `Canh chua ${i}`, slug: `canh-chua-${i}`, categoryId: "c1", categoryName: "Món canh",
  prepTimeMinutes: 10, cookTimeMinutes: 20, servings: 2, difficulty: "Easy", status, primaryImageUrl: null,
  ingredientCount: 1, stepCount: 1, publishedAt: null, createdAt: "2026-10-01T00:00:00Z", updatedAt: "2026-10-02T00:00:00Z", rowVersion: "v",
});
const rowOf = (title: string) => within(screen.getByText(title).closest("tr")!);

beforeEach(() => {
  jest.clearAllMocks();
  localStorage.setItem("accessToken", "t");
  m.getMyRecipeCounts.mockResolvedValue({ all: 2, byStatus: { Draft: 1, Published: 1 } });
  m.getMyRecipes.mockResolvedValue({ items: [row(1, "Draft"), row(2, "Published")], page: 1, pageSize: 20, totalCount: 2 });
});

it("lien ket Xem, Sua va nut Xoa trong hang kem ten cong thuc", async () => {
  render(<DashboardRecipesPage />);
  await screen.findByText("Canh chua 2");

  const published = rowOf("Canh chua 2");
  expect(published.getByRole("link", { name: /^Xem/ })).toHaveAttribute("aria-label", "Xem công thức Canh chua 2");
  expect(published.getByRole("link", { name: /^Xem/ })).toHaveTextContent(/^Xem$/);
  expect(published.getByRole("link", { name: /^Sửa/ })).toHaveAttribute("aria-label", "Sửa công thức Canh chua 2");
  expect(published.getByRole("link", { name: /^Sửa/ })).toHaveTextContent(/^Sửa$/);
  expect(published.getByRole("button", { name: /^Xoá/ })).toHaveAttribute("aria-label", "Xoá công thức Canh chua 2");
  expect(published.getByRole("button", { name: /^Xoá/ })).toHaveTextContent(/^Xoá$/);

  // Bản nháp không có liên kết Xem; Sửa/Xoá mang tên riêng của hàng
  const draft = rowOf("Canh chua 1");
  expect(draft.queryByRole("link", { name: /^Xem/ })).toBeNull();
  expect(draft.getByRole("link", { name: "Sửa công thức Canh chua 1" })).toBeInTheDocument();
  expect(draft.getByRole("button", { name: "Xoá công thức Canh chua 1" })).toBeInTheDocument();
});

it("dang xoa: ten nut doi theo chu nhin thay 'Dang xoa…'", async () => {
  const user = userEvent.setup();
  jest.spyOn(window, "confirm").mockReturnValue(true);
  m.deleteRecipe.mockReturnValue(new Promise(() => { /* treo để giữ trạng thái đang xoá */ }));
  render(<DashboardRecipesPage />);
  await screen.findByText("Canh chua 1");

  await user.click(rowOf("Canh chua 1").getByRole("button", { name: /^Xoá/ }));
  const busy = rowOf("Canh chua 1").getByRole("button", { name: /^Đang xoá/ });
  expect(busy).toHaveAttribute("aria-label", "Đang xoá công thức Canh chua 1");
  expect(busy).toHaveTextContent(/^Đang xoá…$/);
});
