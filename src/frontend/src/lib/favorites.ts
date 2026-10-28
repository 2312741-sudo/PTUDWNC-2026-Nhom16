'use client';

import { useState, useEffect, useCallback } from 'react';
import { RecipeSummary } from '@/types/recipe';

export interface SavedRecipeItem {
  id?: string;
  slug: string;
  title: string;
  description?: string;
  categoryName?: string;
  cookTimeMinutes?: number;
  servings?: number;
  difficulty?: string;
  imageUrl?: string;
  savedAt: string;
}

const STORAGE_KEY = 'culinary_saved_recipes';

export function getSavedRecipes(): SavedRecipeItem[] {
  if (typeof window === 'undefined') return [];
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed : [];
  } catch (e) {
    console.error('Failed to read saved recipes from localStorage:', e);
    return [];
  }
}

export function saveRecipeToStorage(item: SavedRecipeItem): SavedRecipeItem[] {
  if (typeof window === 'undefined') return [];
  const current = getSavedRecipes();
  const exists = current.some((r) => r.slug === item.slug);
  let updated: SavedRecipeItem[];
  if (exists) {
    updated = current.filter((r) => r.slug !== item.slug);
  } else {
    updated = [{ ...item, savedAt: new Date().toISOString() }, ...current];
  }
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(updated));
    window.dispatchEvent(new Event('favorites-updated'));
  } catch (e) {
    console.error('Failed to save recipes to localStorage:', e);
  }
  return updated;
}

export function removeSavedRecipe(slug: string): SavedRecipeItem[] {
  if (typeof window === 'undefined') return [];
  const current = getSavedRecipes();
  const updated = current.filter((r) => r.slug !== slug);
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(updated));
    window.dispatchEvent(new Event('favorites-updated'));
  } catch (e) {
    console.error('Failed to remove recipe from localStorage:', e);
  }
  return updated;
}

export function isRecipeSaved(slug: string): boolean {
  if (typeof window === 'undefined') return false;
  const current = getSavedRecipes();
  return current.some((r) => r.slug === slug);
}

export function useFavorites() {
  const [favorites, setFavorites] = useState<SavedRecipeItem[]>([]);
  const [isLoaded, setIsLoaded] = useState(false);

  const reload = useCallback(() => {
    setFavorites(getSavedRecipes());
  }, []);

  useEffect(() => {
    reload();
    setIsLoaded(true);

    const handleUpdate = () => reload();
    window.addEventListener('favorites-updated', handleUpdate);
    window.addEventListener('storage', handleUpdate);

    return () => {
      window.removeEventListener('favorites-updated', handleUpdate);
      window.removeEventListener('storage', handleUpdate);
    };
  }, [reload]);

  const toggle = useCallback(
    (recipe: {
      id?: string;
      slug: string;
      title: string;
      description?: string;
      categoryName?: string;
      cookTimeMinutes?: number;
      servings?: number;
      difficulty?: string;
      imageUrl?: string;
    }) => {
      const updated = saveRecipeToStorage({
        id: recipe.id,
        slug: recipe.slug,
        title: recipe.title,
        description: recipe.description,
        categoryName: recipe.categoryName,
        cookTimeMinutes: recipe.cookTimeMinutes,
        servings: recipe.servings,
        difficulty: recipe.difficulty,
        imageUrl: recipe.imageUrl,
        savedAt: new Date().toISOString(),
      });
      setFavorites(updated);
      return updated.some((r) => r.slug === recipe.slug);
    },
    []
  );

  const remove = useCallback((slug: string) => {
    const updated = removeSavedRecipe(slug);
    setFavorites(updated);
  }, []);

  const has = useCallback(
    (slug: string) => {
      return favorites.some((r) => r.slug === slug);
    },
    [favorites]
  );

  return {
    favorites,
    count: favorites.length,
    isLoaded,
    toggle,
    remove,
    has,
    reload,
  };
}
