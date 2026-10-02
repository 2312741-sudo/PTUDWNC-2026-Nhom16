using Hangfire.Dashboard;

namespace CulinaryBlog.API;

/// <summary>
/// D23: dashboard Hangfire chỉ dành cho Admin (không phơi retry/queue cho người dùng thường).
/// Vị trí sau UseAuthentication/UseAuthorization nên HttpContext.User đã có claim role.
/// </summary>
public sealed class AdminDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) =>
        context.GetHttpContext().User.IsInRole(CulinaryBlog.Domain.Roles.Admin);
}
