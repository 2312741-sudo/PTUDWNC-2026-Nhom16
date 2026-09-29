# Đề xuất gỡ block B4 — Health check object storage phải **xác thực**, không chỉ TCP probe

> **Block**: [`TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md`](../evidence/TV4/Tuan03/Report/TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md) §2 B4
> **Mức**: 🟡 Trung bình · **Nguồn**: §3.3 và §6 mục 3 của [`BAO_CAO_LOI_UPLOAD_ANH_500.md`](../report/BAO_CAO_LOI_UPLOAD_ANH_500.md)
> **Cần ai quyết**: nhóm (chốt ngữ nghĩa `/health`)

---

## 1. Thực trạng

`MinIOHealthCheck` (`src/backend/CulinaryBlog.API/Health.cs:36-43`) chỉ mở TCP tới `localhost:9000`:

```csharp
public Task<HealthCheckResult> CheckHealthAsync(...) => TcpHealthCheckHelper.CheckTcpAsync(_host, _port, ct);
```

Đo thật (28/09/2026) khi credential **rỗng** nhưng container RustFS vẫn chạy:

```json
{"status":"Healthy","checks":{...,"minio":{"status":"Healthy","description":"localhost:9000 reachable."}}}
```

⇒ `/health` báo `Healthy` cho cả tình huống mà `POST /recipes/{id}/images` trả 500. Người debug
dễ tin `health` và loại trừ nhầm nguyên nhân (báo cáo gốc mất nhiều thời gian chính vì vậy).

Lưu ý: check này gắn tag `["all"]` **không** có tag `ready` (`Program.cs:148`) ⇒
`/health/ready` không gồm storage. Đây là điểm **đúng** (khoá đã đúng), nhưng dễ gây hiểu nhầm
rằng `/health` (all) cũng "nên" phản ánh sẵn sàng thật.

## 2. Nguyên nhân

1. Health check được viết theo mô hình "hạ tầng có còn sống không" (TCP) thay vì "dùng được không"
   (xác thực + đọc/ghi được).
2. Check nhận `IConfiguration` + đọc `HealthChecks:Minio:Host/Port`, **không** nhận
   `IObjectStorageReader` ⇒ không thực hiện được lệnh S3 nào.
3. Cần giữ ranh giới: check có auth phải dùng credential mà **app** dùng, không dùng bản riêng.

## 3. Giải pháp đề xuất

### Phương án A (khuyến nghị) — thêm check mới `storage_authenticated`, giữ nguyên `minio` TCP

```csharp
// Health.cs — check mới, dùng đúng credential app đang chạy
public sealed class ObjectStorageHealthCheck(IObjectStorageReader storage, IConfiguration cfg) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        try
        {
            // Đọc 1 key không tồn tại: xác thực + xác nhận bucket tồn tại, không ghi rác lên storage.
            var content = await storage.ReadAsync("recipes/healthcheck-probe/00000000-0000-0000-0000-000000000000.png", ct);
            return content is null
                ? HealthCheckResult.Healthy("Object storage xac thuc duoc (bucket reachable, key khong ton tai).")
                : HealthCheckResult.Healthy("Object storage xac thuc duoc.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Object storage khong xac thuc duoc (credential/sai endpoint/ban?).", ex);
        }
    }
}
```

```csharp
// Program.cs:148 — thêm vào, KHÔNG gắn tag "ready"
.AddCheck<ObjectStorageHealthCheck>("storage_authenticated", tags: ["all"]);
```

Ưu điểm: `/health` phản ánh đúng "dùng được", `/health/ready` giữ nguyên ngữ nghĩa (app sẵn sàng
nhận traffic **có** phụ thuộc storage hay không là câu hỏi khác — xem §4).

### Phương án B — thay thế luôn check TCP hiện tại

Ít check hơn nhưng mất khả năng phân biệt "container chết" với "credential sai" trong log.

### Phương án C — không đụng code, chỉ ghi rõ trong tài liệu

Rẻ nhất, nhưng `health` vẫn nói dối ⇒ nguyên nhân báo cáo gốc vẫn tốn thời gian để tìm.

## 4. Rủi ro

| Rủi ro | Khả năng | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| Gắn nhầm tag `ready` ⇒ app bị báo `Unhealthy` khi storage chậm, kéo theo việc restart/traffic | Trung bình | Sự cố dịch vụ | **Không** gắn `ready`; nếu muốn gating thì tách cấu hình riêng |
| Check gọi storage mỗi lần `/health` bị poll ⇒ tăng tải / tạo độ trễ khi storage chậm | Trung bình | `/health` chậm | Timeout 2–3 s (`CancellationTokenSource`) + cache kết quả 30–60 s |
| Storage chậm chập chờn ⇒ `/health` nhấp nháy `Unhealthy`/`Healthy` | Trung bình | Cảnh báo sai | Bỏ qua lỗi mạng tạm thời (retry 1 lần) hoặc chấp nhận, chỉ dùng để debug |
| Check chạy khi app khởi động mà storage chưa sẵn sàng ⇒ log đỏ | Thấp | Nhiễu log | Check chỉ chạy khi có request tới `/health` |
| Test nào đó assert đúng shape của `/health` sẽ đổi | Thấp | Test đỏ | Grep `health` trong `HealthTests.cs` trước khi sửa |

## 5. Test case

| # | Test case | Kỳ vọng |
|---|---|---|
| 1 | `/health` khi storage OK | `storage_authenticated=Healthy` |
| 2 | `/health` với `Minio__AccessKey=wrong` | `storage_authenticated=Unhealthy` **và** log nêu rõ credential ⇒ phát hiện được ngay, không cần đọc code |
| 3 | `/health` khi dừng container `s3` | `storage_authenticated=Unhealthy`; `minio` (TCP) cũng `Unhealthy` |
| 4 | `/health/ready` khi storage down | **Không** đổi (không gắn tag `ready`) |
| 5 | `/health/live` | `Healthy` (chỉ liveness) |
| 6 | Storage chậm ~5 s | `/health` trả trong ≤ 5 s (không treo) |
| 7 | `HealthTests.cs` cũ | Không đỏ (hoặc cập nhật kỳ vọng) |
| 8 | Song song 20 request `/health` | Không tạo object rác trong bucket (probe dùng key không tồn tại, chỉ `StatObject`) |

## 6. Chuỗi lỗi liên quan (nếu có)

```
Nguoi dung: /health = Healthy -> "storage OK, van la do khac"
  └─ MinIOHealthCheck -> TcpHealthCheckHelper.ConnectAsync(localhost:9000) -> Healthy
      └─ nhung thuc te: AccessKey = "" -> moi thao tac S3 moi 401
          └─ POST /recipes/{id}/images -> 500 server.error
              └─ nguoi dung moi quay lai doc code (mat thoi gian)
```

Sau khi áp dụng phương án A:

```
Nguoi dung: /health -> storage_authenticated=Unhealthy ("credential/sai endpoint?")
  └─ biet ngay nguyen nhan truoc khi bam nut -> khong con vong "tai sao 500?"
```

## 7. Quyết định cần chốt

1. Có thêm check xác thực (phương án A) hay chỉ sửa tài liệu (C)?
2. `/health/ready` có được gắn thêm kiểm tra storage không (tức coi storage là hạ tầng bắt buộc để nhận traffic)?
3. Có chấp nhận `/health` gọi storage (có I/O) không, hay giữ nguyên vai trò "TCP-only" cho tiêu chuẩn nhẹ?
