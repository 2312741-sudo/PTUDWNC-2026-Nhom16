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

// K18 NVDA lỗi 2 (K18_nvda_speech_log.txt mục B): `Sửa<span className="sr-only"> nguyên liệu X</span>` bị đọc dính
// "Sửanguyên liệu cá viên" -> tên đầy đủ đặt ở aria-label, chữ nhìn thấy vẫn là "Sửa"/"Xoá" và đứng đầu tên (WCAG 2.5.3)
describe("nut lap lai dung aria-label, khong dung span sr-only", () => {
  const expectLabel = (visible: string, label: string) => {
    const b = screen.getByRole("button", { name: label });
    expect(b).toHaveAttribute("aria-label", label);
    expect(b).toHaveTextContent(new RegExp(`^${visible}$`));
    expect(label.startsWith(visible)).toBe(true);
  };

  it("buoc Nguyen lieu", async () => {
    await start(1);
    expectLabel("Sửa", "Sửa nguyên liệu Ca loc");
    expectLabel("Xoá", "Xoá nguyên liệu Ca loc");
  });

  it("buoc Cac buoc", async () => {
    await start(2);
    expectLabel("Sửa", "Sửa bước 1: So che");
    expectLabel("Xoá", "Xoá bước 1: So che");
  });

  it("buoc Anh: kem so thu tu, kem alt neu co", async () => {
    const images = [
      { id: "g1", originalUrl: "https://cdn.example.com/a.webp", altText: "Bat canh chua", isPrimary: true, orderIndex: 0 },
      { id: "g2", originalUrl: "https://cdn.example.com/b.webp", altText: null, isPrimary: false, orderIndex: 1 },
    ];
    render(<RecipeWizard initial={{ step: 3, recipeId: "r1", slug: "s1", rowVersion: "v1", detail: { ...detail, images } as RecipeDetail }} />);
    await act(() => Promise.resolve());
    expectLabel("Xoá", "Xoá ảnh 1: Bat canh chua");
    expectLabel("Đặt làm ảnh chính", "Đặt làm ảnh chính (ảnh 2)");
    expectLabel("Xoá", "Xoá ảnh 2");
  });
});

// K18 NVDA lỗi 1 (K18_nvda_speech_log.txt mục A): bộ đếm (N/2000) nằm trong <label> -> mỗi phím NVDA đọc lại "Mô tả (N/2000)"
// K18 N16 (NVDA 09/10/2026, build C9-tuan6): dù đưa bộ đếm ra ngoài label và không gắn aria-live, NVDA vẫn đọc lại
// "n/2000 ký tự" sau MỖI phím (kể cả Backspace) -> nguyên nhân là bộ đếm được nối vào ô qua aria-describedby và nội
// dung đổi theo từng phím, trình duyệt báo "mô tả đã đổi" cho ô đang focus bất kể có aria-live hay không (cùng cơ chế
// GOV.UK Design System character-count phải xử lý). Sửa: bộ đếm hiện số chỉ còn hiển thị thị giác (aria-hidden), ô
// nối describedby tới một câu tĩnh không đổi khi gõ; vùng polite riêng vẫn chỉ đổi chữ ở ngưỡng 90%/100%.
describe("bo dem o Mo ta khong bi doc lai sau moi phim (N16)", () => {
  const openStep1 = async () => { render(<RecipeWizard />); await act(() => Promise.resolve()); };
  const desc = () => screen.getByRole("textbox", { name: "Mô tả" });
  const limit = () => document.getElementById("description-limit")!;

  it("ten truy cap luon la 'Mo ta' khi go; bo dem so an khoi AT, textarea noi describedby toi cau tinh", async () => {
    const user = userEvent.setup();
    await openStep1();
    for (const ch of "can") {
      await user.type(desc(), ch);
      expect(desc()).toHaveAccessibleName("Mô tả");
    }
    const counter = screen.getByText(/^3\/2000/);
    expect(counter.closest("label")).toBeNull();
    expect(counter).toHaveAttribute("aria-hidden", "true");
    // bo dem so KHONG con trong describedby (la nguyen nhan N16) -> textarea noi toi cau tinh "Toi da N ky tu."
    expect(desc().getAttribute("aria-describedby")?.split(" ")).not.toContain(counter.id);
    const hint = document.getElementById("description-hint")!;
    expect(desc().getAttribute("aria-describedby")?.split(" ")).toContain(hint.id);
    expect(hint).toHaveTextContent("Tối đa 2000 ký tự.");
    expect(counter.closest("[aria-live]")).toBeNull();
    expect(limit()).toHaveAttribute("aria-live", "polite");
    expect(limit()).toHaveAttribute("role", "status");
    expect(limit()).toHaveAttribute("aria-atomic", "true");
    expect(limit()).toBeEmptyDOMElement();
  });

  it("chi thong bao polite khi dat 90% (1800) va 100% (2000), khong doi theo tung phim giua hai nguong", async () => {
    const user = userEvent.setup();
    await openStep1();
    await user.click(desc());
    await user.paste("a".repeat(1800));
    expect(limit()).toHaveTextContent("Mô tả còn dưới 200 ký tự.");
    const node = limit().firstChild;
    await user.type(desc(), "bb");
    expect(limit().firstChild).toBe(node); // cùng một nút chữ, không đổi nội dung -> trình đọc không đọc lại

    await user.paste("c".repeat(198));
    expect(screen.getByText(/^2000\/2000/)).toBeInTheDocument();
    expect(limit()).toHaveTextContent("Mô tả đã đạt giới hạn 2000 ký tự.");
  });
});
