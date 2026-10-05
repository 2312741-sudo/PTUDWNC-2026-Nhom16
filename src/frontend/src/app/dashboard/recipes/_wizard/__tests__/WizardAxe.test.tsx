import { act, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { axe, toHaveNoViolations } from "jest-axe";
import RecipeWizard from "../RecipeWizard";
import * as api from "@/lib/recipe-editor";
import type { RecipeDetail } from "@/lib/recipe-editor";

expect.extend(toHaveNoViolations);

jest.mock("next/navigation", () => ({
  useRouter: () => ({ replace: jest.fn(), push: jest.fn(), refresh: jest.fn() }),
}));
jest.mock("@/lib/recipe-editor", () => ({
  ...jest.requireActual("@/lib/recipe-editor"),
  getCategories: jest.fn(),
  getRecipeDetail: jest.fn(),
  addIngredient: jest.fn(),
}));

/**
 * K18 — kiểm a11y tự động bằng axe-core (jest-axe) cho cả 5 bước của wizard.
 * axe chạy trên jsdom: KHÔNG đo tương phản màu, bố cục 320/768/1200, focus nhìn thấy được, và KHÔNG thay kiểm tay bằng NVDA (LAB_K18_checklist.md).
 */
const m = api as jest.Mocked<typeof api>;
const detail = {
  id: "r1", slug: "canh-chua", rowVersion: "v1", title: "Canh chua cá lóc", description: "Canh chua miền Tây", instructions: "",
  prepTimeMinutes: 15, cookTimeMinutes: 20, servings: 4, difficulty: "Easy", categoryId: "c1", status: "Draft", nutrition: null,
  ingredients: [
    { id: "i1", name: "Cá lóc", quantity: 500, unit: "g", notes: null, orderIndex: 0 },
    { id: "i2", name: "Me chua", quantity: 50, unit: "g", notes: "dầm lấy nước", orderIndex: 1 },
  ],
  steps: [
    { id: "s1", stepNumber: 1, title: "Sơ chế", description: "Làm sạch cá", timerMinutes: 10 },
    { id: "s2", stepNumber: 2, title: "Nấu", description: "Đun sôi nước me", timerMinutes: null },
  ],
  images: [{ id: "g1", originalUrl: "https://cdn.example.com/a.webp", altText: "Bát canh chua", isPrimary: true, orderIndex: 0 }],
} as unknown as RecipeDetail;

beforeEach(() => {
  jest.clearAllMocks();
  localStorage.setItem("accessToken", "t");
  m.getCategories.mockResolvedValue([{ id: "c1", name: "Món canh" }]);
  m.getRecipeDetail.mockResolvedValue(detail);
});

const STEPS = ["1. Thông tin cơ bản", "2. Nguyên liệu", "3. Các bước", "4. Ảnh", "5. Xem lại & Xuất bản"];

it.each(STEPS.map((name, step) => [name, step] as const))("buoc %s khong co vi pham axe", async (_name, step) => {
  const { container } = render(
    step === 0
      ? <RecipeWizard />
      : <RecipeWizard initial={{ step, recipeId: "r1", slug: "canh-chua", rowVersion: "v1", detail }} />);
  await act(() => Promise.resolve()); // chờ danh mục tải xong

  expect(await axe(container)).toHaveNoViolations();
});

describe("trang thai loi cung phai khong co vi pham axe", () => {
  it("buoc 1 sau khi bam Luu voi tieu de trong (loi duoi tung o)", async () => {
    const user = userEvent.setup();
    const { container } = render(<RecipeWizard />);
    await act(() => Promise.resolve());
    await user.click(screen.getByRole("button", { name: /^Lưu & tiếp/ }));
    await screen.findByText(/5 .* 200/);

    expect(await axe(container)).toHaveNoViolations();
  });

  it("buoc 2 voi o so luong sai va banner loi server", async () => {
    const user = userEvent.setup();
    m.addIngredient.mockRejectedValue(new api.ApiError(422, "recipe.concurrency_conflict", "Xung đột"));
    const { container } = render(<RecipeWizard initial={{ step: 1, recipeId: "r1", slug: "canh-chua", rowVersion: "v1", detail }} />);
    await act(() => Promise.resolve());
    await user.type(screen.getByLabelText("Tên nguyên liệu (dòng 1)"), "Muối");
    await user.type(screen.getByLabelText("Số lượng (dòng 1)"), "-1");
    await user.click(screen.getByRole("button", { name: "Lưu" }));
    await screen.findByText("Số lượng phải lớn hơn 0");
    expect(await axe(container)).toHaveNoViolations();

    await user.clear(screen.getByLabelText("Số lượng (dòng 1)"));
    await user.click(screen.getByRole("button", { name: "Lưu" }));
    await screen.findByRole("button", { name: "Tải dữ liệu mới nhất" });
    expect(await axe(container)).toHaveNoViolations();
  });
});
