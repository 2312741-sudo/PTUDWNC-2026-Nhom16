// Schema Zod cho wizard soạn recipe (K05) — khớp validator backend + giới hạn cột DB
import { z } from "zod";
import { BasicInfo, IMAGE_MAX_BYTES, IMAGE_TYPES, IngredientInput, StepInput } from "./recipe-editor";

// Ô number để trống: valueAsNumber cho NaN -> z.number() báo invalid_type, nên mỗi ô có thông báo riêng
const int = (label: string) => z.number({ error: `Vui lòng nhập ${label}` }).int(`${label} phải là số nguyên`);

/** register(..., { setValueAs: emptyToNull }) cho ô số không bắt buộc: "" -> null thay vì NaN */
export const emptyToNull = (v: unknown) => (v === "" || v === null || v === undefined ? null : Number(v));

const nutrient = z.number({ error: "Giá trị dinh dưỡng phải là số" })
  .min(0, "Giá trị dinh dưỡng không được âm")
  .max(999_999.99, "Giá trị dinh dưỡng tối đa 999 999,99")
  .nullable();

export const basicInfoSchema = z.object({
  title: z.string().refine(v => { const n = v.trim().length; return n >= 5 && n <= 200; },
    "Tiêu đề phải từ 5 đến 200 ký tự"),
  description: z.string().max(2000, "Mô tả tối đa 2000 ký tự"),
  instructions: z.string(),
  prepTimeMinutes: int("thời gian sơ chế").min(1, "Thời gian sơ chế phải ≥ 1 phút"),
  cookTimeMinutes: int("thời gian nấu").min(0, "Thời gian nấu không được âm"),
  servings: int("khẩu phần").min(1, "Khẩu phần phải ≥ 1"),
  difficulty: z.enum(["Easy", "Medium", "Hard", "Expert"], { error: "Độ khó không hợp lệ" }),
  categoryId: z.string().min(1, "Vui lòng chọn danh mục"),
  nutrition: z.object({
    calories: nutrient, protein: nutrient, carbohydrates: nutrient,
    fat: nutrient, fiber: nutrient, sodium: nutrient,
  }).nullable(),
});

const optionalText = (max: number, msg: string) =>
  z.string().trim().max(max, msg).transform(v => (v === "" ? null : v));

export const ingredientSchema = z.object({
  name: z.string().trim().min(1, "Vui lòng nhập tên nguyên liệu").max(200, "Tên nguyên liệu tối đa 200 ký tự"),
  quantity: z.number({ error: "Số lượng phải là số" })
    .gt(0, "Số lượng phải lớn hơn 0")
    .max(9_999_999.999, "Số lượng tối đa 9 999 999,999")
    .nullable(),
  unit: optionalText(50, "Đơn vị tối đa 50 ký tự"),
  notes: optionalText(500, "Ghi chú tối đa 500 ký tự"),
});

export const stepSchema = z.object({
  title: z.string().trim().min(1, "Vui lòng nhập tiêu đề bước").max(200, "Tiêu đề bước tối đa 200 ký tự"),
  description: z.string().trim().min(1, "Vui lòng nhập mô tả bước").max(2000, "Mô tả bước tối đa 2000 ký tự"),
  timerMinutes: z.number({ error: "Hẹn giờ phải là số" })
    .int("Hẹn giờ phải là số nguyên")
    .min(0, "Hẹn giờ không được âm")
    .nullable(),
});

export const imageUploadSchema = z.object({
  file: z.file({ error: "Vui lòng chọn ảnh" })
    .refine(f => IMAGE_TYPES.includes(f.type), "Chỉ nhận ảnh JPEG, PNG, WebP hoặc AVIF")
    .max(IMAGE_MAX_BYTES, `Ảnh tối đa ${IMAGE_MAX_BYTES / 1024 / 1024} MiB`),
  altText: optionalText(200, "Mô tả ảnh tối đa 200 ký tự"),
});

export type BasicInfoInput = z.input<typeof basicInfoSchema>;
export type BasicInfoOutput = z.output<typeof basicInfoSchema>;
export type IngredientFormInput = z.input<typeof ingredientSchema>;
export type IngredientFormOutput = z.output<typeof ingredientSchema>;
export type StepFormInput = z.input<typeof stepSchema>;
export type StepFormOutput = z.output<typeof stepSchema>;
export type ImageUploadInput = z.input<typeof imageUploadSchema>;
export type ImageUploadOutput = z.output<typeof imageUploadSchema>;

// Kiểm tra lúc biên dịch: output của resolver khớp kiểu API -> gửi thẳng được, không thừa/thiếu field (JSON strict)
type Same<A, B> = [A] extends [B] ? ([B] extends [A] ? true : false) : false;
type Assert<T extends true> = T;
export type BasicInfoSchemaMatches = Assert<Same<BasicInfoOutput, BasicInfo>>;
export type IngredientSchemaMatches = Assert<Same<IngredientFormOutput, Omit<IngredientInput, "orderIndex">>>;
export type StepSchemaMatches = Assert<Same<StepFormOutput, StepInput>>;
