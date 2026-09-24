import { MetadataRoute } from 'next';
import { getSitemapRecipes } from '@/lib/api';

const BASE_URL = process.env.NEXT_PUBLIC_API_URL
  ? process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000'
  : 'http://localhost:3000';

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const staticRoutes: MetadataRoute.Sitemap = [
    {
      url: `${BASE_URL}/`,
      lastModified: new Date(),
      changeFrequency: 'daily',
      priority: 1,
    },
    {
      url: `${BASE_URL}/recipes`,
      lastModified: new Date(),
      changeFrequency: 'daily',
      priority: 0.9,
    },
    {
      url: `${BASE_URL}/search`,
      lastModified: new Date(),
      changeFrequency: 'weekly',
      priority: 0.7,
    },
    {
      url: `${BASE_URL}/categories`,
      lastModified: new Date(),
      changeFrequency: 'weekly',
      priority: 0.8,
    },
  ];

  // Chỉ Published (backend /api/v1/recipes/sitemap đã lọc Published && !IsDeleted).
  const recipes = await getSitemapRecipes();

  const recipeRoutes: MetadataRoute.Sitemap = recipes.map((r) => ({
    url: `${BASE_URL}/recipes/${encodeURIComponent(r.slug)}`,
    lastModified: r.publishedAt ? new Date(r.publishedAt) : new Date(),
    changeFrequency: 'weekly',
    priority: 0.8,
  }));

  return [...staticRoutes, ...recipeRoutes];
}