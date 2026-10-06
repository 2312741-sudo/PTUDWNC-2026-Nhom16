import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import RecipeWizard from "../RecipeWizard";
import * as api from "@/lib/recipe-editor";
import type { RecipeDetail, Step } from "@/lib/recipe-editor";

jest.mock("next/navigation", () => ({
  useRouter: () => ({ replace: jest.fn(), push: jest.fn(), refresh: jest.fn() }),
}));
jest.mock("@/lib/recipe-editor", () => ({
  ...jest.requireActual("@/lib/recipe-editor"),
  getCategories: jest.fn(),
  getRecipeDetail: jest.fn(),
  addStep: jest.fn(),
  updateStep: jest.fn(),
  deleteStep: jest.fn(),
  reorderSteps: jest.fn(),
}));

const m = api as jest.Mocked<typeof api>;
const s1: Step = { id: "s1", stepNumber: 1, title: "So che ca", description: "Lam sach ca", timerMinutes: 10 };
const s2: Step = { id: "s2", stepNumber: 2, title: "Nau canh", description: "Dun soi nuoc me", timerMinutes: null };
const detail = (steps: Step[] = [s1, s2]) =>
  ({ id: "r1", slug: "s1", rowVersion: "v1", ingredients: [], steps, images: [] }) as unknown as RecipeDetail;

const start = (steps: Step[] = [s1, s2]) =>
  render(<RecipeWizard initial={{ step: 2, recipeId: "r1", slug: "s1", rowVersion: "v1", detail: detail(steps) }} />);
// Danh sách bước là <ol> cuối cùng (trước nó là <ol> thanh điều hướng các bước)
const titles = () => within(screen.getAllByRole("list").at(-1)!).getAllByRole("listitem").map(li => li.querySelector("p")!.textContent);
const itemOf = (title: string) => within(screen.getByText(title).closest("li")!);

beforeEach(() => {
  jest.clearAllMocks();
  jest.restoreAllMocks();
  localStorage.setItem("accessToken", "t");
  m.getCategories.mockResolvedValue([{ id: "c1", name: "Mon chinh" }]);
  m.getRecipeDetail.mockResolvedValue(detail());
  (global as unknown as { fetch: jest.Mock }).fetch = jest.fn().mockResolvedValue({});
});

describe("StepsStep", () => {
  it("them buoc: goi addStep voi body da trim, timer trong -> null", async () => {
    const user = userEvent.setup();
    m.addStep.mockResolvedValue(undefined);
    start([]);

    await user.type(screen.getByLabelText("Tiêu đề bước (dòng 1)"), "  Nem nem  ");
    await user.type(screen.getByLabelText("Mô tả chi tiết (dòng 1)"), "Cho them muoi");
    await user.click(screen.getByRole("button", { name: "Lưu bước" }));

    await waitFor(() => expect(m.addStep).toHaveBeenCalledTimes(1));
    expect(m.addStep).toHaveBeenCalledWith("r1", { title: "Nem nem", description: "Cho them muoi", timerMinutes: null });
  });

  it("sua buoc: goi updateStep dung step id va giu timer cu", async () => {
    const user = userEvent.setup();
    m.updateStep.mockResolvedValue(undefined);
    start();

    await user.click(itemOf("So che ca").getByRole("button", { name: /^Sửa/ }));
    expect(screen.getByText("Đang sửa bước 1")).toBeInTheDocument();
    const title = screen.getByLabelText("Tiêu đề bước (dòng 2)");
    await user.clear(title);
    await user.type(title, "So che ca loc ");
    await user.click(screen.getByRole("button", { name: "Cập nhật" }));

    await waitFor(() => expect(m.updateStep).toHaveBeenCalledTimes(1));
    expect(m.updateStep).toHaveBeenCalledWith("r1", "s1", { title: "So che ca loc", description: "Lam sach ca", timerMinutes: 10 });
    expect(m.addStep).not.toHaveBeenCalled();
  });

  it("xoa buoc: confirm false khong goi API, confirm true goi deleteStep dung id", async () => {
    const user = userEvent.setup();
    const confirm = jest.spyOn(window, "confirm").mockReturnValueOnce(false).mockReturnValueOnce(true);
    m.deleteStep.mockResolvedValue(undefined);
    m.getRecipeDetail.mockResolvedValue(detail([{ ...s2, stepNumber: 1 }]));
    start();

    await user.click(itemOf("So che ca").getByRole("button", { name: /^Xoá/ }));
    expect(confirm).toHaveBeenLastCalledWith('Xoá bước "So che ca"?');
    expect(m.deleteStep).not.toHaveBeenCalled();
    expect(screen.getByText("So che ca")).toBeInTheDocument();

    await user.click(itemOf("So che ca").getByRole("button", { name: /^Xoá/ }));
    await waitFor(() => expect(m.deleteStep).toHaveBeenCalledWith("r1", "s1"));
    await waitFor(() => expect(screen.queryByText("So che ca")).not.toBeInTheDocument());
    expect(titles()).toEqual(["Nau canh"]);
  });

  it("sap xep lai: dua buoc 1 xuong goi reorderSteps voi thu tu moi", async () => {
    const user = userEvent.setup();
    m.reorderSteps.mockResolvedValue(undefined);
    m.getRecipeDetail.mockResolvedValue(detail([{ ...s2, stepNumber: 1 }, { ...s1, stepNumber: 2 }]));
    start();

    await user.click(screen.getByRole("button", { name: "Đưa bước 1 xuống" }));
    await waitFor(() => expect(m.reorderSteps).toHaveBeenCalledWith("r1", ["s2", "s1"]));
    await waitFor(() => expect(titles()).toEqual(["Nau canh", expect.stringContaining("So che ca")]));
    expect(screen.getByRole("button", { name: "Đưa bước 1 lên" })).toBeDisabled();
  });

  it("sap xep lai loi server: hoan tac ve thu tu cu va bao loi", async () => {
    const user = userEvent.setup();
    m.reorderSteps.mockRejectedValue(new api.ApiError(400, "STEP_ORDER_INVALID", "Thu tu khong hop le"));
    start();

    await user.click(screen.getByRole("button", { name: "Đưa bước 1 xuống" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Thu tu khong hop le — đã hoàn tác thay đổi.");
    expect(titles()).toEqual([expect.stringContaining("So che ca"), "Nau canh"]);
  });
});
