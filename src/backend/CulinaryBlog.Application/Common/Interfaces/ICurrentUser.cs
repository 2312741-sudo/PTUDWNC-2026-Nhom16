namespace CulinaryBlog.Application.Common.Interfaces;

/// <summary>
/// Abstraction cho người dùng hiện tại (D18/D24: dùng UserId string ở lớp trong, không kéo HttpContext/Identity vào Application).
/// TV1 (A1/A3) cài đặt ở Presentation/Infrastructure. TV3 dùng để lấy AuthorId khi tạo/sửa recipe,
/// và để kiểm tra ownership ở handler (NFR-SEC-006).
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
