using Microsoft.Extensions.Options;

namespace CulinaryBlog.Infrastructure;

public sealed class MinioOptions
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string Bucket { get; set; } = "culinary-blog";
    public bool UseSsl { get; set; }

    /// <summary>
    /// B2 (issue #21, N1-7): fail-fast lúc khởi động thay vì để lỗi lộ ra lúc runtime.
    /// Trước đây AccessKey/SecretKey rỗng vẫn khởi động được và báo /health = Healthy, chỉ khi
    /// người dùng bấm "Tải lên" mới nhận 500 server.error — phải đọc log Serilog mới hiểu vì sao.
    /// Validate chỉ bắt chuỗi RỖNG: credential sai (có giá trị) vẫn khởi động được, lỗi đó thuộc #20 (503).
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(Endpoint))
            problems.Add("Minio:Endpoint (biến môi trường Minio__Endpoint) phải có giá trị, ví dụ 'localhost:9000'.");
        if (string.IsNullOrWhiteSpace(AccessKey))
            problems.Add("Minio:AccessKey (biến môi trường Minio__AccessKey) phải có giá trị — nếu rỗng thì upload ảnh sẽ trả lỗi 503 storage.unavailable.");
        if (string.IsNullOrWhiteSpace(SecretKey))
            problems.Add("Minio:SecretKey (biến môi trường Minio__SecretKey) phải có giá trị — nếu rỗng thì upload ảnh sẽ trả lỗi 503 storage.unavailable.");
        if (string.IsNullOrWhiteSpace(Bucket))
            problems.Add("Minio:Bucket (biến môi trường Minio__Bucket) phải có giá trị, ví dụ 'culinary-blog'.");

        return problems;
    }
}

/// <summary>
/// B2 (issue #21): validator cho <see cref="MinioOptions"/>, đăng ký kèm ValidateOnStart() ở Program.cs
/// (bỏ qua môi trường Testing vì test host không nạp cấu hình storage thật).
/// Message gộp TẤT CẢ vấn đề lại để lúc khởi động thất bại người vận hành thấy ngay mình thiếu biến nào.
/// </summary>
public sealed class MinioOptionsValidator : IValidateOptions<MinioOptions>
{
    public ValidateOptionsResult Validate(string? name, MinioOptions options)
    {
        var problems = options.Validate();
        return problems.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                "Cấu hình object storage (Minio) không hợp lệ — sửa .env hoặc appsettings, "
                + "xem docs/evidence/TV4/Tuan04/HANDOFF_TV4_TUAN4.md. " + string.Join(" ", problems));
    }
}
