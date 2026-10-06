import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import RecipeWizard from "../RecipeWizard";
import * as api from "@/lib/recipe-editor";
import type { RecipeDetail } from "@/lib/recipe-editor";

jest.mock("next/navigation", () => ({
  useRouter: () => ({ replace: jest.fn(), push: jest.fn(), refresh: jest.fn() }),
}));
jest.mock("@/lib/recipe-editor", () => ({
  ...jest.requireActual("@/lib/recipe-editor"),
  getCategories: jest.fn(),
  createRecipe: jest.fn(),
  getRecipeDetail: jest.fn(),
  addIngredient: jest.fn(),
}));

const m = api as jest.Mocked<typeof api>;
const detail = {
  id: "r1", slug: "s1", rowVersion: "v1", ingredients: [], steps: [], images: [],
} as unknown as RecipeDetail;

beforeEach(() => {
  jest.clearAllMocks();
  localStorage.setItem("accessToken", "t");
  m.getCategories.mockResolvedValue([{ id: "c1", name: "Mon chinh" }]);
  m.getRecipeDetail.mockResolvedValue(detail);
  m.addIngredient.mockResolvedValue(undefined);
  (global as unknown as { fetch: jest.Mock }).fetch = jest.fn().mockResolvedValue({});
});

describe("RecipeWizard buoc 1", () => {
  it("de trong tieu de: hien loi dung o va KHONG goi API", async () => {
    const user = userEvent.setup();
    render(<RecipeWizard />);
    await user.click(screen.getByRole("button", { name: /^L.u & ti.p/ }));
    expect(await screen.findByText(/5 .* 200/)).toBeInTheDocument();
    expect(m.createRecipe).not.toHaveBeenCalled();
  });
});

describe("IngredientsStep trong wizard", () => {
  const start = () => render(<RecipeWizard initial={{ step: 1, recipeId: "r1", slug: "s1", rowVersion: "v1", detail }} />);

  it("con dong nhap chua luu thi chan chuyen buoc", async () => {
    const user = userEvent.setup();
    start();
    await user.type(screen.getByPlaceholderText(/^T.n \*$/), "Ca loc");
    await user.click(screen.getByRole("button", { name: /^Ti.p/ }));
    expect(await screen.findByRole("alert")).toHaveTextContent(/1 d.ng nguy.n li.u ch.a l.u/);
    expect(screen.getByRole("heading", { level: 1 })).toBeInTheDocument();
  });

  it("luu dong hop le goi addIngredient dung 4 field, ten da trim, rong -> null", async () => {
    const user = userEvent.setup();
    start();
    await user.type(screen.getByPlaceholderText(/^T.n \*$/), "  Ca loc  ");
    await user.click(screen.getByRole("button", { name: /^L.u$/ }));
    await waitFor(() => expect(m.addIngredient).toHaveBeenCalledTimes(1));
    expect(m.addIngredient).toHaveBeenCalledWith("r1", { name: "Ca loc", quantity: null, unit: null, notes: null });
  });
});