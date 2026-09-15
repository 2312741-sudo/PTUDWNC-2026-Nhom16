using Microsoft.AspNetCore.Identity;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>
/// Người dùng ứng dụng (SRS 7.7). Kế thừa IdentityUser&lt;string&gt; → bảng "AspNetUsers".
/// Đặt ở Infrastructure (D18): Domain không tham chiếu IdentityUser; lớp trong chỉ dùng UserId (string).
///
/// LƯU Ý VAI TRÒ: bản tối thiểu (custom columns theo SRS) để C1 chạy độc lập và FK Recipe.AuthorId là thật.
/// TV1 (task A1) sở hữu Identity đầy đủ: roles/policy/PBKDF2/JWT/seed. Khi tích hợp, TV1 mở rộng chính class này.
/// </summary>
public class RecipeAuthorUser : IdentityUser<string>
{
    public string DisplayName { get; set; } = string.Empty;   // <=100, NOT NULL
    public string? AvatarUrl { get; set; }                    // <=500, nullable
    public string? Bio { get; set; }                          // nullable
    public bool IsActive { get; set; } = true;                // NOT NULL default true
    public DateTime CreatedAt { get; set; }                   // NOT NULL default NOW()
}
