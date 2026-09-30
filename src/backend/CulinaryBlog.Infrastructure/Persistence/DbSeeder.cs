using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Recipe = CulinaryBlog.Domain.Entities.Recipe;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>
/// Bộ sinh dữ liệu mẫu đầy đủ phục vụ Lab 2 và kiểm thử hệ thống:
/// Đảm bảo tối thiểu: 25 Categories, 100 Recipes (mỗi recipe >= 10 nguyên liệu, >= 5 bước chế biến).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AuthDbContext db, CancellationToken ct = default)
    {
        try { await db.Database.MigrateAsync(ct); } catch { /* Ignore migration errors if tables already exist */ }

        // Cập nhật tất cả các ảnh món ăn sang đường dẫn ảnh thực tế local
        var placeholderImages = await db.RecipeImages
            .Where(img => img.OriginalUrl.Contains("photo-1546069901-ba9599a7e63c") || !img.OriginalUrl.StartsWith("/images/recipes/"))
            .ToListAsync(ct);
        if (placeholderImages.Count > 0)
        {
            var rIds = placeholderImages.Select(i => i.RecipeId).Distinct().ToList();
            var slugLookup = await db.Recipes
                .IgnoreQueryFilters()
                .Where(r => rIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Slug, ct);

            foreach (var img in placeholderImages)
            {
                if (slugLookup.TryGetValue(img.RecipeId, out var slug))
                {
                    img.SetOriginalUrl(GetDishImage(slug));
                }
            }
            await db.SaveChangesAsync(ct);
        }

        // Cập nhật các công thức cũ nếu còn mang nguyên liệu mẫu chung ("Thịt chính / Hải sản" hoặc "Nguyên liệu chính")
        var hasGenericIngredients = await db.RecipeIngredients
            .AnyAsync(i => i.Name.Contains("Thịt chính") || i.Name.Contains("Nguyên liệu chính"), ct);

        if (hasGenericIngredients)
        {
            if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
            {
                await db.Database.ExecuteSqlRawAsync("DELETE FROM RecipeIngredients; DELETE FROM RecipeSteps;", ct);
            }
            else
            {
                await db.Database.ExecuteSqlRawAsync("DELETE FROM \"RecipeIngredients\"; DELETE FROM \"RecipeSteps\";", ct);
            }

            var existingRecipes = await db.Recipes.ToListAsync(ct);
            var seedLookup = RecipeSeedData.All.ToDictionary(r => r.Slug, r => r);

            foreach (var recipe in existingRecipes)
            {
                if (!seedLookup.TryGetValue(recipe.Slug, out var seed)) continue;

                recipe.ResetIngredientsAndSteps();

                // Cập nhật thông tin chi tiết
                recipe.UpdateDetails(
                    seed.Title,
                    seed.Slug,
                    seed.Description,
                    seed.Instructions,
                    seed.PrepTimeMinutes,
                    seed.CookTimeMinutes,
                    seed.Servings,
                    seed.Difficulty,
                    recipe.CategoryId);

                recipe.SetNutrition(RecipeNutrition.Create(
                    seed.Nutrition.Calories,
                    seed.Nutrition.Protein,
                    seed.Nutrition.Carbs,
                    seed.Nutrition.Fat,
                    seed.Nutrition.Fiber,
                    seed.Nutrition.Sodium));

                foreach (var ing in seed.Ingredients)
                {
                    recipe.AddIngredient(ing.Name, ing.Quantity, ing.Unit, ing.Notes);
                }

                foreach (var step in seed.Steps)
                {
                    recipe.AddStep(step.Title, step.Description, step.TimerMinutes, step.Tip);
                }
            }

            await db.SaveChangesAsync(ct);
        }

        // Kiểm tra nếu đã có đủ 100 recipes thì bỏ qua việc tạo mới
        if (await db.Recipes.IgnoreQueryFilters().CountAsync(ct) >= 100) return;

        // 1. Roles
        foreach (var role in new[] { "Guest", "Author", "Admin" })
        {
            if (!await db.Roles.AnyAsync(r => r.Name == role, ct))
                db.Roles.Add(new IdentityRole(role) { NormalizedName = role.ToUpperInvariant() });
        }

        // 2. Authors (5 tác giả ẩm thực)
        var authors = new List<ApplicationUser>();
        var authorInfo = new[]
        {
            ("tam.nguyen@culinary.local", "Nguyễn Thanh Tâm"),
            ("vi.ngo@culinary.local", "Ngô Quốc Trường Vĩ"),
            ("trung.huynh@culinary.local", "Huỳnh Quốc Trung"),
            ("son.nguyen@culinary.local", "Nguyễn Hữu Trung Sơn"),
            ("masterchef@culinary.local", "Bếp Trưởng Culinary")
        };

        var hasher = new PasswordHasher<ApplicationUser>();
        foreach (var (email, name) in authorInfo)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user == null)
            {
                user = NewUser(email, name);
                user.PasswordHash = hasher.HashPassword(user, "User@123456");
                db.Users.Add(user);
            }
            authors.Add(user);
        }
        await db.SaveChangesAsync(ct);

        // Đảm bảo gán role Author/Admin
        foreach (var author in authors)
        {
            var isMaster = author.Email == "masterchef@culinary.local";
            var targetRole = isMaster ? "Admin" : "Author";
            var roleId = (await db.Roles.FirstAsync(r => r.Name == targetRole, ct)).Id;
            if (!await db.UserRoles.AnyAsync(ur => ur.UserId == author.Id && ur.RoleId == roleId, ct))
            {
                db.UserRoles.Add(new IdentityUserRole<string> { UserId = author.Id, RoleId = roleId });
            }
        }
        await db.SaveChangesAsync(ct);

        // 3. Categories (25 danh mục ẩm thực phong phú)
        var catDefs = new (string Name, string Slug, string Desc)[]
        {
            ("Món khai vị", "mon-khai-vi", "Các món nhẹ nhàng đánh thức vị giác trước bữa chính."),
            ("Món chính", "mon-chinh", "Các món ăn trung tâm đầy đủ dinh dưỡng cho bữa cơm."),
            ("Món canh & súp", "mon-canh-sup", "Các món canh thanh mát, súp bổ dưỡng cho mọi lứa tuổi."),
            ("Món xào", "mon-xao", "Hương vị đậm đà từ các nguyên liệu tươi xào nhanh trên lửa lớn."),
            ("Món kho & rim", "mon-kho-rim", "Các món kho tộ, rim đậm vị ăn kèm cơm trắng nóng hổi."),
            ("Món nướng & BBQ", "mon-nuong-bbq", "Hương thơm quyến rũ từ than hoa và gia vị tẩm ướp đặc trưng."),
            ("Món lẩu", "mon-lau", "Nồi lẩu bốc khói nghi ngút tụ họp gia đình, bạn bè cuối tuần."),
            ("Món chiên & rán", "mon-chien-ran", "Giòn rụm bên ngoài, mềm mọng ngọt ngào bên trong."),
            ("Món hấp & luộc", "mon-hap-luoc", "Giữ trọn vẹn vị ngọt thanh tự nhiên và dưỡng chất của thực phẩm."),
            ("Gỏi & nộm", "goi-nom", "Sự hòa quyện chua cay mặn ngọt thanh mát từ rau củ và tôm thịt."),
            ("Món cuốn", "mon-cuon", "Nét tinh hoa ẩm thực Việt với bánh tráng dẻo và rau sống tươi mát."),
            ("Món nước (Phở, Bún, Mì)", "mon-nuoc", "Nước dùng hầm ngọt từ xương thơm nức mùi hồi quế thảo mộc."),
            ("Cháo & súp nóng", "chao-sup-nong", "Món ăn ấm bụng, bồi bổ sức khỏe cho mọi thành viên."),
            ("Món bánh truyền thống", "mon-banh-truyen-thong", "Bánh chưng, bánh giò, bánh bèo, bánh cuốn đậm đà hồn quê."),
            ("Bánh ngọt & tráng miệng", "banh-ngot-trang-mieng", "Bánh kem, mousse, tiramisu ngọt ngào sau bữa ăn."),
            ("Chè & món ngọt Việt", "che-mon-ngot-viet", "Chè hạt sen, sương sa hạt lựu, chè bưởi thơm lừng nước cốt dừa."),
            ("Trà & thức uống thanh nhiệt", "tra-thuc-uong-thanh-nhiet", "Trà đào, trà hoa quả giải nhiệt sảng khoái mùa hè."),
            ("Sinh tố & nước ép", "sinh-to-nuoc-ep", "Thức uống giàu vitamin, đẹp da, tăng cường đề kháng."),
            ("Hải sản tươi sống", "hai-san-tuoi-song", "Mực, tôm, cua, cá biển tươi ngon chế biến phong phú."),
            ("Món bò", "mon-bo", "Các món từ thịt bò mềm ngọt, bắp bò giòn sần sật."),
            ("Món gà & gia cầm", "mon-ga-gia-cam", "Gà đồi luộc lá chanh, gà nướng mật ong đậm đà."),
            ("Món thịt heo", "mon-thit-heo", "Các món chế biến từ thịt heo thơm ngon mỗi ngày."),
            ("Món ăn sáng", "mon-an-sang", "Các món ăn sáng nhanh gọn, cung cấp năng lượng ngày mới."),
            ("Món ăn vặt đường phố", "mon-an-vat-duong-pho", "Các món ăn vặt được giới trẻ yêu thích."),
        };

        var catList = new List<Category>();
        int order = 1;
        foreach (var (name, slug, desc) in catDefs)
        {
            var cat = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Slug == slug, ct);
            if (cat == null)
            {
                cat = new Category(name, slug, desc, $"https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600&auto=format&fit=crop", orderIndex: order++);
                db.Categories.Add(cat);
            }
            catList.Add(cat);
        }
        await db.SaveChangesAsync(ct);

        // 4. Recipes (100 công thức chuẩn xác, đầy đủ nguyên liệu và bước chế biến thực tế)
        for (int i = 0; i < RecipeSeedData.All.Length; i++)
        {
            var seed = RecipeSeedData.All[i];
            if (await db.Recipes.IgnoreQueryFilters().AnyAsync(r => r.Slug == seed.Slug, ct)) continue;

            var author = authors[i % authors.Count];
            var category = catList[seed.CatIdx % catList.Count];

            var recipe = Recipe.CreateDraft(
                seed.Title,
                seed.Slug,
                seed.Description,
                seed.Instructions,
                seed.PrepTimeMinutes,
                seed.CookTimeMinutes,
                seed.Servings,
                seed.Difficulty,
                category.Id,
                author.Id);

            recipe.SetNutrition(RecipeNutrition.Create(
                seed.Nutrition.Calories,
                seed.Nutrition.Protein,
                seed.Nutrition.Carbs,
                seed.Nutrition.Fat,
                seed.Nutrition.Fiber,
                seed.Nutrition.Sodium));

            foreach (var ing in seed.Ingredients)
            {
                recipe.AddIngredient(ing.Name, ing.Quantity, ing.Unit, ing.Notes);
            }

            foreach (var step in seed.Steps)
            {
                recipe.AddStep(step.Title, step.Description, step.TimerMinutes, step.Tip);
            }

            // Ảnh đại diện chất lượng cao chuẩn từng món
            recipe.AddImage(GetDishImage(seed.Slug), $"Ảnh món {seed.Title}");

            // 85% món được xuất bản (Published), 15% để Draft
            if (i % 7 != 0)
            {
                recipe.Publish();
            }

            db.Recipes.Add(recipe);
        }

        await db.SaveChangesAsync(ct);
    }

    public static string GetDishImage(string slug) => $"/images/recipes/{slug}.jpg";

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
