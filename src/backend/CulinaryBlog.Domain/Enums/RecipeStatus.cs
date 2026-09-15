namespace CulinaryBlog.Domain.Enums;

/// <summary>Vòng đời công thức. Xóa xử lý bằng soft delete (IsDeleted), không đưa vào enum này (D08).</summary>
public enum RecipeStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}
