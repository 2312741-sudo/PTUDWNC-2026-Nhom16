// API helper cho wizard tạo/sửa recipe (C4)
const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080/api/v1";

export type Difficulty = "Easy" | "Medium" | "Hard" | "Expert";
export const DIFFICULTIES: { value: Difficulty; label: string }[] = [
  { value: "Easy", label: "Dễ" },
  { value: "Medium", label: "Trung bình" },
  { value: "Hard", label: "Khó" },
  { value: "Expert", label: "Chuyên gia" },
];

export interface Category { id: string; name: string }

export interface Nutrition {
  calories: number | null; protein: number | null; carbohydrates: number | null;
  fat: number | null; fiber: number | null; sodium: number | null;
}

export interface BasicInfo {
  title: string;
  description: string;
  instructions: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: Difficulty;
  categoryId: string;
  nutrition: Nutrition | null;
}

export interface Ingredient {
  id: string; name: string; quantity: number | null;
  unit: string | null; notes: string | null; orderIndex: number;
}
export interface IngredientInput {
  name: string; quantity: number | null; unit: string | null; notes: string | null; orderIndex?: number;
}

export interface Step {
  id: string; stepNumber: number; title: string; description: string; timerMinutes: number | null;
}
export interface StepInput { title: string; description: string; timerMinutes: number | null }

export interface RecipeDetail {
  id: string; slug: string; title: string; description: string; instructions: string | null;
  prepTimeMinutes: number; cookTimeMinutes: number; servings: number;
  difficulty: Difficulty; categoryId: string; status?: string;
  nutrition: Nutrition | null; rowVersion: string;
  ingredients: Ingredient[]; steps: Step[]; images?: RecipeImage[];
}

export interface SavedRecipe { id: string; slug: string; rowVersion: string; status?: string }

export class ApiError extends Error {
  constructor(public status: number, public code: string | undefined, message: string) {
    super(message);
  }
}
export class UnauthorizedError extends ApiError {}

// B1 (issue #20): API đã trả 503 + code "storage.unavailable" thay vì 500 server.error.
// Thông điệp tiếng Việt đặt ở API, nhưng map lại ở FE để không phụ thuộc vào Title/Detail của API
// (nếu gặp API cũ hoặc reverse proxy nuốt body, người dùng vẫn thấy đúng câu).
// Quyết định 30/09: KHÔNG retry tự động — chỉ thông báo rõ lỗi tạm thời.
const CODE_MESSAGES: Record<string, string> = {
  "storage.unavailable": "Dịch vụ lưu trữ ảnh tạm thời không khả dụng. Vui lòng thử lại sau.",
};

function errorMessage(body: any, status: number): string {
  // ValidationProblemDetails: { errors: { Title: ["..."] } }
  if (body?.errors && typeof body.errors === "object") {
    const msgs = Object.values(body.errors).flat().filter(Boolean) as string[];
    if (msgs.length) return msgs.join(" • ");
  }
  const code = body?.code ?? body?.error?.code;
  if (code && CODE_MESSAGES[code]) return CODE_MESSAGES[code];
  return body?.message ?? body?.error?.message ?? body?.detail ?? body?.title ?? `Lỗi ${status}`;
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = typeof window !== "undefined" ? localStorage.getItem("accessToken") : null;
  const res = await fetch(`${API}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  });
  if (res.status === 401) throw new UnauthorizedError(401, "UNAUTHORIZED", "Phiên đăng nhập hết hạn");
  const body = res.status === 204 ? null : await res.json().catch(() => null);
  if (!res.ok) throw new ApiError(res.status, body?.code ?? body?.error?.code, errorMessage(body, res.status));
  return (body?.data ?? body) as T;
}

// Backend nhận Difficulty dạng số (enum), trả về dạng chuỗi
const DIFFICULTY_NUM: Record<Difficulty, number> = { Easy: 1, Medium: 2, Hard: 3, Expert: 4 };
export const NUTRITION_FIELDS: { key: keyof Nutrition; label: string; unit: string }[] = [
  { key: "calories", label: "Năng lượng", unit: "kcal" },
  { key: "protein", label: "Đạm", unit: "g" },
  { key: "carbohydrates", label: "Tinh bột", unit: "g" },
  { key: "fat", label: "Chất béo", unit: "g" },
  { key: "fiber", label: "Chất xơ", unit: "g" },
  { key: "sodium", label: "Natri", unit: "mg" },
];
// NutritionDto: gửi đủ 6 field (JSON strict); tất cả trống -> null
const normNutrition = (n: Nutrition | null): Nutrition | null => {
  if (!n) return null;
  const full = Object.fromEntries(NUTRITION_FIELDS.map(f => [f.key, n[f.key] ?? null])) as unknown as Nutrition;
  return Object.values(full).every(v => v === null) ? null : full;
};
const toPayload = (i: BasicInfo) => ({ ...i, difficulty: DIFFICULTY_NUM[i.difficulty], nutrition: normNutrition(i.nutrition) });

export function toBasicInfo(d: RecipeDetail): BasicInfo {
  return {
    title: d.title, description: d.description ?? "", instructions: d.instructions ?? "",
    prepTimeMinutes: d.prepTimeMinutes, cookTimeMinutes: d.cookTimeMinutes, servings: d.servings,
    difficulty: d.difficulty, categoryId: d.categoryId, nutrition: d.nutrition ?? null,
  };
}

const json = (method: string, body?: unknown): RequestInit =>
  ({ method, body: body === undefined ? undefined : JSON.stringify(body) });

// ---- Recipe
export const getCategories = () => request<Category[]>("/categories");
export const getRecipeDetail = (slug: string) => request<RecipeDetail>(`/recipes/${slug}`);
export const createRecipe = (info: BasicInfo) => request<SavedRecipe>("/recipes", json("POST", toPayload(info)));
export const updateRecipe = (id: string, info: BasicInfo, rowVersion: string) =>
  request<SavedRecipe>(`/recipes/${id}`, json("PUT", { ...toPayload(info), rowVersion }));
export const publishRecipe = (id: string) => request<unknown>(`/recipes/${id}/publish`, json("PATCH"));

// Body phải khớp CHÍNH XÁC record backend (JSON strict: không thừa, không thiếu field)
// IngredientBody(Name, Quantity, Unit, Notes) — StepBody(Title, Description, TimerMinutes, ImageUrl)
const ingBody = (i: IngredientInput) => ({ name: i.name, quantity: i.quantity, unit: i.unit, notes: i.notes });
const stepBody = (s: StepInput) => ({ title: s.title, description: s.description, timerMinutes: s.timerMinutes, imageUrl: null });

// ---- Ingredients
export const addIngredient = (id: string, i: IngredientInput) =>
  request<unknown>(`/recipes/${id}/ingredients`, json("POST", ingBody(i)));
export const updateIngredient = (id: string, ingId: string, i: IngredientInput) =>
  request<unknown>(`/recipes/${id}/ingredients/${ingId}`, json("PUT", ingBody(i)));
export const deleteIngredient = (id: string, ingId: string) =>
  request<unknown>(`/recipes/${id}/ingredients/${ingId}`, json("DELETE"));

// ---- Steps
export const addStep = (id: string, s: StepInput) => request<unknown>(`/recipes/${id}/steps`, json("POST", stepBody(s)));
export const updateStep = (id: string, stepId: string, s: StepInput) =>
  request<unknown>(`/recipes/${id}/steps/${stepId}`, json("PUT", stepBody(s)));
export const deleteStep = (id: string, stepId: string) =>
  request<unknown>(`/recipes/${id}/steps/${stepId}`, json("DELETE"));
export const reorderSteps = (id: string, orderedStepIds: string[]) =>
  request<unknown>(`/recipes/${id}/steps/reorder`, json("PATCH", { orderedStepIds }));

// ================================================================ Ảnh (hợp đồng TV4 — docs/IMAGE_CONTRACT.md)
export interface RecipeImage {
  id: string;
  originalUrl?: string | null; mediumUrl?: string | null; thumbnailUrl?: string | null; url?: string | null;
  altText?: string | null; isPrimary: boolean; orderIndex: number;
  /**
   * B5 (TV4, PA-3): URL có chữ ký cho ảnh PRIVATE (recipe chưa Published).
   * Proxy ảnh chỉ cho owner/Admin nên thẻ <img> (không gửi header Bearer) cần URL này mới tải được.
   * Hết hạn sau ~10 phút → phải PATCH lại ảnh để lấy URL mới. Luôn null với ảnh public.
   */
  presignedUrl?: string | null;
}

export const IMAGE_TYPES = ["image/jpeg", "image/png", "image/webp", "image/avif"];
export const IMAGE_MAX_BYTES = 5 * 1024 * 1024; // 5 MiB (FR-FILE-001)

const MEDIA = process.env.NEXT_PUBLIC_MEDIA_URL;
/** originalUrl là KEY MinIO (recipes/{id}/{uuid}.ext) hoặc đường dẫn ảnh tĩnh cục bộ (/images/...) — ghép với NEXT_PUBLIC_MEDIA_URL để ra URL trình duyệt nếu là key MinIO. */
export function mediaUrl(key?: string | null): string | null {
  if (!key) return null;
  if (/^https?:\/\//i.test(key)) return key;
  if (key.startsWith('/images/') || key.startsWith('images/')) {
    return key.startsWith('/') ? key : `/${key}`;
  }
  return MEDIA ? `${MEDIA.replace(/\/$/, "")}/${key.replace(/^\//, "")}` : (key.startsWith('/') ? key : `/${key}`);
}
/**
 * B5: ưu tiên URL có chữ ký khi ảnh còn private. `mediaUrl` trả nguyên URL tuyệt đối,
 * nên không cần ghép NEXT_PUBLIC_MEDIA_URL cho trường này.
 */
export const imageSrc = (i: RecipeImage) =>
  mediaUrl(i.presignedUrl ?? i.thumbnailUrl ?? i.mediumUrl ?? i.originalUrl ?? i.url);

/** Chặn dùng URL ký sắp hết hạn (bù độ trễ mạng/render) — 20 giây. */
const PRESIGNED_SAFETY_MS = 20_000;

/**
 * B5: thời điểm hết hạn (ms) của URL ký, đọc từ X-Amz-Date + X-Amz-Expires.
 * Trả null nếu không phải URL ký hoặc thiếu tham số → coi như không hết hạn.
 */
export function presignedExpiresAt(url?: string | null): number | null {
  if (!url || !/^https?:\/\//i.test(url)) return null;
  const date = /[?&]X-Amz-Date=(\d{8}T\d{6}Z)/i.exec(url);
  const expires = /[?&]X-Amz-Expires=(\d+)/i.exec(url);
  if (!date || !expires) return null;

  const signed = Date.parse(
    `${date[1].slice(0, 4)}-${date[1].slice(4, 6)}-${date[1].slice(6, 8)}T` +
    `${date[1].slice(9, 11)}:${date[1].slice(11, 13)}:${date[1].slice(13, 15)}Z`);
  return Number.isNaN(signed) ? null : signed + Number(expires[1]) * 1000;
}

/** B5: URL ký đã hết hạn (hoặc sắp hết) thì thẻ <img> sẽ 403 → cần báo người dùng tải lại. */
export function isPresignedStale(url?: string | null, now: number = Date.now()): boolean {
  const at = presignedExpiresAt(url);
  return at !== null && at - now <= PRESIGNED_SAFETY_MS;
}

export async function uploadImage(id: string, file: File, altText: string | null): Promise<RecipeImage> {
  const token = typeof window !== "undefined" ? localStorage.getItem("accessToken") : null;
  const fd = new FormData();
  fd.append("file", file);
  if (altText) fd.append("altText", altText);
  // KHÔNG đặt Content-Type: trình duyệt tự thêm boundary multipart
  const res = await fetch(`${API}/recipes/${id}/images`, {
    method: "POST", headers: token ? { Authorization: `Bearer ${token}` } : {}, body: fd,
  });
  if (res.status === 401) throw new UnauthorizedError(401, "UNAUTHORIZED", "Phiên đăng nhập hết hạn");
  const body = await res.json().catch(() => null);
  if (!res.ok) throw new ApiError(res.status, body?.code ?? body?.error?.code, errorMessage(body, res.status));
  return (body?.data ?? body) as RecipeImage;
}

// PATCH gửi đủ 3 field (JSON strict) và giữ nguyên giá trị hiện tại để không vô tình xoá altText/orderIndex.
// B5: response là RecipeImageDto nên mang cả presignedUrl MỚI — cần để gia hạn URL đã hết hạn.
export const updateImage = (id: string, img: RecipeImage, patch: Partial<Pick<RecipeImage, "isPrimary" | "altText" | "orderIndex">>) =>
  request<RecipeImage>(`/recipes/${id}/images/${img.id}`, json("PATCH", {
    isPrimary: patch.isPrimary ?? img.isPrimary,
    altText: patch.altText ?? img.altText ?? null,
    orderIndex: patch.orderIndex ?? img.orderIndex,
  }));
export const deleteImage = (id: string, imageId: string) =>
  request<unknown>(`/recipes/${id}/images/${imageId}`, json("DELETE"));

/** RowVersion lệch / ghi đồng thời: backend trả 409 hoặc 422 kèm mã concurrency. */
export function isConflict(e: unknown): boolean {
  if (!(e instanceof ApiError)) return false;
  return e.status === 409
    || (e.status === 422 && /concurren|conflict|version/i.test(e.code ?? ""))
    || /thay đổi ở nơi khác/i.test(e.message);
}