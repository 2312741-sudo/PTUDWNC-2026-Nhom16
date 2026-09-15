namespace CulinaryBlog.Domain.Enums;

/// <summary>
/// Độ khó. Đánh số THEO SRS 7.2: 1=Easy, 2=Medium, 3=Hard, 4=Expert (DEFAULT 1).
/// Đủ Expert (D15 - Expert bị thiếu trong filter gốc chương 3, bổ sung ở đây).
/// </summary>
public enum RecipeDifficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3,
    Expert = 4
}
