import { render, screen } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import DashboardRecipesPage from "../page";
import * as api from "@/lib/my-recipes";
import type { MyRecipeSummary } from "@/lib/my-recipes";

expect.extend(toHaveNoViolations);

jest.mock("next/navigation", () => ({
  useRouter: () => ({ replace: jest.fn(), push: jest.fn(), refresh: jest.fn() }),
}));
jest.mock("@/lib/my-recipes", () => ({
  ...jest.requireActual("@/lib/my-recipes"),
  getMyRecipes: jest.fn(),
  getMyRecipeCounts: jest.fn(),
  deleteRecipe: jest.fn(),
}));

/** K18 — axe-core (jest-axe) cho /dashboard/recipes. Không đo tương phản màu/bố cục (jsdom) và KHÔNG thay NVDA. */
const m = api as jest.Mocked<typeof api>;
const row = (i: number, status: string, img: string | null): MyRecipeSummary => ({
  id: `r${i}`, title: `Canh chua ${i}`, slug: `canh-chua-${i}`, categoryId: "c1", categoryName: "Món canh",
  prepTimeMinutes: 10, cookTimeMinutes: 20, servings: 2, difficulty: "Easy", status, primaryImageUrl: img,
  ingredientCount: i, stepCount: i - 1, publishedAt: null, createdAt: "2026-10-01T00:00:00Z", updatedAt: "2026-10-02T00:00:00Z", rowVersion: "v",
});

beforeEach(() => {
  jest.clearAllMocks();
  localStorage.setItem("accessToken", "t");
  m.getMyRecipeCounts.mockResolvedValue({ all: 3, byStatus: { Draft: 2, Published: 1 } });
});

it("danh sach co cong thuc: khong co vi pham axe", async () => {
  m.getMyRecipes.mockResolvedValue({
    items: [row(1, "Draft", null), row(2, "Published", "https://cdn.example.com/a.webp"), row(3, "Draft", null)],
    page: 1, pageSize: 20, totalCount: 3,
  });
  const { container } = render(<DashboardRecipesPage />);
  await screen.findByText("Canh chua 2");

  expect(await axe(container)).toHaveNoViolations();
});

it("danh sach rong: khong co vi pham axe", async () => {
  m.getMyRecipes.mockResolvedValue({ items: [], page: 1, pageSize: 20, totalCount: 0 });
  const { container } = render(<DashboardRecipesPage />);
  await screen.findByText("Bạn chưa có công thức nào.");

  expect(await axe(container)).toHaveNoViolations();
});
