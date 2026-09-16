import { Category, CreateCategoryRequest, UpdateCategoryRequest } from '@/types/category';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api/v1';

export async function getCategories(): Promise<Category[]> {
  try {
    const res = await fetch(`${API_BASE_URL}/categories`, {
      next: { revalidate: 3600 },
    });
    if (!res.ok) throw new Error('Không thể tải danh sách danh mục.');
    return await res.json();
  } catch (error) {
    console.error('Error in getCategories:', error);
    return [];
  }
}

export async function getCategoryBySlug(slug: string): Promise<Category | null> {
  try {
    const res = await fetch(`${API_BASE_URL}/categories/${encodeURIComponent(slug)}`, {
      next: { revalidate: 600 },
    });
    if (res.status === 404) return null;
    if (!res.ok) throw new Error('Không thể tải chi tiết danh mục.');
    return await res.json();
  } catch (error) {
    console.error('Error in getCategoryBySlug:', error);
    return null;
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

    return { success: true, data: result };
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

    return { success: true, data: result };
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

    if (res.status === 204) return { success: true };

    const result = await res.json();
    return { success: false, error: result.title || result.detail || 'Không thể xóa danh mục.' };
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
    return { success: true, data };
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

    return { success: true, data: result };
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

