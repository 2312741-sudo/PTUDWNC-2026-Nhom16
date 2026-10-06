using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using CulinaryBlog.Application;
using CulinaryBlog.Domain.Enums;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>MeterListener nghe toàn process -> chạy riêng, không song song với test khác để đếm chính xác.</summary>
[CollectionDefinition("recipe-metrics", DisableParallelization = true)]
public sealed class RecipeMetricsCollection { }

/// <summary>
/// K20 (SP, TV3) — metric nghiệp vụ của công thức: Meter "CulinaryBlog.Recipes" đếm số công thức tạo/sửa thành công.
/// Lỗi (chưa đăng nhập, không phải chủ sở hữu) thì KHÔNG đếm. Dùng lại FakeAuthoringRecipeRepository / FakeTestCurrentUser.
/// </summary>
[Collection("recipe-metrics")]
public sealed class RecipeMetricsTests : IDisposable
{
    private readonly FakeAuthoringRecipeRepository _repo = new();
    private readonly FakeTestCurrentUser _user = new();
    private readonly Guid _catId = Guid.NewGuid();
    private readonly MeterListener _listener = new();
    private readonly ConcurrentQueue<(string Name, long Value)> _measurements = new();

    public RecipeMetricsTests()
    {
        _repo.Categories.Add(_catId);
        _listener.InstrumentPublished = (i, l) =>
        {
            if (i.Meter.Name == RecipeMetrics.MeterName) l.EnableMeasurementEvents(i);
        };
        _listener.SetMeasurementEventCallback<long>((i, v, _, _) => _measurements.Enqueue((i.Name, v)));
        _listener.Start();
    }

    public void Dispose() => _listener.Dispose();

    private CreateRecipeCommand NewCommand(string title) => new(
        title, "Mô tả", null, 10, 20, 2, RecipeDifficulty.Easy, _catId, null);

    private long Sum(string name) => _measurements.Where(m => m.Name == name).Sum(m => m.Value);

    [Fact]
    public async Task Tao_cong_thuc_thanh_cong_tang_counter_created_dung_1()
    {
        await new CreateRecipeHandler(_repo, _user).Handle(NewCommand("Canh chua cá lóc"), CancellationToken.None);

        Assert.Equal(1, Sum("culinary.recipes.created"));
        Assert.Equal(0, Sum("culinary.recipes.updated"));
    }

    [Fact]
    public async Task Sua_cong_thuc_thanh_cong_tang_counter_updated_dung_1()
    {
        var created = await new CreateRecipeHandler(_repo, _user).Handle(NewCommand("Gà kho gừng"), CancellationToken.None);

        await new UpdateRecipeHandler(_repo, _user).Handle(new UpdateRecipeCommand(
            created.Id, "Gà kho gừng sả", "Mô tả mới", null, 10, 25, 3, RecipeDifficulty.Medium, _catId, null, null),
            CancellationToken.None);

        Assert.Equal(1, Sum("culinary.recipes.updated"));
    }

    [Fact]
    public async Task Chua_dang_nhap_hoac_khong_phai_chu_so_huu_thi_khong_dem()
    {
        var created = await new CreateRecipeHandler(_repo, _user).Handle(NewCommand("Bò lúc lắc"), CancellationToken.None);
        var anonymous = new FakeTestCurrentUser { UserId = null };
        var stranger = new FakeTestCurrentUser { UserId = "author-khac" };

        await Assert.ThrowsAnyAsync<Exception>(() =>
            new CreateRecipeHandler(_repo, anonymous).Handle(NewCommand("Không có token"), CancellationToken.None));
        await Assert.ThrowsAnyAsync<Exception>(() =>
            new UpdateRecipeHandler(_repo, stranger).Handle(new UpdateRecipeCommand(
                created.Id, "Sửa trộm", "Mô tả", null, 10, 20, 2, RecipeDifficulty.Easy, _catId, null, null),
                CancellationToken.None));

        Assert.Equal(1, Sum("culinary.recipes.created")); // chỉ lần tạo hợp lệ
        Assert.Equal(0, Sum("culinary.recipes.updated"));
    }
}
