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

export interface RecipeIngredient {
  id: string;
  recipeId: string;
  name: string;
  quantity?: number | null;
  unit?: string | null;
  notes?: string | null;
  orderIndex: number;
}

export interface RecipeStep {
  id: string;
  recipeId: string;
  stepNumber: number;
  title: string;
  description: string;
  timerMinutes?: number | null;
  imageUrl?: string | null;
}

export interface RecipeNutrition {
  calories?: number | null;
  protein?: number | null;
  carbohydrates?: number | null;
  fat?: number | null;
  fiber?: number | null;
  sodium?: number | null;
}

export interface RecipeImageSummary {
  id: string;
  originalUrl: string;
  mediumUrl?: string | null;
  thumbnailUrl?: string | null;
  altText?: string | null;
  isPrimary: boolean;
  orderIndex: number;
}

/** Chi tiết công thức — GET /api/v1/recipes/{slug} (TV3, FR-RCP-002) */
export interface RecipeDetail {
  id: string;
  title: string;
  slug: string;
  description: string;
  instructions: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  totalTimeMinutes: number;
  difficulty: 'Easy' | 'Medium' | 'Hard' | 'Expert' | string;
  status: 'Draft' | 'Published' | 'Archived' | string;
  publishedAt?: string | null;
  categoryId: string;
  authorId: string;
  nutrition?: RecipeNutrition | null;
  ingredients: RecipeIngredient[];
  steps: RecipeStep[];
  images: RecipeImageSummary[];
  rowVersion: string;
  createdAt: string;
  updatedAt?: string | null;
}