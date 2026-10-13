using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// BUG-W4-01 — hồi quy ở mức MODEL: Id của các entity con (RecipeImage/RecipeStep/RecipeIngredient)
/// sinh ở domain (`Guid.NewGuid`), nên EF **buộc** phải coi là <c>ValueGenerated.Never</c>.
///
/// Nếu ai đó xoá dòng `b.Property(i => i.Id).ValueGeneratedNever()` trong 
/// <c>*Configuration.cs</c> thì EF lại coi Id là "sinh khi thêm" (ValueGeneratedOnAdd), ảnh/bước
/// mới thêm vào collection bị coi là dòng đã tồn tại -> phát UPDATE 0 dòng ->
/// <c>DbUpdateConcurrencyException</c> → 422 `recipe.version_conflict` (đúng lỗi `BUG-W4-01`).
///
/// Test này KHÔNG cần Postgres: chỉ dựng metadata model (không mở kết nối), nên chạy nhanh và
/// luôn chạy được kể cả khi không có DB — bù cho test tích hợp đắt đỏ ở
/// `RecipeImageUploadPersistenceTests` (đã phủ cùng bug qua đường API thật).
/// </summary>
public sealed class RecipeChildIdGenerationTests
{
    private static Microsoft.EntityFrameworkCore.Metadata.IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=postgres;Password=postgres")
            .Options;
        using var db = new AuthDbContext(options);
        return db.Model;
    }

    [Theory]
    [InlineData(typeof(RecipeImage))]
    [InlineData(typeof(RecipeStep))]
    [InlineData(typeof(RecipeIngredient))]
    public void Child_entity_Id_phai_la_ValueGeneratedNever(Type entityType)
    {
        var model = BuildModel();
        var entity = model.FindEntityType(entityType);
        Assert.NotNull(entity);
        var id = entity!.FindProperty("Id");
        Assert.NotNull(id);
        Assert.Equal(ValueGenerated.Never, id!.ValueGenerated);
    }
}
