import { fireEvent, render, screen } from "@testing-library/react";
import ImagesStep from "../ImagesStep";
import type { RecipeDetail } from "@/lib/recipe-editor";

/**
 * W5-6 (N2-D1) — thanh tiến trình % khi tải ảnh lên.
 *
 * Before: FE hoàn toàn không có chỗ nào báo tiến trình upload (fetch thuần không có
 * `upload.onprogress`). `uploadImage` giờ dùng XMLHttpRequest và callback `onProgress`.
 * Trong bước upload, `uploadPct` được đặt = 0 ngay trước khi `run()` trả về, nên khi quá trình
 * tải đang chạy thì thanh `role="progressbar"` phải hiện với aria-valuenow đúng giá trị %.
 */

const recipe = () =>
  ({ id: "r1", slug: "canh-chua", title: "Canh chua cá lóc", images: [] }) as unknown as RecipeDetail;

function fileOf(byteSize: number, type: string): File {
  return new File([new Uint8Array(byteSize)], "anh.png", { type });
}

it("hien thanh phan tram khi upload dang chay va tat sau khi xong", async () => {
  // `run` giữ promise treo 150 ms để mô phỏng upload đang chạy (busy chưa bật).
  const run = jest.fn(() => new Promise<boolean>((res) => setTimeout(() => res(true), 150)));
  const { container } = render(<ImagesStep recipe={recipe()} busy={false} run={run} onError={jest.fn()} />);

  const input = screen.getByLabelText("Chọn ảnh");
  fireEvent.change(input, { target: { files: [fileOf(64, "image/png")] } });
  fireEvent.click(screen.getByRole("button", { name: "Tải lên" }));

  // Zod resolver bất đồng bộ: chờ form pass rồi thanh % mới xuất hiện với giá trị 0.
  const bar = await screen.findByRole("progressbar", { name: "Tiến trình tải ảnh lên" });
  expect(bar).toHaveAttribute("aria-valuenow", "0");
  expect(container.querySelector('[style*="width: 0%"]')).not.toBeNull();

  // Xong upload -> thanh biến mất.
  await new Promise((r) => setTimeout(r, 250));
  expect(screen.queryByRole("progressbar", { name: "Tiến trình tải ảnh lên" })).toBeNull();
});