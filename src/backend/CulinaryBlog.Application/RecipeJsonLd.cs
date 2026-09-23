using System.Text.Json;
using System.Text.Json.Serialization;

namespace CulinaryBlog.Application;

public sealed record RecipeJsonLdModel(
    [property: JsonPropertyName("@context")] string Context,
    [property: JsonPropertyName("@type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("image")] string[] Image,
    [property: JsonPropertyName("author")] RecipeAuthorJsonLd Author,
    [property: JsonPropertyName("datePublished")] string? DatePublished,
    [property: JsonPropertyName("prepTime")] string? PrepTime,
    [property: JsonPropertyName("cookTime")] string? CookTime,
    [property: JsonPropertyName("totalTime")] string? TotalTime,
    [property: JsonPropertyName("recipeYield")] string RecipeYield,
    [property: JsonPropertyName("recipeCategory")] string? RecipeCategory,
    [property: JsonPropertyName("recipeCuisine")] string RecipeCuisine,
    [property: JsonPropertyName("recipeIngredient")] string[] RecipeIngredient,
    [property: JsonPropertyName("recipeInstructions")] RecipeStepJsonLd[] RecipeInstructions,
    [property: JsonPropertyName("nutrition")] RecipeNutritionJsonLd? Nutrition
);

public sealed record RecipeAuthorJsonLd(
    [property: JsonPropertyName("@type")] string Type,
    [property: JsonPropertyName("name")] string Name
);

public sealed record RecipeStepJsonLd(
    [property: JsonPropertyName("@type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("text")] string Text
);

public sealed record RecipeNutritionJsonLd(
    [property: JsonPropertyName("@type")] string Type,
    [property: JsonPropertyName("calories")] string? Calories,
    [property: JsonPropertyName("proteinContent")] string? ProteinContent,
    [property: JsonPropertyName("carbohydrateContent")] string? CarbohydrateContent,
    [property: JsonPropertyName("fatContent")] string? FatContent,
    [property: JsonPropertyName("fiberContent")] string? FiberContent,
    [property: JsonPropertyName("sodiumContent")] string? SodiumContent
);

public static class RecipeJsonLdBuilder
{
    private static string ToIsoDuration(int minutes) => $"PT{minutes}M";

    public static RecipeJsonLdModel Build(
        string title,
        string description,
        string? primaryImageUrl,
        string authorName,
        DateTimeOffset? publishedAt,
        int prepTimeMinutes,
        int cookTimeMinutes,
        int servings,
        string? categoryName,
        IEnumerable<string> ingredients,
        IEnumerable<(string Title, string Description)> steps,
        (int? Calories, int? Protein, int? Carbs, int? Fat, int? Fiber, int? Sodium)? nutrition = null)
    {
        var images = !string.IsNullOrWhiteSpace(primaryImageUrl) ? new[] { primaryImageUrl } : Array.Empty<string>();
        var totalMinutes = prepTimeMinutes + cookTimeMinutes;

        var stepModels = steps.Select((s, idx) => new RecipeStepJsonLd(
            Type: "HowToStep",
            Name: !string.IsNullOrWhiteSpace(s.Title) ? s.Title : $"Bước {idx + 1}",
            Text: s.Description
        )).ToArray();

        RecipeNutritionJsonLd? nutritionModel = null;
        if (nutrition.HasValue && (nutrition.Value.Calories.HasValue || nutrition.Value.Protein.HasValue))
        {
            var n = nutrition.Value;
            nutritionModel = new RecipeNutritionJsonLd(
                Type: "NutritionInformation",
                Calories: n.Calories.HasValue ? $"{n.Calories.Value} calories" : null,
                ProteinContent: n.Protein.HasValue ? $"{n.Protein.Value} g" : null,
                CarbohydrateContent: n.Carbs.HasValue ? $"{n.Carbs.Value} g" : null,
                FatContent: n.Fat.HasValue ? $"{n.Fat.Value} g" : null,
                FiberContent: n.Fiber.HasValue ? $"{n.Fiber.Value} g" : null,
                SodiumContent: n.Sodium.HasValue ? $"{n.Sodium.Value} mg" : null
            );
        }

        return new RecipeJsonLdModel(
            Context: "https://schema.org",
            Type: "Recipe",
            Name: title,
            Description: description,
            Image: images,
            Author: new RecipeAuthorJsonLd("Person", authorName),
            DatePublished: publishedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            PrepTime: prepTimeMinutes > 0 ? ToIsoDuration(prepTimeMinutes) : null,
            CookTime: cookTimeMinutes > 0 ? ToIsoDuration(cookTimeMinutes) : null,
            TotalTime: totalMinutes > 0 ? ToIsoDuration(totalMinutes) : null,
            RecipeYield: $"{servings} khẩu phần",
            RecipeCategory: categoryName,
            RecipeCuisine: "Món Việt",
            RecipeIngredient: ingredients.ToArray(),
            RecipeInstructions: stepModels,
            Nutrition: nutritionModel
        );
    }

    public static string ToJsonString(RecipeJsonLdModel model)
    {
        return JsonSerializer.Serialize(model, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}
