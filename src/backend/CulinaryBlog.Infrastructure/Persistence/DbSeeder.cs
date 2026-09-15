using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>
/// Seed tối thiểu cho C1 (chứng minh schema chạy + có dữ liệu cho C2). KHÔNG đặt password (login là việc của TV1).
/// TV1/TV2 thay bằng seed Bogus đầy đủ (≥50 recipe, 5 author) khi tích hợp.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (await db.Recipes.IgnoreQueryFilters().AnyAsync(ct)) return; // đã seed

        // Roles
        foreach (var role in new[] { "Guest", "Author", "Admin" })
            if (!await db.Roles.AnyAsync(r => r.Name == role, ct))
                db.Roles.Add(new IdentityRole(role) { NormalizedName = role.ToUpperInvariant() });

        // Users (chỉ đủ cho FK — không có password)
        var author = NewUser("author@demo.local", "Tác giả Demo");
        var admin = NewUser("admin@demo.local", "Quản trị Demo");
        db.Users.AddRange(author, admin);

        // Categories
        var mainDish = Category.Create("Món chính", "mon-chinh", "Các món ăn chính", orderIndex: 1);
        var soup = Category.Create("Món canh", "mon-canh", "Các món canh, súp", orderIndex: 2);
        db.Categories.AddRange(mainDish, soup);

        // Recipe published (đủ nguyên liệu + bước để demo publish - D07)
        var pho = Recipe.CreateDraft(
            "Phở bò truyền thống", "pho-bo-truyen-thong",
            "Món phở bò chuẩn vị Hà Nội.", null,
            prepTimeMinutes: 30, cookTimeMinutes: 180, servings: 4,
            RecipeDifficulty.Medium, soup.Id, author.Id);
        pho.SetNutrition(RecipeNutrition.Create(450, 25, 50, 12, 3, 800));
        pho.AddIngredient("Xương bò", 1000, "g", "Ninh lấy nước dùng");
        pho.AddIngredient("Bánh phở", 500, "g", null);
        pho.AddStep("Ninh xương", "Ninh xương bò 3 tiếng lấy nước dùng trong.", 180, null);
        pho.AddStep("Trần bánh", "Trần bánh phở qua nước sôi, xếp ra tô.", 2, null);
        pho.Publish();

        // Recipe draft
        var trung = Recipe.CreateDraft(
            "Trứng chiên hành", "trung-chien-hanh",
            "Món đơn giản cho bữa sáng.", null,
            prepTimeMinutes: 5, cookTimeMinutes: 5, servings: 2,
            RecipeDifficulty.Easy, mainDish.Id, author.Id);
        trung.AddIngredient("Trứng gà", 3, "quả", null);
        trung.AddStep("Đánh trứng", "Đánh tan trứng với hành lá.", null, null);

        db.Recipes.AddRange(pho, trung);
        await db.SaveChangesAsync(ct);
    }

    private static ApplicationUser NewUser(string email, string displayName)
    {
        var id = Guid.NewGuid().ToString();
        return new ApplicationUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            DisplayName = displayName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }
}
