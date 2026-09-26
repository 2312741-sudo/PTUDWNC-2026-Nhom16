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

export interface BasicInfo {
  title: string;
  description: string;
  instructions: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: Difficulty;
  categoryId: string;
  nutrition: unknown | null; // TODO: khớp shape Nutrition của backend
}

export interface SavedRecipe { id: string; slug: string; rowVersion: string; status?: string }

export class ApiError extends Error {
  constructor(public status: number, public code: string | undefined, message: string) {
    super(message);
  }
}

export class UnauthorizedError extends ApiError {}

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
  if (!res.ok) {
    const code = body?.code ?? body?.error?.code;
    const msg = body?.message ?? body?.error?.message ?? body?.title ?? `Lỗi ${res.status}`;
    throw new ApiError(res.status, code, msg);
  }
  return (body?.data ?? body) as T;
}

export const getCategories = () => request<Category[]>("/categories");

export const createRecipe = (info: BasicInfo) =>
  request<SavedRecipe>("/recipes", { method: "POST", body: JSON.stringify(info) });

export const updateRecipe = (id: string, info: BasicInfo, rowVersion: string) =>
  request<SavedRecipe>(`/recipes/${id}`, {
    method: "PUT",
    body: JSON.stringify({ ...info, rowVersion }),
  });