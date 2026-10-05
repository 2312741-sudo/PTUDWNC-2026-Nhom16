import { render, screen, waitFor, within } from "@testing-library/react";
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
  getRecipeDetail: jest.fn(),
  addIngredient: jest.fn(),
  updateIngredient: jest.fn(),
  deleteIngredient: jest.fn(),
}));

const m = api as jest.Mocked<typeof api>;
const caLoc = { id: "i1", name: "Ca loc", quantity: 500, unit: "g", notes: null, orderIndex: 0 };
const detail = (ingredients = [caLoc]) =>
  ({ id: "r1", slug: "s1", rowVersion: "v1", ingredients, steps: [], images: [] }) as unknown as RecipeDetail;

const start = () => render(<RecipeWizard initial={{ step: 1, recipeId: "r1", slug: "s1", rowVersion: "v1", detail: detail() }} />);
const table = () => screen.getByRole("table");
const editorOf = (text: RegExp) => within(screen.getByText(text).parentElement!);

beforeEach(() => {
  jest.clearAllMocks();
  jest.restoreAllMocks();
  localStorage.setItem("accessToken", "t");
  m.getCategories.mockResolvedValue([{ id: "c1", name: "Mon chinh" }]);
  m.getRecipeDetail.mockResolvedValue(detail());
  (global as unknown as { fetch: jest.Mock }).fetch = jest.fn().mockResolvedValue({});
});

describe("IngredientsStep: sua nguyen lieu", () => {
  it("Cap nhat goi updateIngredient dung recipe id, ingredient id va body da trim", async () => {
    const user = userEvent.setup();
    m.updateIngredient.mockResolvedValue(undefined);
    m.getRecipeDetail.mockResolvedValue(detail([{ ...caLoc, name: "Ca loc dong", quantity: 600 }]));
    start();

    await user.click(within(table()).getByRole("button", { name: /^Sửa/ }));
    const name = screen.getByLabelText("Tên nguyên liệu (dòng 2)");
    expect(name).toHaveValue("Ca loc");
    await user.clear(name);
    await user.type(name, "  Ca loc dong  ");
    const qty = screen.getByLabelText("Số lượng (dòng 2)");
    await user.clear(qty);
    await user.type(qty, "600");
    await user.click(screen.getByRole("button", { name: "Cập nhật" }));

    await waitFor(() => expect(m.updateIngredient).toHaveBeenCalledTimes(1));
    expect(m.updateIngredient).toHaveBeenCalledWith("r1", "i1", { name: "Ca loc dong", quantity: 600, unit: "g", notes: null });
    expect(m.addIngredient).not.toHaveBeenCalled();
    expect(await within(table()).findByText("Ca loc dong")).toBeInTheDocument();
    expect(screen.queryByText(/^Đang sửa:/)).not.toBeInTheDocument();
  });

  it("mo Sua khong doi gi van chan chuyen buoc cho toi khi bam Bo", async () => {
    const user = userEvent.setup();
    start();

    await user.click(within(table()).getByRole("button", { name: /^Sửa/ }));
    await user.click(screen.getByRole("button", { name: /^Ti.p/ }));
    expect(await screen.findByRole("alert")).toHaveTextContent(/1 dòng nguyên liệu chưa lưu/);
    expect(screen.getByLabelText("Tên nguyên liệu (dòng 2)")).toBeInTheDocument(); // vẫn ở bước 2

    await user.click(editorOf(/^Đang sửa: Ca loc/).getByRole("button", { name: "Bỏ" }));
    await user.click(screen.getByRole("button", { name: /^Ti.p/ }));
    expect(await screen.findByLabelText("Tiêu đề bước (dòng 1)")).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    expect(m.updateIngredient).not.toHaveBeenCalled();
  });

  it("server loi khi cap nhat: hoan tac ten tren bang va tra lai dong dang sua", async () => {
    const user = userEvent.setup();
    m.updateIngredient.mockRejectedValue(new api.ApiError(400, "validation", "Ten khong hop le"));
    start();

    await user.click(within(table()).getByRole("button", { name: /^Sửa/ }));
    const name = screen.getByLabelText("Tên nguyên liệu (dòng 2)");
    await user.clear(name);
    await user.type(name, "Ten moi");
    await user.click(screen.getByRole("button", { name: "Cập nhật" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Ten khong hop le — đã hoàn tác thay đổi.");
    expect(within(table()).getByText("Ca loc")).toBeInTheDocument();
    expect(within(table()).queryByText("Ten moi")).not.toBeInTheDocument();
    expect(screen.getByText(/^Đang sửa: Ca loc/)).toBeInTheDocument();
    expect(screen.getByDisplayValue("Ten moi")).toBeInTheDocument();
  });
});

describe("IngredientsStep: xoa nguyen lieu", () => {
  it("confirm tra false: khong goi deleteIngredient, dong van con", async () => {
    const user = userEvent.setup();
    const confirm = jest.spyOn(window, "confirm").mockReturnValue(false);
    start();

    await user.click(within(table()).getByRole("button", { name: /^Xoá/ }));
    expect(confirm).toHaveBeenCalledWith('Xoá nguyên liệu "Ca loc"?');
    expect(m.deleteIngredient).not.toHaveBeenCalled();
    expect(within(table()).getByText("Ca loc")).toBeInTheDocument();
  });

  it("confirm tra true: goi deleteIngredient dung id va dong bien mat", async () => {
    const user = userEvent.setup();
    jest.spyOn(window, "confirm").mockReturnValue(true);
    m.deleteIngredient.mockResolvedValue(undefined);
    m.getRecipeDetail.mockResolvedValue(detail([]));
    start();

    await user.click(within(table()).getByRole("button", { name: /^Xoá/ }));
    await waitFor(() => expect(m.deleteIngredient).toHaveBeenCalledWith("r1", "i1"));
    expect(await screen.findByText("Chưa có nguyên liệu nào.")).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("server loi khi xoa: hoan tac, dong quay lai bang", async () => {
    const user = userEvent.setup();
    jest.spyOn(window, "confirm").mockReturnValue(true);
    m.deleteIngredient.mockRejectedValue(new api.ApiError(500, undefined, "Loi may chu"));
    start();

    await user.click(within(table()).getByRole("button", { name: /^Xoá/ }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Loi may chu — đã hoàn tác thay đổi.");
    expect(within(table()).getByText("Ca loc")).toBeInTheDocument();
  });
});

describe("IngredientsStep: them moi loi server", () => {
  it("hoan tac dong tam tren bang va tra lai dong nhap de sua tiep", async () => {
    const user = userEvent.setup();
    m.addIngredient.mockRejectedValue(new api.ApiError(400, "validation", "Ten trung"));
    m.getRecipeDetail.mockResolvedValue(detail([]));
    render(<RecipeWizard initial={{ step: 1, recipeId: "r1", slug: "s1", rowVersion: "v1", detail: detail([]) }} />);

    await user.type(screen.getByLabelText("Tên nguyên liệu (dòng 1)"), "Me chua");
    await user.click(screen.getByRole("button", { name: "Lưu" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Ten trung — đã hoàn tác thay đổi.");
    expect(screen.getByText("Chưa có nguyên liệu nào.")).toBeInTheDocument();
    expect(screen.getByLabelText("Tên nguyên liệu (dòng 1)")).toHaveValue("Me chua");
  });
});
