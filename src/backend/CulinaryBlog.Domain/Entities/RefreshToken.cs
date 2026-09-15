namespace CulinaryBlog.Domain.Entities;

/// <summary>
/// Refresh token (SRS 7.8). KHÔNG kế thừa BaseEntity — cấu trúc riêng (D24): không có IsDeleted/UpdatedAt/RowVersion.
/// TokenHash là SHA-256 của raw token (không lưu raw). Theo dõi family qua ReplacedByTokenHash.
///
/// LƯU Ý VAI TRÒ: bản tối thiểu để FK RefreshToken→AspNetUsers (CASCADE) là thật và để C5 (rotation) có chỗ dựa.
/// Logic sinh/rotation/reuse thuộc C5 (TV3) và phối hợp auth contract với TV1.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UserId { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;   // SHA-256, 64 hex
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? CreatedByIp { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Issue(string userId, string tokenHash, DateTime expiresAt, DateTime createdAtUtc, string? createdByIp)
        => new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = createdAtUtc,
            CreatedByIp = createdByIp
        };

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

    public void Revoke(DateTime whenUtc, string? replacedByTokenHash = null)
    {
        RevokedAt = whenUtc;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
