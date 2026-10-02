import { act, render, screen, waitFor } from "@testing-library/react";
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
}));

/** K18 — kiểm tra tự động phần a11y làm được bằng jsdom; NVDA/zoom/màu kiểm tay theo LAB_K18_checklist.md */
const m = api as jest.Mocked<typeof api>;
const detail = {
  id: "r1", slug: "s1", rowVersion: "v1", images: [],
  ingredients: [{ id: "i1", name: "Ca loc", quantity: 500, unit: "g", notes: null, orderIndex: 0 }],
  steps: [{ id: "s1", stepNumber: 1, title: "So che", description: "Lam sach", timerMinutes: null }],
} as unknown as RecipeDetail;
// Chờ getCategories resolve trong act để không có cảnh báo cập nhật state ngoài act
const start = async (step: number) => {
  render(<RecipeWizard initial={{ step, recipeId: "r1", slug: "s1", rowVersion: "v1", detail }} />);
  await act(() => Promise.resolve());
};

beforeEach(() => {
  jest.clearAllMocks();
  localStorage.setItem("accessToken", "t");
  m.getCategories.mockResolvedValue([{ id: "c1", name: "Mon chinh" }]);
  m.getRecipeDetail.mockResolvedValue(detail);
  (global as unknown as { fetch: jest.Mock }).fetch = jest.fn().mockResolvedValue({});
});

it("luc mo wizard khong cuop focus; chuyen buoc thi focus toi tieu de buoc moi", async () => {
  const user = userEvent.setup();
  await start(1);
  const heading = screen.getByRole("heading", { level: 2, name: "Bước 2/5: Nguyên liệu" });
  expect(heading).not.toHaveFocus();

  await user.click(screen.getByRole("button", { name: /^Ti.p/ }));
  expect(screen.getByRole("heading", { level: 2, name: "Bước 3/5: Các bước" })).toHaveFocus();
});

it("nut Sua/Xoa lap lai co ngu canh rieng cho trinh doc man hinh", async () => {
  await start(1);
  expect(screen.getByRole("button", { name: "Sửa nguyên liệu Ca loc" })).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Xoá nguyên liệu Ca loc" })).toBeInTheDocument();
});

it("nut Sua/Xoa buoc co so thu tu va tieu de buoc", async () => {
  await start(2);
  expect(screen.getByRole("button", { name: "Sửa bước 1: So che" })).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Xoá bước 1: So che" })).toBeInTheDocument();
});

it("dang luu duoc thong bao qua role=status, xong thi xoa thong bao", async () => {
  const user = userEvent.setup();
  let finish!: () => void;
  m.addIngredient.mockReturnValue(new Promise(r => { finish = () => r(undefined); }));
  await start(1);

  await user.type(screen.getByLabelText("Tên nguyên liệu (dòng 1)"), "Me chua");
  await user.click(screen.getByRole("button", { name: "Lưu" }));
  expect(screen.getByRole("status")).toHaveTextContent("Đang lưu…");

  finish();
  await waitFor(() => expect(screen.getByRole("status")).toBeEmptyDOMElement());
});

it("o loi co aria-invalid va aria-describedby tro toi thong bao loi", async () => {
  const user = userEvent.setup();
  await start(1);
  const qty = screen.getByLabelText("Số lượng (dòng 1)");
  await user.type(screen.getByLabelText("Tên nguyên liệu (dòng 1)"), "Muoi");
  await user.type(qty, "-1");
  await user.click(screen.getByRole("button", { name: "Lưu" }));

  await waitFor(() => expect(qty).toHaveAttribute("aria-invalid", "true"));
  expect(qty).toHaveAccessibleDescription("Số lượng phải lớn hơn 0");
  expect(m.addIngredient).not.toHaveBeenCalled();
});
