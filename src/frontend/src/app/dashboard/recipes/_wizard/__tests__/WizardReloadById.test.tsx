import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import RecipeWizard from "../RecipeWizard";
import * as api from "@/lib/recipe-editor";
import { ApiError, type RecipeDetail } from "@/lib/recipe-editor";

/**
 * Lỗi tìm ra bằng E2E (e) tuần 4: sửa tiêu đề bản nháp làm đổi slug. Tab thứ hai còn giữ slug cũ -> nạp lại theo slug
 * nhận 404, nút "Tải dữ liệu mới nhất" không nạp được bản mới. Wizard phải nạp lại theo id (không đổi).
 */
jest.mock("next/navigation", () => ({
  useRouter: () => ({ replace: jest.fn(), push: jest.fn(), refresh: jest.fn() }),
}));
jest.mock("@/lib/recipe-editor", () => ({
  ...jest.requireActual("@/lib/recipe-editor"),
  getCategories: jest.fn(),
  getRecipeDetail: jest.fn(),
  updateRecipe: jest.fn(),
  addIngredient: jest.fn(),
}));

const m = api as jest.Mocked<typeof api>;
const base = {
  id: "r1", description: "", instructions: "", prepTimeMinutes: 10, cookTimeMinutes: 10, servings: 2,
  difficulty: "Easy", categoryId: "c1", nutrition: null, status: "Draft", ingredients: [], steps: [], images: [],
};
const mine = { ...base, slug: "tieu-de-cu", title: "Tieu de cu", rowVersion: "v1" } as unknown as RecipeDetail;
// Bản trên server sau khi tab khác đổi tiêu đề: slug mới, rowVersion mới
const latest = { ...base, slug: "tieu-de-moi-cua-a", title: "Tieu de moi cua A", rowVersion: "v2" } as unknown as RecipeDetail;

const notFound = () => new ApiError(404, "recipe.not_found", "Không tìm thấy công thức.");

beforeEach(() => {
  jest.clearAllMocks();
  localStorage.setItem("accessToken", "t");
  m.getCategories.mockResolvedValue([{ id: "c1", name: "Mon chinh" }]);
  // Server chỉ còn tra được theo id; slug cũ đã mất
  m.getRecipeDetail.mockImplementation(key => (key === "r1" ? Promise.resolve(latest) : Promise.reject(notFound())));
  (global as unknown as { fetch: jest.Mock }).fetch = jest.fn().mockResolvedValue({});
});

const start = (step: number) => render(<RecipeWizard initial={{
  step, recipeId: "r1", slug: mine.slug, rowVersion: "v1", detail: mine, info: api.toBasicInfo(mine),
}} />);

it("xung dot -> Tai du lieu moi nhat nap theo id, form hien tieu de moi, luu lai dung rowVersion moi", async () => {
  const user = userEvent.setup();
  m.updateRecipe
    .mockRejectedValueOnce(new ApiError(422, "recipe.version_conflict", "Phiên bản đã thay đổi"))
    .mockResolvedValueOnce({ id: "r1", slug: "tieu-de-cua-b", rowVersion: "v3" });
  start(0);

  const title = screen.getByPlaceholderText("VD: Canh chua cá lóc");
  await user.clear(title);
  await user.type(title, "Tieu de cua B");
  await user.click(screen.getByRole("button", { name: /^Lưu & tiếp/ }));
  await user.click(await screen.findByRole("button", { name: "Tải dữ liệu mới nhất" }));

  await waitFor(() => expect(title).toHaveValue("Tieu de moi cua A"));
  expect(m.getRecipeDetail).toHaveBeenLastCalledWith("r1");
  expect(screen.queryByRole("alert")).toBeNull();

  await user.clear(title);
  await user.type(title, "Tieu de cua B");
  await user.click(screen.getByRole("button", { name: /^Lưu & tiếp/ }));
  await waitFor(() => expect(m.updateRecipe).toHaveBeenCalledTimes(2));
  expect(m.updateRecipe.mock.calls[1][2]).toBe("v2");
});

it("luu nguyen lieu sau khi slug doi o noi khac -> nap lai theo id, khong bao loi", async () => {
  const user = userEvent.setup();
  m.addIngredient.mockResolvedValue(undefined);
  start(1);

  await user.type(screen.getByLabelText("Tên nguyên liệu (dòng 1)"), "Ca loc");
  await user.click(screen.getByRole("button", { name: "Lưu" }));

  await waitFor(() => expect(m.getRecipeDetail).toHaveBeenCalled());
  expect(m.getRecipeDetail).toHaveBeenCalledWith("r1");
  await waitFor(() => expect(screen.queryByText(/hoàn tác|Không tìm thấy/)).toBeNull());
  expect(screen.queryByRole("alert")).toBeNull();
});
