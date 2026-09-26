// Kiểu khớp với MyRecipeSummaryDto / MyRecipeCountsDto bên backend.
// difficulty/status là chuỗi (backend đã ToString()).

export type RecipeStatus = string; // lấy danh sách thực tế từ /counts, không hardcode
export type Difficulty = "Easy" | "Medium" | "Hard" | "Expert";

export interface MyRecipeSummary {
  id: string;
  title: string;
  slug: string;
  categoryId: string;
  categoryName: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: Difficulty;
  status: RecipeStatus;
  primaryImageUrl: string | null;
  ingredientCount: number;
  stepCount: number;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  rowVersion: string;
}

// ĐỐI CHIẾU: tên field theo PagedResult<T> thực tế (items/totalCount/...).
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface MyRecipeCounts {
  all: number;
  byStatus: Record<string, number>;
}

export interface MyRecipesParams {
  page: number;
  pageSize: number;
  sortBy: "updatedAt" | "createdAt" | "title" | "publishedAt";
  sortOrder: "asc" | "desc";
  status?: string;
  q?: string;
}

export class ApiError extends Error {
  constructor(public status: number, public code: string | undefined, message: string) {
    super(message);
  }
}

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080/api/v1";

// ĐỐI CHIẾU: nếu đã có client dùng chung từ C5 (tự refresh khi 401 rồi retry),
// thay hàm này bằng client đó. Bản dưới chỉ gắn Bearer và unwrap { data }.
async function authFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = typeof window !== "undefined" ? localStorage.getItem("accessToken") : null;
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  });

  if (res.status === 204) return undefined as T;

  const body = await res.json().catch(() => null);
  if (!res.ok) {
    throw new ApiError(res.status, body?.code ?? body?.error?.code, body?.message ?? body?.title ?? res.statusText);
  }
  return body?.data as T; // C08
}

interface PagedResponse<T> {
  data: T[];
  meta: Record<string, number | undefined>;
}

export async function getMyRecipes(p: MyRecipesParams): Promise<PagedResult<MyRecipeSummary>> {
  const qs = new URLSearchParams({
    page: String(p.page),
    pageSize: String(p.pageSize),
    sortBy: p.sortBy,
    sortOrder: p.sortOrder,
  });
  if (p.status) qs.set("status", p.status);
  if (p.q?.trim()) qs.set("q", p.q.trim());
  const raw = await authFetch<PagedResponse<MyRecipeSummary>>(`/me/recipes?${qs}`);
  const m = raw?.meta ?? {};
  return {
    items: raw?.data ?? [],
    page: m.page ?? m.currentPage ?? p.page,
    pageSize: m.pageSize ?? p.pageSize,
    totalCount: m.totalCount ?? m.totalItems ?? m.total ?? 0,
  };
}
export function getMyRecipeCounts() {
  return authFetch<MyRecipeCounts>("/me/recipes/counts");
}

// ĐỐI CHIẾU: cách endpoint DELETE của C2 nhận rowVersion (header If-Match hay query/body).
export function deleteRecipe(id: string, rowVersion: string) {
  return authFetch<void>(`/recipes/${id}`, {
    method: "DELETE",
    headers: { "If-Match": rowVersion },
  });
}