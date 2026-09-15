namespace CulinaryBlog.Domain.Common;

/// <summary>
/// Lớp cơ sở cho mọi entity nghiệp vụ (SRS 7.1 / D24). 5 cột kế thừa:
/// Id, CreatedAt (NOT NULL), UpdatedAt (NULL), IsDeleted (soft delete - D08), RowVersion (concurrency - D19).
/// Domain thuần BCL (D18): không tham chiếu EF Core / Identity / thư viện ngoài.
/// Giá trị CreatedAt/UpdatedAt/RowVersion do AuditableEntityInterceptor gán khi SaveChanges.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; }

    /// <summary>NULL cho tới lần cập nhật đầu tiên (SRS 7.1: timestamptz NULL).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft delete flag (D08). Global query filter: .Where(e =&gt; !e.IsDeleted).</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Token concurrency opaque (bytea). Interceptor gán giá trị mới khi Added/Modified.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
