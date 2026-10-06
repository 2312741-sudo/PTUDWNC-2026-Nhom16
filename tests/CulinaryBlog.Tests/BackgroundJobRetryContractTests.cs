using System.Reflection;
using CulinaryBlog.Infrastructure;
using Hangfire;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// N2-C1c — khoá số lần retry của các job nền bằng HỢP ĐỒNG, không chỉ bằng lời kê.
///
/// Vì sao phải test: yêu cầu là "sitemap retry 2 lần · ảnh resize/delete retry 3 lần ·
/// welcome retry 3 lần (lịch 1/5/30 phút)". Trước khi có test này, <c>SitemapGenerationJob</c>
/// không có <c>[AutomaticRetry]</c> — Hangfire khi đó **không thử lại lần nào**, nên một lần DB
/// chậm lúc 02:00 UTC là mất sitemap cả ngày mà không để lại dấu vết nào. Đây là loại hợp đồng
/// ngầm dễ mất nhất: sửa nhầm một dòng attribute là đổi hành vi production mà test vẫn xanh.
///
/// Đọc cấu hình bằng reflection thay vì chạy job thật: đây là hợp đồng cấu hình, còn hành vi runtime
/// đã được chứng minh ở outage drill (logs/c1_outage_drill.log) và Hangfire dashboard.
/// </summary>
public sealed class BackgroundJobRetryContractTests
{
    /// <summary>
    /// Hangfire gộp attribute ở cấp class lẫn cấp method khi quyết định số lần retry, và nơi đặt
    /// không nhất quát giữa các job (resize đặt trên method, sitemap đặt trên class). Test vì vậy
    /// phải dò cả hai — chỉ dò class sẽ báo xanh nhầm cho resize và báo đỏ oan cho sitemap.
    /// </summary>
    private static int AttemptsOn<TJob>(string methodName)
    {
        var fromClass = typeof(TJob).GetCustomAttribute<AutomaticRetryAttribute>();
        var fromMethod = typeof(TJob).GetMethod(methodName)?.GetCustomAttribute<AutomaticRetryAttribute>();
        var attribute = fromClass ?? fromMethod;

        Assert.True(fromClass is not null || fromMethod is not null,
            $"{typeof(TJob).Name} phải có [AutomaticRetry] để job hạ tầng thất bại được thử lại.");
        return attribute!.Attempts;
    }

    [Fact]
    public void Sitemap_retry_2_lan()
    {
        Assert.Equal(2, AttemptsOn<SitemapGenerationJob>(nameof(SitemapGenerationJob.RunAsync)));
    }

    [Fact]
    public void Resize_anh_retry_3_lan()
    {
        Assert.Equal(3, AttemptsOn<ResizeImageJob>(nameof(ResizeImageJob.ExecuteAsync)));
    }

    /// <summary>
    /// Xoá ảnh KHÔNG đi qua Hangfire — nó chạy trong transaction của request
    /// (xem DeleteRecipeImageHandler, N2-E4 về unique index primary). Nên "retry 3 lần" của delete
    /// được cài ở tầng storage: <c>MinioStorageService.DeleteAsync</c> lặp lại trong thân hàm.
    /// </summary>
    [Fact]
    public void Xoa_anh_retry_3_lan()
    {
        Assert.Equal(3, MinioStorageService.DeleteAttempts);
    }

    [Fact]
    public void Welcome_email_retry_3_lan_theo_lich_1_5_30_phut()
    {
        var delays = WelcomeEmailWorker.RetryDelays;

        Assert.Equal(TimeSpan.Zero, delays[0]);
        Assert.Equal(TimeSpan.FromMinutes(1), delays[1]);
        Assert.Equal(TimeSpan.FromMinutes(5), delays[2]);
        Assert.Equal(TimeSpan.FromMinutes(30), delays[3]);

        // 4 lần thử = 1 lần gửi đầu + 3 lần retry.
        Assert.Equal(4, delays.Length);
    }
}
