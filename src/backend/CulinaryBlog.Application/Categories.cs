using CulinaryBlog.Domain;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    int OrderIndex,
    int RecipesCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateCategoryCommand(
    string Name,
    string? Description,
    string? ImageUrl,
    int OrderIndex) : IRequest<CategoryDto>;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    int OrderIndex) : IRequest<CategoryDto>;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Unit>;

public sealed record GetCategoriesQuery(bool OnlyWithRecipes = false) : IRequest<IReadOnlyList<CategoryDto>>;

public sealed record GetCategoryBySlugQuery(string Slug) : IRequest<CategoryDto>;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(bool onlyWithRecipes, CancellationToken ct);
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct);
    Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId, CancellationToken ct);
    Task<int> CountRecipesAsync(Guid categoryId, CancellationToken ct);
    Task AddAsync(Category category, CancellationToken ct);
    Task UpdateAsync(Category category, CancellationToken ct);
    Task DeleteAsync(Category category, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public sealed class CreateCategoryHandler(ICategoryRepository repository) : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var trimmedName = request.Name.Trim();
        if (await repository.ExistsByNameAsync(trimmedName, null, ct))
        {
            throw new AppException(409, "category.name_exists", "Tên danh mục đã tồn tại.");
        }

        var baseSlug = SlugHelper.GenerateSlug(trimmedName);
        if (string.IsNullOrEmpty(baseSlug))
        {
            baseSlug = "danh-muc";
        }

        var uniqueSlug = baseSlug;
        var counter = 1;
        while (await repository.ExistsBySlugAsync(uniqueSlug, null, ct))
        {
            uniqueSlug = $"{baseSlug}-{counter++}";
        }

        var category = new Category(
            name: trimmedName,
            slug: uniqueSlug,
            description: request.Description,
            imageUrl: request.ImageUrl,
            orderIndex: request.OrderIndex);

        await repository.AddAsync(category, ct);
        await repository.SaveChangesAsync(ct);

        return ToDto(category, 0);
    }

    private static CategoryDto ToDto(Category c, int recipesCount) => new(
        c.Id,
        c.Name,
        c.Slug,
        c.Description,
        c.ImageUrl,
        c.OrderIndex,
        recipesCount,
        c.CreatedAt,
        c.UpdatedAt);
}

public sealed class UpdateCategoryHandler(ICategoryRepository repository) : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await repository.GetByIdAsync(request.Id, ct)
            ?? throw new AppException(404, "category.not_found", "Không tìm thấy danh mục.");

        var trimmedName = request.Name.Trim();
        if (await repository.ExistsByNameAsync(trimmedName, request.Id, ct))
        {
            throw new AppException(409, "category.name_exists", "Tên danh mục đã tồn tại.");
        }

        // According to D15 / ADR, category slug is preserved on update to maintain SEO links
        category.Update(
            name: trimmedName,
            description: request.Description,
            imageUrl: request.ImageUrl,
            orderIndex: request.OrderIndex);

        await repository.UpdateAsync(category, ct);
        await repository.SaveChangesAsync(ct);

        var recipesCount = await repository.CountRecipesAsync(category.Id, ct);
        return new(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageUrl,
            category.OrderIndex,
            recipesCount,
            category.CreatedAt,
            category.UpdatedAt);
    }
}

public sealed class DeleteCategoryHandler(ICategoryRepository repository) : IRequestHandler<DeleteCategoryCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await repository.GetByIdAsync(request.Id, ct)
            ?? throw new AppException(404, "category.not_found", "Không tìm thấy danh mục.");

        var recipesCount = await repository.CountRecipesAsync(category.Id, ct);
        if (recipesCount > 0)
        {
            throw new AppException(409, "category.delete_has_recipes",
                "Không thể xoá danh mục vì vẫn còn công thức liên kết.");
        }

        await repository.DeleteAsync(category, ct);
        await repository.SaveChangesAsync(ct);

        return Unit.Value;
    }
}

public sealed class GetCategoriesHandler(ICategoryRepository repository) : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var categories = await repository.GetAllAsync(request.OnlyWithRecipes, ct);
        var dtos = new List<CategoryDto>(categories.Count);

        foreach (var category in categories)
        {
            var count = await repository.CountRecipesAsync(category.Id, ct);
            dtos.Add(new(
                category.Id,
                category.Name,
                category.Slug,
                category.Description,
                category.ImageUrl,
                category.OrderIndex,
                count,
                category.CreatedAt,
                category.UpdatedAt));
        }

        return dtos;
    }
}

public sealed class GetCategoryBySlugHandler(ICategoryRepository repository) : IRequestHandler<GetCategoryBySlugQuery, CategoryDto>
{
    public async Task<CategoryDto> Handle(GetCategoryBySlugQuery request, CancellationToken ct)
    {
        var category = await repository.GetBySlugAsync(request.Slug.Trim().ToLowerInvariant(), ct)
            ?? throw new AppException(404, "category.not_found", "Không tìm thấy danh mục.");

        var count = await repository.CountRecipesAsync(category.Id, ct);
        return new(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageUrl,
            category.OrderIndex,
            count,
            category.CreatedAt,
            category.UpdatedAt);
    }
}

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên danh mục không được để trống.")
            .MinimumLength(2).WithMessage("Tên danh mục phải có từ 2 ký tự trở lên.")
            .MaximumLength(100).WithMessage("Tên danh mục không được vượt quá 100 ký tự.")
            .Must(x => x is not null && !x.Any(char.IsControl) && !x.Contains('<') && !x.Contains('>'))
            .WithMessage("Tên danh mục không được chứa ký tự điều khiển hoặc thẻ HTML.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Đường dẫn ảnh không được vượt quá 500 ký tự.");

        RuleFor(x => x.OrderIndex)
            .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị phải lớn hơn hoặc bằng 0.");
    }
}

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mã định danh danh mục không được để trống.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên danh mục không được để trống.")
            .MinimumLength(2).WithMessage("Tên danh mục phải có từ 2 ký tự trở lên.")
            .MaximumLength(100).WithMessage("Tên danh mục không được vượt quá 100 ký tự.")
            .Must(x => x is not null && !x.Any(char.IsControl) && !x.Contains('<') && !x.Contains('>'))
            .WithMessage("Tên danh mục không được chứa ký tự điều khiển hoặc thẻ HTML.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Đường dẫn ảnh không được vượt quá 500 ký tự.");

        RuleFor(x => x.OrderIndex)
            .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị phải lớn hơn hoặc bằng 0.");
    }
}
