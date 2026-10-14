import { Category, CreateCategoryRequest, UpdateCategoryRequest } from '@/types/category';
import { MOCK_CATEGORIES, MOCK_RECIPES, MOCK_RECIPE_DETAIL, getMockPagedRecipes } from './mockData';
export * from './errors';
export { httpClient, HttpClient } from './httpClient';

// `NEXT_PUBLIC_API_URL` phục vụ TRÌNH DUYỆT (qua reverse proxy). Trên server (SSR/ISR) thì
// `localhost` là chính container frontend, không phải API -> fetch thất bại (ECONNREFUSED).
// `API_INTERNAL_URL` là biến chỉ dùng phía server (không có tiền tố NEXT_PUBLIC nên không bị
// inline vào bundle client): staging đặt về địa chỉ nội bộ của API trong mạng compose.
export const API_BASE_URL =
  (typeof window === 'undefined' ? process.env.API_INTERNAL_URL : undefined) ||
  process.env.NEXT_PUBLIC_API_URL ||
  'http://localhost:5080/api/v1';

export async function getCategories(): Promise<Category[]> {
  try {
    const res = await fetch(`${API_BASE_URL}/categories`, {
      next: { revalidate: 3600 },
    });
    if (!res.ok) throw new Error('Không thể tải danh sách danh mục.');
    const json = await res.json();
    return json.data ?? json;
  } catch (error) {
    console.error('Error in getCategories:', error);
    return MOCK_CATEGORIES;
  }
}

export async function getCategoryBySlug(slug: string): Promise<Category | null> {
  try {
    const res = await fetch(`${API_BASE_URL}/categories/${encodeURIComponent(slug)}`, {
      next: { revalidate: 600 },
    });
    if (res.status === 404) return null;
    if (!res.ok) throw new Error('Không thể tải chi tiết danh mục.');
    const json = await res.json();
    return json.data ?? json;
  } catch (error) {
    console.error('Error in getCategoryBySlug:', error);
    return MOCK_CATEGORIES.find((c) => c.slug === slug) ?? null;
  }
}

export async function createCategory(
  data: CreateCategoryRequest,
  token?: string
): Promise<{ success: boolean; data?: Category; error?: string }> {
  try {
    const res = await fetch(`${API_BASE_URL}/categories`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(data),
    });

    const result = await res.json();
    if (!res.ok) {
      return { success: false, error: result.title || result.detail || 'Lỗi khi tạo danh mục.' };
    }

    return { success: true, data: result.data ?? result };
  } catch (err: any) {
    return { success: false, error: err.message || 'Lỗi kết nối máy chủ.' };
  }
}

export async function updateCategory(
  id: string,
  data: UpdateCategoryRequest,
  token?: string
): Promise<{ success: boolean; data?: Category; error?: string }> {
  try {
    const res = await fetch(`${API_BASE_URL}/categories/${encodeURIComponent(id)}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(data),
    });

    const result = await res.json();
    if (!res.ok) {
      return { success: false, error: result.title || result.detail || 'Lỗi khi cập nhật danh mục.' };
    }

    return { success: true, data: result.data ?? result };
  } catch (err: any) {
    return { success: false, error: err.message || 'Lỗi kết nối máy chủ.' };
  }
}

export async function deleteCategory(
  id: string,
  token?: string
): Promise<{ success: boolean; error?: string }> {
  try {
    const res = await fetch(`${API_BASE_URL}/categories/${encodeURIComponent(id)}`, {
      method: 'DELETE',
      headers: {
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    });

    if (res.status === 204 || res.ok) {
      return { success: true };
    }

    const result = await res.json().catch(() => ({}));
    return {
      success: false,
      error: result.title || result.detail || 'Lỗi khi xoá danh mục.',
    };
  } catch (err: any) {
    return { success: false, error: err.message || 'Lỗi kết nối máy chủ.' };
  }
}

// ----------------------------------------------------------------------
// Auth & Profile Endpoints (TV1 - Tuần 2)
// ----------------------------------------------------------------------

export async function getMe(token: string): Promise<{ success: boolean; data?: import('@/types/auth').User; error?: string }> {
  try {
    const res = await fetch(`${API_BASE_URL}/auth/me`, {
      method: 'GET',
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });

    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      return { success: false, error: err.title || err.detail || 'Không thể tải thông tin tài khoản.' };
    }

    const data = await res.json();
    return { success: true, data: data.data ?? data };
  } catch (err: any) {
    return { success: false, error: err.message || 'Lỗi kết nối máy chủ.' };
  }
}

export async function updateProfile(
  data: import('@/types/auth').UpdateProfileRequest,
  token: string
): Promise<{ success: boolean; data?: import('@/types/auth').User; error?: string; validationErrors?: Record<string, string[]> }> {
  try {
    const res = await fetch(`${API_BASE_URL}/auth/me`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });

    const result = await res.json().catch(() => ({}));
    if (!res.ok) {
      return {
        success: false,
        error: result.title || result.detail || 'Cập nhật hồ sơ thất bại.',
        validationErrors: result.errors,
      };
    }

    return { success: true, data: result.data ?? result };
  } catch (err: any) {
    return { success: false, error: err.message || 'Lỗi kết nối máy chủ.' };
  }
}

export async function logout(token: string): Promise<{ success: boolean; error?: string }> {
  try {
    const res = await fetch(`${API_BASE_URL}/auth/logout`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({}),
    });

    if (res.status === 204 || res.ok) return { success: true };
    const err = await res.json().catch(() => ({}));
    return { success: false, error: err.title || err.detail || 'Đăng xuất thất bại.' };
  } catch (err: any) {
    return { success: false, error: err.message || 'Lỗi kết nối máy chủ.' };
  }
}

export async function requestChangePasswordCode(
  token: string
): Promise<{ success: boolean; message?: string; error?: string }> {
  try {
    const res = await fetch(`${API_BASE_URL}/auth/change-password/request-code`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
    });

    const result = await res.json().catch(() => ({}));
    if (!res.ok) {
      return {
        success: false,
        error: result.title || result.detail || 'Không thể gửi mã xác thực.',
      };
    }

    return { success: true, message: result.message || 'Mã xác thực đã được gửi về email của bạn.' };
  } catch (err: any) {
    return { success: false, error: err.message || 'Lỗi kết nối máy chủ.' };
  }
}

export async function changePassword(
  data: { currentPassword: string; newPassword: string; code?: string },
  token: string
): Promise<{ success: boolean; error?: string; validationErrors?: Record<string, string[]> }> {
  try {
    const res = await fetch(`${API_BASE_URL}/auth/change-password`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });

    if (res.status === 204 || res.ok) return { success: true };
    const err = await res.json().catch(() => ({}));
    return {
      success: false,
      error: err.title || err.detail || 'Đổi mật khẩu thất bại.',
      validationErrors: err.errors,
    };
  } catch (err: any) {
    return { success: false, error: err.message || 'Lỗi kết nối máy chủ.' };
  }
}

export async function forgotPassword(
  email: string
): Promise<{ success: boolean; message?: string; error?: string }> {
  try {
    const res = await fetch(`${API_BASE_URL}/auth/forgot-password`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email }),
    });

    const result = await res.json().catch(() => ({}));
    if (!res.ok) {
      return {
        success: false,
        error: result.title || result.detail || 'Không thể khôi phục mật khẩu.',
      };
    }

    return {
      success: true,
      message: result.message || 'Mật khẩu mới đã được gửi về email của bạn. Vui lòng kiểm tra hộp thư.',
    };
  } catch (err: any) {
    return { success: false, error: err.message || 'Lỗi kết nối máy chủ.' };
  }
}

// ----------------------------------------------------------------------
// Recipe Discovery & Search Endpoints (TV2 - Tuần 2)
// ----------------------------------------------------------------------

export async function getRecipes(
  filters: import('@/types/recipe').RecipeFilters = {}
): Promise<import('@/types/recipe').PagedRecipesResult> {
  const params = new URLSearchParams();
  if (filters.page) params.set('page', filters.page.toString());
  if (filters.pageSize) params.set('pageSize', filters.pageSize.toString());
  if (filters.sortBy) params.set('sortBy', filters.sortBy);
  if (filters.sortOrder) params.set('sortOrder', filters.sortOrder);
  if (filters.categoryId) params.set('categoryId', filters.categoryId);
  if (filters.difficulty) params.set('difficulty', filters.difficulty);
  if (filters.maxCookTime !== undefined) params.set('maxCookTime', filters.maxCookTime.toString());
  if (filters.minServings !== undefined) params.set('minServings', filters.minServings.toString());

  try {
    const res = await fetch(`${API_BASE_URL}/recipes?${params.toString()}`, {
      cache: 'no-store',
    });
    if (!res.ok) throw new Error('Không thể tải danh sách công thức.');
    const json = await res.json();
    return json.data && json.meta ? json : (json.data ?? json);
  } catch (error) {
    console.error('Error in getRecipes:', error);
    return getMockPagedRecipes(filters.page || 1, filters.pageSize || 12);
  }
}

export async function searchRecipes(
  q: string,
  filters: import('@/types/recipe').RecipeFilters = {}
): Promise<import('@/types/recipe').PagedRecipesResult> {
  const params = new URLSearchParams();
  params.set('q', q);
  if (filters.page) params.set('page', filters.page.toString());
  if (filters.pageSize) params.set('pageSize', filters.pageSize.toString());
  if (filters.sortBy) params.set('sortBy', filters.sortBy);
  if (filters.sortOrder) params.set('sortOrder', filters.sortOrder);
  if (filters.categoryId) params.set('categoryId', filters.categoryId);
  if (filters.difficulty) params.set('difficulty', filters.difficulty);
  if (filters.maxCookTime !== undefined) params.set('maxCookTime', filters.maxCookTime.toString());
  if (filters.minServings !== undefined) params.set('minServings', filters.minServings.toString());

  try {
    const res = await fetch(`${API_BASE_URL}/recipes/search?${params.toString()}`, {
      cache: 'no-store',
    });
    if (!res.ok) throw new Error('Không thể tìm kiếm công thức.');
    const json = await res.json();
    return json.data && json.meta ? json : (json.data ?? json);
  } catch (error) {
    console.error('Error in searchRecipes:', error);
    const filtered = MOCK_RECIPES.filter(
      (r) =>
        r.title.toLowerCase().includes(q.toLowerCase()) ||
        r.description.toLowerCase().includes(q.toLowerCase())
    );
    return {
      data: filtered,
      meta: {
        page: 1,
        pageSize: 12,
        total: filtered.length,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      },
    };
  }
}

export async function loginWithGoogle(
  idToken: string
): Promise<{ success: boolean; data?: any; error?: string }> {
  try {
    const res = await fetch(`${API_BASE_URL}/auth/google`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ idToken }),
    });

    const result = await res.json();
    if (!res.ok) {
      return { success: false, error: result.title || result.detail || 'Đăng nhập Google thất bại.' };
    }

    return { success: true, data: result.data ?? result };
  } catch (err: any) {
    return { success: false, error: err.message || 'Lỗi kết nối máy chủ.' };
  }
}

export async function login(
  data: import('@/types/auth').LoginRequest
): Promise<{ success: boolean; data?: import('@/types/auth').AuthResponse; error?: string }> {
  try {
    const res = await fetch(`${API_BASE_URL}/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    });

    const result = await res.json().catch(() => ({}));
    if (!res.ok) {
      return {
        success: false,
        error: result.detail || result.title || 'Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin.',
      };
    }

    return { success: true, data: result.data ?? result };
  } catch (err: any) {
    return { success: false, error: err.message || 'Lỗi kết nối máy chủ.' };
  }
}

export async function register(
  data: import('@/types/auth').RegisterRequest
): Promise<{ success: boolean; data?: import('@/types/auth').AuthResponse; error?: string; validationErrors?: Record<string, string[]> }> {
  try {
    const res = await fetch(`${API_BASE_URL}/auth/register`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    });

    const result = await res.json().catch(() => ({}));
    if (!res.ok) {
      return {
        success: false,
        error: result.detail || result.title || 'Đăng ký tài khoản thất bại.',
        validationErrors: result.errors,
      };
    }

    return { success: true, data: result.data ?? result };
  } catch (err: any) {
    return { success: false, error: err.message || 'Lỗi kết nối máy chủ.' };
  }
}

// ----------------------------------------------------------------------
// Sitemap (SEO, D26/TV4): chỉ trả công thức Published (backend đã lọc)
// ----------------------------------------------------------------------

export interface SitemapRecipe {
  id: string;
  slug: string;
  publishedAt?: string | null;
}

export async function getSitemapRecipes(): Promise<SitemapRecipe[]> {
  try {
    const res = await fetch(`${API_BASE_URL}/recipes/sitemap`, {
      cache: 'no-store',
    });
    if (!res.ok) throw new Error('Không thể tải sitemap.');
    const json = await res.json();
    const data = json.data ?? json;
    return Array.isArray(data) ? data : [];
  } catch (error) {
    console.error('Error in getSitemapRecipes:', error);
    return [];
  }
}

// ----------------------------------------------------------------------
// Recipe Detail (TV3 - Tuần 3)
// ----------------------------------------------------------------------

export async function getRecipeBySlug(
  slug: string,
  token?: string
): Promise<import('@/types/recipe').RecipeDetail | null> {
  try {
    const res = await fetch(`${API_BASE_URL}/recipes/${encodeURIComponent(slug)}`, {
      ...(token
        ? { cache: 'no-store' as const, headers: { Authorization: `Bearer ${token}` } }
        : { next: { revalidate: 300 } }),
    });
    if (res.status === 404) return null;
    if (!res.ok) throw new Error('Không thể tải chi tiết công thức.');
    const json = await res.json();
    return json.data ?? json;
  } catch (error) {
    console.error('Error in getRecipeBySlug:', error);
    if (slug === 'pho-bo-gia-truyen-ha-noi' || MOCK_RECIPE_DETAIL.slug === slug) {
      return MOCK_RECIPE_DETAIL;
    }
    const found = MOCK_RECIPES.find((r) => r.slug === slug);
    if (found) {
      return {
        ...MOCK_RECIPE_DETAIL,
        id: found.id,
        title: found.title,
        slug: found.slug,
        description: found.description,
        categoryId: found.categoryId,
        authorId: found.authorId,
        prepTimeMinutes: found.prepTimeMinutes,
        cookTimeMinutes: found.cookTimeMinutes,
        totalTimeMinutes: found.prepTimeMinutes + found.cookTimeMinutes,
        servings: found.servings,
        difficulty: found.difficulty,
        status: found.status,
        publishedAt: found.publishedAt,
        createdAt: found.createdAt,
        images: [{ id: 'mock-img', originalUrl: found.primaryImageUrl || '', isPrimary: true, orderIndex: 1 }],
      };
    }
    return null;
  }
}
