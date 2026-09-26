using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using FluentValidation;
using MediatR;
using Recipe = CulinaryBlog.Domain.Entities.Recipe;

namespace CulinaryBlog.Application;

// =====================================================================
// C2 — Create / Update / Detail công thức   (FR-RCP-002, 003, 004)
// C3 — CRUD nguyên liệu và bước             (FR-RCP-009, 010)
//
// Quy ước bám theo code đã có trên main:
//   - AppException(status, code, message), mã lỗi kiểu "recipe.not_found"
//   - ICurrentUser của TV1 (Auth.cs): chỉ có UserId + IsInRole
//   - SlugHelper.GenerateSlug của TV2 (Domain/SlugHelper.cs)
//   - Envelope { data = ... } bọc ở Program.cs, không bọc trong handler
// =====================================================================

#region DTO

public sealed record RecipeDto(
    Guid Id, string Title, string Slug, string Description, string Instructions,
    int PrepTimeMinutes, int CookTimeMinutes, int Servings,
    string Difficulty, string Status, DateTime? PublishedAt,
    Guid CategoryId, string AuthorId, NutritionDto? Nutrition,
    string RowVersion,                      // base64 — client gửi lại khi update (D19)
    DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record RecipeDetailDto(
    Guid Id, string Title, string Slug, string Description, string Instructions,
    int PrepTimeMinutes, int CookTimeMinutes, int Servings, int TotalTimeMinutes,
    string Difficulty, string Status, DateTime? PublishedAt,
    Guid CategoryId, string AuthorId, NutritionDto? Nutrition,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    IReadOnlyList<RecipeStepDto> Steps,
    IReadOnlyList<RecipeImageSummaryDto> Images,
    string RowVersion, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record NutritionDto(
    decimal? Calories, decimal? Protein, decimal? Carbohydrates,
    decimal? Fat, decimal? Fiber, decimal? Sodium);

public sealed record RecipeIngredientDto(
    Guid Id, Guid RecipeId, string Name, decimal? Quantity,
    string? Unit, string? Notes, int OrderIndex);

public sealed record RecipeStepDto(
    Guid Id, Guid RecipeId, int StepNumber, string Title,
    string Description, int? TimerMinutes, string? ImageUrl);

/// <summary>Tóm tắt ảnh trong detail. DTO đầy đủ thuộc TV4 — xem docs/IMAGE_CONTRACT.md.</summary>
public sealed record RecipeImageSummaryDto(
    Guid Id, string OriginalUrl, string? MediumUrl, string? ThumbnailUrl,
    string? AltText, bool IsPrimary, int OrderIndex);

// Body record — RecipeId lấy từ route, không nhận từ JSON
public sealed record UpdateRecipeBody(
    string Title, string Description, string? Instructions,
    int PrepTimeMinutes, int CookTimeMinutes, int Servings,
    RecipeDifficulty Difficulty, Guid CategoryId, NutritionDto? Nutrition, string? RowVersion);

public sealed record IngredientBody(string Name, decimal? Quantity, string? Unit, string? Notes);
public sealed record StepBody(string Title, string Description, int? TimerMinutes, string? ImageUrl);
public sealed record ReorderStepsBody(IReadOnlyList<Guid> OrderedStepIds);

internal static class RecipeMapper
{
    private static string Rv(byte[] rowVersion) => Convert.ToBase64String(rowVersion);

    public static NutritionDto? ToDto(this RecipeNutrition? n) => n is null ? null
        : new NutritionDto(n.Calories, n.Protein, n.Carbohydrates, n.Fat, n.Fiber, n.Sodium);

    public static RecipeDto ToDto(this Recipe r) => new(
        r.Id, r.Title, r.Slug, r.Description, r.Instructions,
        r.PrepTimeMinutes, r.CookTimeMinutes, r.Servings,
        r.Difficulty.ToString(), r.Status.ToString(), r.PublishedAt, r.CategoryId, r.AuthorId,
        r.Nutrition.ToDto(), Rv(r.RowVersion), r.CreatedAt, r.UpdatedAt);

    public static RecipeDetailDto ToDetailDto(this Recipe r) => new(
        r.Id, r.Title, r.Slug, r.Description, r.Instructions,
        r.PrepTimeMinutes, r.CookTimeMinutes, r.Servings,
        r.PrepTimeMinutes + r.CookTimeMinutes,
        r.Difficulty.ToString(), r.Status.ToString(), r.PublishedAt, r.CategoryId, r.AuthorId,
        r.Nutrition.ToDto(),
        r.Ingredients.OrderBy(i => i.OrderIndex).Select(i => i.ToDto()).ToList(),
        r.Steps.OrderBy(s => s.StepNumber).Select(s => s.ToDto()).ToList(),
        r.Images.OrderBy(i => i.OrderIndex)
            .Select(i => new RecipeImageSummaryDto(
                i.Id, i.OriginalUrl, i.MediumUrl, i.ThumbnailUrl, i.AltText, i.IsPrimary, i.OrderIndex))
            .ToList(),
        Rv(r.RowVersion), r.CreatedAt, r.UpdatedAt);

    public static RecipeIngredientDto ToDto(this RecipeIngredient i) =>
        new(i.Id, i.RecipeId, i.Name, i.Quantity, i.Unit, i.Notes, i.OrderIndex);

    public static RecipeStepDto ToDto(this RecipeStep s) =>
        new(s.Id, s.RecipeId, s.StepNumber, s.Title, s.Description, s.TimerMinutes, s.ImageUrl);
}

#endregion

#region Helper dùng chung

public interface IRecipeRepository
{
    /// <summary>Nạp recipe kèm nguyên liệu và bước (có theo dõi thay đổi) để ghi.</summary>
    Task<Recipe?> FindForWriteAsync(Guid id, CancellationToken ct);

    /// <summary>Nạp recipe theo slug kèm toàn bộ con (chỉ đọc) cho màn chi tiết.</summary>
    Task<Recipe?> FindBySlugAsync(string slug, CancellationToken ct);

    /// <summary>Các slug đã dùng, kể cả bản ghi đã soft delete (vẫn chiếm unique index).</summary>
    Task<IReadOnlyList<string>> FindUsedSlugsAsync(string baseSlug, Guid? excludeRecipeId, CancellationToken ct);

    Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken ct);

    void Add(Recipe recipe);
    void Remove(Recipe recipe);
    void RemoveIngredient(RecipeIngredient ingredient);
    void RemoveStep(RecipeStep step);

    Task SaveChangesAsync(CancellationToken ct);
}

internal static class RecipeGuard
{
    /// <summary>
    /// Sinh slug duy nhất bằng SlugHelper của TV2, thêm suffix -1, -2… khi trùng (D14).
    /// IgnoreQueryFilters vì bản ghi đã soft delete VẪN chiếm slug trong unique index.
    /// </summary>
    public static async Task<string> UniqueSlugAsync(
        IRecipeRepository repo, string title, Guid? excludeRecipeId, CancellationToken ct)
    {
        var baseSlug = SlugHelper.GenerateSlug(title);
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "cong-thuc";

        var taken = await repo.FindUsedSlugsAsync(baseSlug, excludeRecipeId, ct);
        if (!taken.Contains(baseSlug)) return baseSlug;

        for (var i = 1; i <= 100; i++)
        {
            var candidate = $"{baseSlug}-{i}";
            if (!taken.Contains(candidate)) return candidate;
        }

        throw new AppException(409, "recipe.slug_exists", "Không sinh được slug duy nhất.");
    }

    /// <summary>Nạp recipe kèm child và kiểm quyền ghi. 401/404/403 (NFR-SEC-006).</summary>
    public static async Task<Recipe> LoadOwnedAsync(
        IRecipeRepository repo, ICurrentUser currentUser, Guid recipeId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(currentUser.UserId))
            throw new AppException(401, "auth.unauthorized", "Cần đăng nhập.");

        var recipe = await repo.FindForWriteAsync(recipeId, ct)
            ?? throw new AppException(404, "recipe.not_found", "Không tìm thấy công thức.");

        if (recipe.AuthorId != currentUser.UserId && !currentUser.IsInRole(Roles.Admin))
            throw new AppException(403, "recipe.forbidden", "Bạn không có quyền với công thức này.");

        return recipe;
    }

    /// <summary>So khớp RowVersion client gửi lên. Lệch thì 422 (D02) trước cả khi chạm DB.</summary>
    public static void EnsureVersion(Recipe recipe, string? expectedRowVersion)
    {
        if (string.IsNullOrEmpty(expectedRowVersion)) return;

        if (Convert.ToBase64String(recipe.RowVersion) != expectedRowVersion)
            throw new AppException(422, "recipe.concurrency_conflict",
                "Dữ liệu đã được người khác thay đổi. Vui lòng tải lại.");
    }

    public static async Task EnsureCategoryExistsAsync(
        IRecipeRepository repo, Guid categoryId, CancellationToken ct)
    {
        if (!await repo.CategoryExistsAsync(categoryId, ct))
            throw new AppException(404, "category.not_found", "Không tìm thấy danh mục.");
    }
}

public sealed class NutritionValidator : AbstractValidator<NutritionDto>
{
    public NutritionValidator()
    {
        RuleFor(x => x.Calories).GreaterThanOrEqualTo(0).When(x => x.Calories.HasValue);
        RuleFor(x => x.Protein).GreaterThanOrEqualTo(0).When(x => x.Protein.HasValue);
        RuleFor(x => x.Carbohydrates).GreaterThanOrEqualTo(0).When(x => x.Carbohydrates.HasValue);
        RuleFor(x => x.Fat).GreaterThanOrEqualTo(0).When(x => x.Fat.HasValue);
        RuleFor(x => x.Fiber).GreaterThanOrEqualTo(0).When(x => x.Fiber.HasValue);
        RuleFor(x => x.Sodium).GreaterThanOrEqualTo(0).When(x => x.Sodium.HasValue);
    }
}

#endregion

#region C2.1 — Tạo công thức (FR-RCP-003)

public sealed record CreateRecipeCommand(
    string Title, string Description, string? Instructions,
    int PrepTimeMinutes, int CookTimeMinutes, int Servings,
    RecipeDifficulty Difficulty, Guid CategoryId, NutritionDto? Nutrition)
    : IRequest<RecipeDto>;

public sealed class CreateRecipeValidator : AbstractValidator<CreateRecipeCommand>
{
    public CreateRecipeValidator()
    {
        RuleFor(x => x.Title).NotEmpty().Length(5, 200);
        RuleFor(x => x.Description).NotNull().MaximumLength(2000);
        RuleFor(x => x.PrepTimeMinutes).GreaterThan(0);
        RuleFor(x => x.CookTimeMinutes).GreaterThanOrEqualTo(0);   // SRS 7.2 — 0 cho món no-cook
        RuleFor(x => x.Servings).GreaterThan(0);
        RuleFor(x => x.Difficulty).IsInEnum();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Nutrition!).SetValidator(new NutritionValidator()).When(x => x.Nutrition is not null);
    }
}

public sealed class CreateRecipeHandler(IRecipeRepository repo, ICurrentUser currentUser)
    : IRequestHandler<CreateRecipeCommand, RecipeDto>
{
    public async Task<RecipeDto> Handle(CreateRecipeCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(currentUser.UserId))
            throw new AppException(401, "auth.unauthorized", "Cần đăng nhập.");

        await RecipeGuard.EnsureCategoryExistsAsync(repo, cmd.CategoryId, ct);

        var slug = await RecipeGuard.UniqueSlugAsync(repo, cmd.Title, null, ct);

        // AuthorId LUÔN lấy từ token (FR-RCP-003) — không bao giờ nhận từ body
        var recipe = Recipe.CreateDraft(
            cmd.Title, slug, cmd.Description, cmd.Instructions,
            cmd.PrepTimeMinutes, cmd.CookTimeMinutes, cmd.Servings,
            cmd.Difficulty, cmd.CategoryId, currentUser.UserId);

        if (cmd.Nutrition is { } n)
            recipe.SetNutrition(RecipeNutrition.Create(
                n.Calories, n.Protein, n.Carbohydrates, n.Fat, n.Fiber, n.Sodium));

        repo.Add(recipe);
        await repo.SaveChangesAsync(ct);   // unique index slug là chốt chặn cuối khi race
        return recipe.ToDto();
    }
}

#endregion

#region C2.2 — Cập nhật công thức (FR-RCP-004)

public sealed record UpdateRecipeCommand(
    Guid Id, string Title, string Description, string? Instructions,
    int PrepTimeMinutes, int CookTimeMinutes, int Servings,
    RecipeDifficulty Difficulty, Guid CategoryId, NutritionDto? Nutrition,
    string? RowVersion) : IRequest<RecipeDto>;

public sealed class UpdateRecipeValidator : AbstractValidator<UpdateRecipeCommand>
{
    public UpdateRecipeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().Length(5, 200);
        RuleFor(x => x.Description).NotNull().MaximumLength(2000);
        RuleFor(x => x.PrepTimeMinutes).GreaterThan(0);
        RuleFor(x => x.CookTimeMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Servings).GreaterThan(0);
        RuleFor(x => x.Difficulty).IsInEnum();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Nutrition!).SetValidator(new NutritionValidator()).When(x => x.Nutrition is not null);
    }
}

public sealed class UpdateRecipeHandler(IRecipeRepository repo, ICurrentUser currentUser)
    : IRequestHandler<UpdateRecipeCommand, RecipeDto>
{
    public async Task<RecipeDto> Handle(UpdateRecipeCommand cmd, CancellationToken ct)
    {
        var recipe = await RecipeGuard.LoadOwnedAsync(repo, currentUser, cmd.Id, ct);
        RecipeGuard.EnsureVersion(recipe, cmd.RowVersion);
        await RecipeGuard.EnsureCategoryExistsAsync(repo, cmd.CategoryId, ct);

        // D14: slug ỔN ĐỊNH sau publish. Chỉ Draft mới sinh lại slug khi đổi tiêu đề.
        var slug = recipe.Status == RecipeStatus.Draft && recipe.Title != cmd.Title
            ? await RecipeGuard.UniqueSlugAsync(repo, cmd.Title, recipe.Id, ct)
            : recipe.Slug;

        recipe.UpdateDetails(
            cmd.Title, slug, cmd.Description, cmd.Instructions,
            cmd.PrepTimeMinutes, cmd.CookTimeMinutes, cmd.Servings,
            cmd.Difficulty, cmd.CategoryId);

        if (cmd.Nutrition is { } n)
            recipe.SetNutrition(RecipeNutrition.Create(
                n.Calories, n.Protein, n.Carbohydrates, n.Fat, n.Fiber, n.Sodium));

        await repo.SaveChangesAsync(ct);
        return recipe.ToDto();
    }
}

#endregion

#region C2.3 — Xem chi tiết (FR-RCP-002)

/// <summary>Lấy theo slug. Draft/Archived chỉ owner hoặc Admin xem được (D12).</summary>
public sealed record GetRecipeBySlugQuery(string Slug) : IRequest<RecipeDetailDto>;

public sealed class GetRecipeBySlugHandler(IRecipeRepository repo, ICurrentUser currentUser)
    : IRequestHandler<GetRecipeBySlugQuery, RecipeDetailDto>
{
    public async Task<RecipeDetailDto> Handle(GetRecipeBySlugQuery q, CancellationToken ct)
    {
        var recipe = await repo.FindBySlugAsync(q.Slug, ct)
            ?? throw new AppException(404, "recipe.not_found", "Không tìm thấy công thức.");

        if (recipe.Status != RecipeStatus.Published)
        {
            var isOwner = !string.IsNullOrEmpty(currentUser.UserId) && recipe.AuthorId == currentUser.UserId;
            // Trả 404 thay vì 403: không tiết lộ sự tồn tại của Draft người khác
            if (!isOwner && !currentUser.IsInRole(Roles.Admin))
                throw new AppException(404, "recipe.not_found", "Không tìm thấy công thức.");
        }

        return recipe.ToDetailDto();
    }
}

#endregion

#region C3.1 — Nguyên liệu (FR-RCP-009)

public sealed record AddIngredientCommand(
    Guid RecipeId, string Name, decimal? Quantity, string? Unit, string? Notes)
    : IRequest<RecipeIngredientDto>;

public sealed class AddIngredientValidator : AbstractValidator<AddIngredientCommand>
{
    public AddIngredientValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);           // SRS 7.4
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);
        RuleFor(x => x.Unit).MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class AddIngredientHandler(IRecipeRepository repo, ICurrentUser currentUser)
    : IRequestHandler<AddIngredientCommand, RecipeIngredientDto>
{
    public async Task<RecipeIngredientDto> Handle(AddIngredientCommand cmd, CancellationToken ct)
    {
        var recipe = await RecipeGuard.LoadOwnedAsync(repo, currentUser, cmd.RecipeId, ct);
        var ingredient = recipe.AddIngredient(cmd.Name, cmd.Quantity, cmd.Unit, cmd.Notes);
        await repo.SaveChangesAsync(ct);
        return ingredient.ToDto();
    }
}

public sealed record UpdateIngredientCommand(
    Guid RecipeId, Guid IngredientId, string Name, decimal? Quantity, string? Unit, string? Notes)
    : IRequest<RecipeIngredientDto>;

public sealed class UpdateIngredientValidator : AbstractValidator<UpdateIngredientCommand>
{
    public UpdateIngredientValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.IngredientId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);
        RuleFor(x => x.Unit).MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class UpdateIngredientHandler(IRecipeRepository repo, ICurrentUser currentUser)
    : IRequestHandler<UpdateIngredientCommand, RecipeIngredientDto>
{
    public async Task<RecipeIngredientDto> Handle(UpdateIngredientCommand cmd, CancellationToken ct)
    {
        var recipe = await RecipeGuard.LoadOwnedAsync(repo, currentUser, cmd.RecipeId, ct);

        // Aggregate ném INGREDIENT_NOT_FOUND nếu id thuộc recipe khác — chặn cross-recipe
        recipe.UpdateIngredient(cmd.IngredientId, cmd.Name, cmd.Quantity, cmd.Unit, cmd.Notes);
        await repo.SaveChangesAsync(ct);

        return recipe.Ingredients.Single(i => i.Id == cmd.IngredientId).ToDto();
    }
}

public sealed record DeleteIngredientCommand(Guid RecipeId, Guid IngredientId) : IRequest;

public sealed class DeleteIngredientHandler(IRecipeRepository repo, ICurrentUser currentUser)
    : IRequestHandler<DeleteIngredientCommand>
{
    public async Task Handle(DeleteIngredientCommand cmd, CancellationToken ct)
    {
        var recipe = await RecipeGuard.LoadOwnedAsync(repo, currentUser, cmd.RecipeId, ct);
        var target = recipe.Ingredients.SingleOrDefault(i => i.Id == cmd.IngredientId)
                     ?? throw new AppException(404, "ingredient.not_found",
                            "Nguyên liệu không thuộc công thức này.");

        recipe.RemoveIngredient(cmd.IngredientId);   // aggregate tự đánh lại OrderIndex
        repo.RemoveIngredient(target);
        await repo.SaveChangesAsync(ct);
    }
}

#endregion

#region C3.2 — Bước thực hiện (FR-RCP-010)

public sealed record AddStepCommand(
    Guid RecipeId, string Title, string Description, int? TimerMinutes, string? ImageUrl)
    : IRequest<RecipeStepDto>;

public sealed class AddStepValidator : AbstractValidator<AddStepCommand>
{
    public AddStepValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);          // SRS 7.3 — Title BẮT BUỘC
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.TimerMinutes).GreaterThanOrEqualTo(0).When(x => x.TimerMinutes.HasValue);  // C05
        RuleFor(x => x.ImageUrl).MaximumLength(500);
    }
}

public sealed class AddStepHandler(IRecipeRepository repo, ICurrentUser currentUser)
    : IRequestHandler<AddStepCommand, RecipeStepDto>
{
    public async Task<RecipeStepDto> Handle(AddStepCommand cmd, CancellationToken ct)
    {
        var recipe = await RecipeGuard.LoadOwnedAsync(repo, currentUser, cmd.RecipeId, ct);

        // StepNumber do server quản lý, liên tục 1..N (D16) — không nhận từ client
        var step = recipe.AddStep(cmd.Title, cmd.Description, cmd.TimerMinutes, cmd.ImageUrl);
        await repo.SaveChangesAsync(ct);
        return step.ToDto();
    }
}

public sealed record UpdateStepCommand(
    Guid RecipeId, Guid StepId, string Title, string Description, int? TimerMinutes, string? ImageUrl)
    : IRequest<RecipeStepDto>;

public sealed class UpdateStepValidator : AbstractValidator<UpdateStepCommand>
{
    public UpdateStepValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.TimerMinutes).GreaterThanOrEqualTo(0).When(x => x.TimerMinutes.HasValue);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
    }
}

public sealed class UpdateStepHandler(IRecipeRepository repo, ICurrentUser currentUser)
    : IRequestHandler<UpdateStepCommand, RecipeStepDto>
{
    public async Task<RecipeStepDto> Handle(UpdateStepCommand cmd, CancellationToken ct)
    {
        var recipe = await RecipeGuard.LoadOwnedAsync(repo, currentUser, cmd.RecipeId, ct);
        recipe.UpdateStep(cmd.StepId, cmd.Title, cmd.Description, cmd.TimerMinutes, cmd.ImageUrl);
        await repo.SaveChangesAsync(ct);
        return recipe.Steps.Single(s => s.Id == cmd.StepId).ToDto();
    }
}

public sealed record DeleteStepCommand(Guid RecipeId, Guid StepId) : IRequest;

/// <summary>Xóa bước rồi đánh số lại 1..N — trong transaction vì đụng unique (RecipeId, StepNumber).</summary>
public sealed class DeleteStepHandler(
    IRecipeRepository repo, ICurrentUser currentUser, IUnitOfWork uow)
    : IRequestHandler<DeleteStepCommand>
{
    public async Task Handle(DeleteStepCommand cmd, CancellationToken ct)
    {
        var recipe = await RecipeGuard.LoadOwnedAsync(repo, currentUser, cmd.RecipeId, ct);
        var target = recipe.Steps.SingleOrDefault(s => s.Id == cmd.StepId)
                     ?? throw new AppException(404, "step.not_found", "Bước không thuộc công thức này.");

        await uow.ExecuteInTransactionAsync(_ =>
        {
            recipe.RemoveStep(cmd.StepId);   // aggregate renumber 1..N
            repo.RemoveStep(target);
            return Task.CompletedTask;
        }, ct);
    }
}

/// <summary>Đổi thứ tự các bước. OrderedStepIds phải đủ và đúng mọi bước hiện có.</summary>
public sealed record ReorderStepsCommand(Guid RecipeId, IReadOnlyList<Guid> OrderedStepIds)
    : IRequest<IReadOnlyList<RecipeStepDto>>;

public sealed class ReorderStepsValidator : AbstractValidator<ReorderStepsCommand>
{
    public ReorderStepsValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.OrderedStepIds).NotEmpty();
    }
}

public sealed class ReorderStepsHandler(
    IRecipeRepository repo, ICurrentUser currentUser, IUnitOfWork uow)
    : IRequestHandler<ReorderStepsCommand, IReadOnlyList<RecipeStepDto>>
{
    public async Task<IReadOnlyList<RecipeStepDto>> Handle(ReorderStepsCommand cmd, CancellationToken ct)
    {
        var recipe = await RecipeGuard.LoadOwnedAsync(repo, currentUser, cmd.RecipeId, ct);

        await uow.ExecuteInTransactionAsync(_ =>
        {
            recipe.ReorderSteps(cmd.OrderedStepIds);
            return Task.CompletedTask;
        }, ct);

        return recipe.Steps.OrderBy(s => s.StepNumber).Select(s => s.ToDto()).ToList();
    }
}

#endregion

#region D3.1/D3.2 — Publish / Unpublish công thức (FR-RCP-005/006, C02, D07)

// Publish yêu cầu >= 1 ingredient VÀ >= 1 step (C02). Recipe.Publish() ném
// DomainException("RECIPE_PUBLISH_INCOMPLETE") khi thiếu -> ApiExceptionHandler map 422.
public sealed record PublishRecipeCommand(Guid RecipeId) : IRequest<RecipeDto>;

public sealed class PublishRecipeValidator : AbstractValidator<PublishRecipeCommand>
{
    public PublishRecipeValidator() => RuleFor(x => x.RecipeId).NotEmpty();
}

public sealed class PublishRecipeHandler(IRecipeRepository repo, ICurrentUser currentUser)
    : IRequestHandler<PublishRecipeCommand, RecipeDto>
{
    public async Task<RecipeDto> Handle(PublishRecipeCommand cmd, CancellationToken ct)
    {
        var recipe = await RecipeGuard.LoadOwnedAsync(repo, currentUser, cmd.RecipeId, ct);

        // Idempotent: đã Published -> giữ Published, PublishedAt giữ nguyên (D07). Thiếu thành phần -> RECIPE_PUBLISH_INCOMPLETE (422).
        recipe.Publish();

        await repo.SaveChangesAsync(ct);
        return recipe.ToDto();
    }
}

public sealed record UnpublishRecipeCommand(Guid RecipeId) : IRequest<RecipeDto>;

public sealed class UnpublishRecipeValidator : AbstractValidator<UnpublishRecipeCommand>
{
    public UnpublishRecipeValidator() => RuleFor(x => x.RecipeId).NotEmpty();
}

public sealed class UnpublishRecipeHandler(IRecipeRepository repo, ICurrentUser currentUser)
    : IRequestHandler<UnpublishRecipeCommand, RecipeDto>
{
    public async Task<RecipeDto> Handle(UnpublishRecipeCommand cmd, CancellationToken ct)
    {
        var recipe = await RecipeGuard.LoadOwnedAsync(repo, currentUser, cmd.RecipeId, ct);

        // Published -> Draft ngay (D13: public ẩn, không giữ cache cũ). Idempotent nếu đã Draft.
        recipe.Unpublish();

        await repo.SaveChangesAsync(ct);
        return recipe.ToDto();
    }
}

#endregion

#region C2.4 — Xoá công thức (FR-RCP-007) — soft delete (ADR-0001)

// Xoá mềm: set IsDeleted, global query filter tự ẩn khỏi mọi truy vấn.
// Con (ingredient/step/image) để nguyên — ON DELETE CASCADE chỉ chạy khi hard delete;
// với soft delete ta chỉ cần ẩn aggregate gốc là đủ (D-recipe không lộ qua filter).
public sealed record DeleteRecipeCommand(Guid Id, string? RowVersion) : IRequest;

public sealed class DeleteRecipeValidator : AbstractValidator<DeleteRecipeCommand>
{
    public DeleteRecipeValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class DeleteRecipeHandler(IRecipeRepository repo, ICurrentUser currentUser)
    : IRequestHandler<DeleteRecipeCommand>
{
    public async Task Handle(DeleteRecipeCommand cmd, CancellationToken ct)
    {
        var recipe = await RecipeGuard.LoadOwnedAsync(repo, currentUser, cmd.Id, ct);
        RecipeGuard.EnsureVersion(recipe, cmd.RowVersion);   // 422 nếu bản ghi đã đổi ở nơi khác

        recipe.SoftDelete();                 // set IsDeleted; interceptor cập nhật RowVersion
        await repo.SaveChangesAsync(ct);
    }
}

#endregion
