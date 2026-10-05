import { render, screen, waitFor } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import ImagesStep from "../ImagesStep";
import type { RecipeDetail, RecipeImage } from "@/lib/recipe-editor";

expect.extend(toHaveNoViolations);

/**
 * Xem trước ảnh recipe Draft trong bước 4 (DE_XUAT_05 phương án C).
 * Proxy D27 trả 403 image.forbidden khi không có Bearer, mà <img src> không gửi được header
 * -> ảnh từ proxy phải tải bằng fetch kèm Authorization rồi hiển thị qua blob URL.
 * URL không cần token (/images/..., host khác) vẫn dùng <img src> thường.
 */
const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080/api/v1";
// mediaUrl(key) với NEXT_PUBLIC_MEDIA_URL=<API>/resources/images cho ra đúng dạng này
const PROXY = `${API}/resources/images/recipes/r1/thumb.webp`;

const img = (over: Partial<RecipeImage> = {}): RecipeImage =>
  ({ id: "g1", originalUrl: PROXY, altText: "Bát canh chua", isPrimary: true, orderIndex: 0, ...over });
const recipe = (images: RecipeImage[]) =>
  ({ id: "r1", slug: "canh-chua", title: "Canh chua cá lóc", images }) as unknown as RecipeDetail;
const renderStep = (images: RecipeImage[]) =>
  render(<ImagesStep recipe={recipe(images)} busy={false} run={jest.fn()} onError={jest.fn()} />);

let fetchMock: jest.Mock;
let createObjectURL: jest.Mock;
let revokeObjectURL: jest.Mock;
const ok = () => ({ ok: true, status: 200, blob: () => Promise.resolve(new Blob(["x"], { type: "image/webp" })) });

beforeEach(() => {
  localStorage.setItem("accessToken", "tok-123");
  fetchMock = jest.fn().mockResolvedValue(ok());
  createObjectURL = jest.fn().mockReturnValue("blob:preview-1");
  revokeObjectURL = jest.fn();
  Object.assign(global, { fetch: fetchMock });
  Object.assign(URL, { createObjectURL, revokeObjectURL });
});

it("anh qua proxy duoc tai bang fetch kem Bearer va hien qua blob URL, giu alt", async () => {
  renderStep([img()]);

  const shown = await screen.findByRole("img", { name: "Bát canh chua" });
  await waitFor(() => expect(shown).toHaveAttribute("src", "blob:preview-1"));
  expect(fetchMock).toHaveBeenCalledTimes(1);
  const [url, init] = fetchMock.mock.calls[0];
  expect(url).toBe(PROXY);
  expect(init.headers).toEqual({ Authorization: "Bearer tok-123" });
});

it("khong dua URL proxy vao <img src> truc tiep (tranh 403 -> anh vo)", async () => {
  const { container } = renderStep([img()]);
  expect(container.querySelector(`img[src="${PROXY}"]`)).toBeNull();
  await screen.findByRole("img", { name: "Bát canh chua" });
});

it("proxy tra 403 image.forbidden -> khoi du phong co chu, khong co anh vo", async () => {
  fetchMock.mockResolvedValue({ ok: false, status: 403, blob: () => Promise.resolve(new Blob()) });
  const { container } = renderStep([img()]);

  expect(await screen.findByText(/Không tải được ảnh/)).toBeInTheDocument();
  expect(container.querySelector("img")).toBeNull();
  expect(createObjectURL).not.toHaveBeenCalled();
  expect(await axe(container)).toHaveNoViolations();
});

it("loi mang -> khoi du phong co chu", async () => {
  fetchMock.mockRejectedValue(new TypeError("Failed to fetch"));
  const { container } = renderStep([img()]);

  expect(await screen.findByText(/Không tải được ảnh/)).toBeInTheDocument();
  expect(container.querySelector("img")).toBeNull();
});

it("go component thi revokeObjectURL blob da tao", async () => {
  const { unmount } = renderStep([img()]);
  await waitFor(() => expect(screen.getByRole("img", { name: "Bát canh chua" })).toHaveAttribute("src", "blob:preview-1"));
  expect(revokeObjectURL).not.toHaveBeenCalled();

  unmount();
  expect(revokeObjectURL).toHaveBeenCalledWith("blob:preview-1");
});

it.each([
  ["anh tinh cuc bo", "/images/recipes/canh-chua.webp"],
  ["URL tuyet doi host khac", "https://cdn.example.com/a.webp"],
])("%s khong can token -> <img src> thuong, khong fetch", async (_name, url) => {
  renderStep([img({ originalUrl: url })]);

  expect(screen.getByRole("img", { name: "Bát canh chua" })).toHaveAttribute("src", url);
  expect(fetchMock).not.toHaveBeenCalled();
});

it("khong co altText thi alt la ten cong thuc; da tai xong khong co vi pham axe", async () => {
  const { container } = renderStep([img({ altText: null })]);
  await waitFor(() => expect(screen.getByRole("img", { name: "Canh chua cá lóc" })).toHaveAttribute("src", "blob:preview-1"));
  expect(await axe(container)).toHaveNoViolations();
});
