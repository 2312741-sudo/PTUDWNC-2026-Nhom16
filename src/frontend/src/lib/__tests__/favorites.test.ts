import {
  getSavedRecipes,
  saveRecipeToStorage,
  removeSavedRecipe,
  isRecipeSaved,
  SavedRecipeItem,
} from '../favorites';

describe('Recipe Favorites / Bookmark Storage Utility', () => {
  beforeEach(() => {
    localStorage.clear();
    jest.clearAllMocks();
  });

  const sampleRecipe: SavedRecipeItem = {
    id: 'test-1',
    slug: 'pho-bo-ha-noi',
    title: 'Phở bò Hà Nội',
    categoryName: 'Món nước',
    cookTimeMinutes: 45,
    servings: 4,
    difficulty: 'Medium',
    savedAt: new Date().toISOString(),
  };

  it('returns empty array when no favorites are saved', () => {
    expect(getSavedRecipes()).toEqual([]);
  });

  it('saves a recipe and marks it as saved', () => {
    const list = saveRecipeToStorage(sampleRecipe);
    expect(list.length).toBe(1);
    expect(list[0].slug).toBe('pho-bo-ha-noi');
    expect(isRecipeSaved('pho-bo-ha-noi')).toBe(true);
    expect(isRecipeSaved('bun-cha')).toBe(false);
  });

  it('toggles (removes) a recipe if it is already in favorites', () => {
    saveRecipeToStorage(sampleRecipe);
    expect(isRecipeSaved('pho-bo-ha-noi')).toBe(true);

    const afterToggle = saveRecipeToStorage(sampleRecipe);
    expect(afterToggle.length).toBe(0);
    expect(isRecipeSaved('pho-bo-ha-noi')).toBe(false);
  });

  it('removes a specific recipe using removeSavedRecipe', () => {
    saveRecipeToStorage(sampleRecipe);
    saveRecipeToStorage({
      id: 'test-2',
      slug: 'bun-bo-hue',
      title: 'Bún bò Huế',
      savedAt: new Date().toISOString(),
    });

    expect(getSavedRecipes().length).toBe(2);

    const remaining = removeSavedRecipe('pho-bo-ha-noi');
    expect(remaining.length).toBe(1);
    expect(remaining[0].slug).toBe('bun-bo-hue');
    expect(isRecipeSaved('pho-bo-ha-noi')).toBe(false);
    expect(isRecipeSaved('bun-bo-hue')).toBe(true);
  });

  it('handles invalid or corrupted JSON in localStorage safely', () => {
    localStorage.setItem('culinary_saved_recipes', 'not-a-json-string{');
    expect(getSavedRecipes()).toEqual([]);
  });
});
