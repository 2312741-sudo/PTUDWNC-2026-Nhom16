using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Entities;

/// <summary>
/// Bước thực hiện (D16). StepNumber do server quản lý, liên tục 1..N, unique theo (RecipeId, StepNumber).
/// Title &lt;=200 bắt buộc; Description 1..2000; TimerMinutes &gt;=0 nullable; ImageUrl &lt;=500 nullable.
/// Soft delete đồng nhất qua BaseEntity; renumber trong transaction (xử lý ở Recipe aggregate).
/// </summary>
public sealed class RecipeStep : BaseEntity
{
    public Guid RecipeId { get; private set; }
    public int StepNumber { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int? TimerMinutes { get; private set; }
    public string? ImageUrl { get; private set; }

    private RecipeStep() { }

    internal RecipeStep(Guid recipeId, int stepNumber, string title, string description, int? timerMinutes, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
            throw new DomainException("STEP_TITLE_INVALID", "Tiêu đề bước phải từ 1 đến 200 ký tự.");
        if (string.IsNullOrWhiteSpace(description) || description.Length > 2000)
            throw new DomainException("STEP_DESCRIPTION_INVALID", "Mô tả bước phải từ 1 đến 2000 ký tự.");
        if (timerMinutes is < 0)
            throw new DomainException("STEP_TIMER_INVALID", "Thời gian hẹn giờ không được âm.");
        if (imageUrl is { Length: > 500 })
            throw new DomainException("STEP_IMAGE_URL_INVALID", "URL ảnh bước tối đa 500 ký tự.");

        RecipeId = recipeId;
        StepNumber = stepNumber;
        Title = title.Trim();
        Description = description.Trim();
        TimerMinutes = timerMinutes;
        ImageUrl = imageUrl?.Trim();
    }

    internal void SetNumber(int stepNumber) => StepNumber = stepNumber;
}
