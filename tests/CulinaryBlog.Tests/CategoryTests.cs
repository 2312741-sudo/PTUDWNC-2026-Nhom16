using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using FluentValidation;
using MediatR;
using Xunit;

namespace CulinaryBlog.Tests;

public sealed class FakeCategoryRepository : ICategoryRepository
{
    public readonly List<Category> Categories = [];
    public readonly Dictionary<Guid, int> RecipeCounts = [];

    public Task<IReadOnlyList<Category>> GetAllAsync(bool onlyWithRecipes, CancellationToken ct)
    {
        var result = Categories.Where(c => !c.IsDeleted);
        if (onlyWithRecipes)
        {
            result = result.Where(c => RecipeCounts.GetValueOrDefault(c.Id, 0) > 0);
        }

        return Task.FromResult<IReadOnlyList<Category>>(
            result.OrderBy(c => c.OrderIndex).ThenBy(c => c.Name).ToList());
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Categories.FirstOrDefault(c => c.Id == id && !c.IsDeleted));

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken ct) =>
        Task.FromResult(Categories.FirstOrDefault(c => c.Slug == slug && !c.IsDeleted));

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct) =>
        Task.FromResult(Categories.Any(c => !c.IsDeleted && c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && (!excludeId.HasValue || c.Id != excludeId.Value)));

    public Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId, CancellationToken ct) =>
        Task.FromResult(Categories.Any(c => !c.IsDeleted && c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase) && (!excludeId.HasValue || c.Id != excludeId.Value)));

    public Task<int> CountRecipesAsync(Guid categoryId, CancellationToken ct) =>
        Task.FromResult(RecipeCounts.GetValueOrDefault(categoryId, 0));

    public Task AddAsync(Category category, CancellationToken ct)
    {
        Categories.Add(category);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Category category, CancellationToken ct) => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

public sealed class CategoryTests
{
    [Fact]
    public void Category_domain_creates_and_validates_properties()
    {
        var cat = new Category("Món Khai Vị", "mon-khai-vi", "Mô tả món khai vị", "https://example.com/img.jpg", 1);
        Assert.Equal("Món Khai Vị", cat.Name);
        Assert.Equal("mon-khai-vi", cat.Slug);
        Assert.Equal("Mô tả món khai vị", cat.Description);
        Assert.Equal("https://example.com/img.jpg", cat.ImageUrl);
        Assert.Equal(1, cat.OrderIndex);
        Assert.False(cat.IsDeleted);

        cat.Update("Món Khai Vị Mới", "Mô tả mới", null, 2);
        Assert.Equal("Món Khai Vị Mới", cat.Name);
        Assert.Equal(2, cat.OrderIndex);
        Assert.Null(cat.ImageUrl);

        cat.MarkDeleted();
        Assert.True(cat.IsDeleted);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")] // < 2 chars
    [InlineData("<script>alert(1)</script>")]
    public void Category_domain_rejects_invalid_names(string invalidName)
    {
        Assert.Throws<ArgumentException>(() => new Category(invalidName, "slug"));
    }

    [Fact]
    public void SlugHelper_generates_vietnamese_slug_correctly()
    {
        Assert.Equal("mon-khai-vi-an-vat", SlugHelper.GenerateSlug("Món Khai Vị & Ăn Vặt!"));
        Assert.Equal("do-uong-thanh-mat", SlugHelper.GenerateSlug("Đồ uống thanh mát"));
        Assert.Equal("am-thuc-viet-nam", SlugHelper.GenerateSlug("  Ẩm thực Việt Nam  "));
    }

    [Fact]
    public async Task CreateCategory_validator_catches_invalid_inputs()
    {
        var validator = new CreateCategoryValidator();

        var invalidCmd = new CreateCategoryCommand("A", null, null, -1);
        var result = await validator.ValidateAsync(invalidCmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "OrderIndex");
    }

    [Fact]
    public async Task CreateCategory_handler_creates_and_generates_unique_slug()
    {
        var repo = new FakeCategoryRepository();
        var handler = new CreateCategoryHandler(repo);

        var cmd1 = new CreateCategoryCommand("Món Tráng Miệng", "Bánh và chè", null, 1);
        var res1 = await handler.Handle(cmd1, CancellationToken.None);

        Assert.Equal("Món Tráng Miệng", res1.Name);
        Assert.Equal("mon-trang-mieng", res1.Slug);
        Assert.Single(repo.Categories);

        // Tạo cùng tên -> Conflict 409
        await Assert.ThrowsAsync<AppException>(() => handler.Handle(cmd1, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateCategory_preserves_slug_and_validates_uniqueness()
    {
        var repo = new FakeCategoryRepository();
        var createHandler = new CreateCategoryHandler(repo);
        var updateHandler = new UpdateCategoryHandler(repo);

        var created = await createHandler.Handle(new("Món Nướng", null, null, 1), CancellationToken.None);
        Assert.Equal("mon-nuong", created.Slug);

        // Update tên khác nhưng slug giữ nguyên theo D15/ADR
        var updated = await updateHandler.Handle(new(created.Id, "Món Nướng Đặc Biệt", "Nướng than hoa", null, 2), CancellationToken.None);
        Assert.Equal("Món Nướng Đặc Biệt", updated.Name);
        Assert.Equal("mon-nuong", updated.Slug); // Slug được giữ nguyên
        Assert.Equal(2, updated.OrderIndex);
    }

    [Fact]
    public async Task DeleteCategory_blocks_when_recipes_exist_and_soft_deletes_when_empty()
    {
        var repo = new FakeCategoryRepository();
        var createHandler = new CreateCategoryHandler(repo);
        var deleteHandler = new DeleteCategoryHandler(repo);

        var cat1 = await createHandler.Handle(new("Món Canh", null, null, 1), CancellationToken.None);
        repo.RecipeCounts[cat1.Id] = 3; // Có 3 món ăn trong danh mục

        // Chặn xoá vì còn công thức -> 409 Conflict (FR-CAT-005)
        var ex = await Assert.ThrowsAsync<AppException>(() => deleteHandler.Handle(new(cat1.Id), CancellationToken.None));
        Assert.Equal(409, ex.Status);
        Assert.Equal("category.delete_has_recipes", ex.Code);

        // Danh mục rỗng -> xoá thành công (soft-delete)
        var cat2 = await createHandler.Handle(new("Món Trộn", null, null, 2), CancellationToken.None);
        await deleteHandler.Handle(new(cat2.Id), CancellationToken.None);

        var deletedCat = repo.Categories.First(c => c.Id == cat2.Id);
        Assert.True(deletedCat.IsDeleted);
    }

    [Fact]
    public async Task GetCategories_orders_by_order_index_then_name()
    {
        var repo = new FakeCategoryRepository();
        var handler = new GetCategoriesHandler(repo);

        repo.Categories.Add(new("Salad", "salad", null, null, 2));
        repo.Categories.Add(new("Khai vị", "khai-vi", null, null, 1));
        repo.Categories.Add(new("Bánh mì", "banh-mi", null, null, 1));

        var list = await handler.Handle(new(), CancellationToken.None);

        Assert.Equal(3, list.Count);
        // Cùng OrderIndex = 1 thì sắp theo Name: "Bánh mì" trước "Khai vị"
        Assert.Equal("Bánh mì", list[0].Name);
        Assert.Equal("Khai vị", list[1].Name);
        Assert.Equal("Salad", list[2].Name);
    }

    [Fact]
    public void PaginationMeta_calculates_page_bounds_correctly()
    {
        var meta = PaginationMeta.Create(page: 2, pageSize: 12, total: 25);
        Assert.Equal(2, meta.Page);
        Assert.Equal(12, meta.PageSize);
        Assert.Equal(25, meta.Total);
        Assert.Equal(3, meta.TotalPages); // 25 / 12 = 2.08 -> 3
        Assert.True(meta.HasNextPage);
        Assert.True(meta.HasPreviousPage);

        var lastPageMeta = PaginationMeta.Create(page: 3, pageSize: 12, total: 25);
        Assert.False(lastPageMeta.HasNextPage);
        Assert.True(lastPageMeta.HasPreviousPage);
    }
}
