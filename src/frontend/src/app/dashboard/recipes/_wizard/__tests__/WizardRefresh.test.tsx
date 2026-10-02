import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import RecipeWizard from "../RecipeWizard";
import * as api from "@/lib/recipe-editor";
import type { RecipeDetail } from "@/lib/recipe-editor";

const router = { replace: jest.fn(), push: jest.fn(), refresh: jest.fn() };
jest.mock("next/navigation", () => ({ useRouter: () => router }));
jest.mock("@/lib/recipe-editor", () => ({
  ...jest.requireActual("@/lib/recipe-editor"),
  getCategories: jest.fn(),
  createRecipe: jest.fn(),
  getRecipeDetail: jest.fn(),
  addIngredient: jest.fn(),
}));

/**
 * Lỗi đã gặp ở E2E: tạo mới ở /dashboard/recipes/new, wizard đổi URL sang /{id}/edit bằng history.replaceState;
 * router.refresh() sau đó render route edit -> EditRecipeClient mount lại -> wizard nhảy về bước 1, mất state.
 */
const m = api as jest.Mocked<typeof api>;
const detail = { id: "r1", slug: "canh-chua", rowVersion: "v1", ingredients: [], steps: [], images: [] } as unknown as RecipeDetail;
const fetchMock = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  localStorage.setItem("accessToken", "t");
  m.getCategories.mockResolvedValue([{ id: "c1", name: "Mon chinh" }]);
  m.getRecipeDetail.mockResolvedValue(detail);
  m.createRecipe.mockResolvedValue({ id: "r1", slug: "canh-chua", rowVersion: "v1" });
  m.addIngredient.mockResolvedValue(undefined);
  fetchMock.mockResolvedValue({});
  (global as unknown as { fetch: jest.Mock }).fetch = fetchMock;
});

it("tao moi o /new: sau khi URL doi sang /edit thi KHONG goi router.refresh (tranh mount lai ve buoc 1)", async () => {
  window.history.replaceState(null, "", "/dashboard/recipes/new");
  const user = userEvent.setup();
  render(<RecipeWizard />);
  await screen.findByRole("option", { name: "Mon chinh" });

  await user.type(screen.getByPlaceholderText("VD: Canh chua cá lóc"), "Canh chua ca loc");
  await user.selectOptions(screen.getByLabelText(/Danh mục/), "c1");
  await user.click(screen.getByRole("button", { name: /^Lưu & tiếp/ }));

  expect(await screen.findByLabelText("Tên nguyên liệu (dòng 1)")).toBeInTheDocument();
  expect(window.location.pathname).toBe("/dashboard/recipes/r1/edit");
  await waitFor(() => expect(fetchMock).toHaveBeenCalledWith("/api/revalidate", expect.anything()));
  await new Promise(r => setTimeout(r, 0)); // cho .finally() của refreshPublic chạy
  expect(router.refresh).not.toHaveBeenCalled();

  // Lưu tiếp ở bước 2 cũng không được refresh vì route Next đang giữ là /new
  await user.type(screen.getByLabelText("Tên nguyên liệu (dòng 1)"), "Ca loc");
  await user.click(screen.getByRole("button", { name: "Lưu" }));
  await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
  await new Promise(r => setTimeout(r, 0));
  expect(router.refresh).not.toHaveBeenCalled();
});

it("dang o trang /edit (URL khong doi pathname): van goi router.refresh de lam moi cache", async () => {
  window.history.replaceState(null, "", "/dashboard/recipes/r1/edit?slug=canh-chua");
  const user = userEvent.setup();
  render(<RecipeWizard initial={{ step: 1, recipeId: "r1", slug: "canh-chua", rowVersion: "v1", detail }} />);

  await user.type(screen.getByLabelText("Tên nguyên liệu (dòng 1)"), "Ca loc");
  await user.click(screen.getByRole("button", { name: "Lưu" }));
  await waitFor(() => expect(router.refresh).toHaveBeenCalledTimes(1));
});
