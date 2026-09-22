import { PaginationMeta } from './category';

export interface RecipeSummary {
  id: string;
  title: string;
  slug: string;
  description: string;
  categoryId: string;
  categoryName: string;
  authorId: string;
  authorDisplayName: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: 'Easy' | 'Medium' | 'Hard' | 'Expert' | string;
  status: 'Draft' | 'Published' | 'Archived' | string;
  primaryImageUrl?: string | null;
  publishedAt?: string | null;
  createdAt: string;
}

export interface RecipeFilters {
  page?: number;
  pageSize?: number;
  sortBy?: 'createdAt' | 'title' | 'cookTimeMinutes' | 'prepTimeMinutes' | string;
  sortOrder?: 'asc' | 'desc' | string;
  categoryId?: string;
  difficulty?: string;
  maxCookTime?: number;
  minServings?: number;
}

export interface PagedRecipesResult {
  data: RecipeSummary[];
  meta: PaginationMeta;
}
