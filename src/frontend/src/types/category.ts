export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  imageUrl?: string | null;
  orderIndex: number;
  recipesCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCategoryRequest {
  name: string;
  description?: string;
  imageUrl?: string;
  orderIndex?: number;
}

export interface UpdateCategoryRequest {
  id: string;
  name: string;
  description?: string;
  imageUrl?: string;
  orderIndex?: number;
}

export interface PaginationMeta {
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface PagedResult<T> {
  data: T[];
  meta: PaginationMeta;
}
