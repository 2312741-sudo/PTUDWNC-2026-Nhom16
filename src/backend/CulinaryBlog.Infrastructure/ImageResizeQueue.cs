using CulinaryBlog.Application;
using Hangfire;

namespace CulinaryBlog.Infrastructure;

/// <summary>
/// D23 PA-1: đẩy job resize vào Hangfire (queue PostgreSQL, retry 3 theo [AutomaticRetry] trên ResizeImageJob).
/// Job chạy ngoài request → upload ảnh không bị chặn bởi CPU encode.
/// </summary>
public sealed class HangfireImageResizeQueue : IImageResizeQueue
{
    public Task EnqueueAsync(Guid recipeId, Guid imageId, string originalKey, CancellationToken ct)
    {
        BackgroundJob.Enqueue<ResizeImageJob>(job =>
            job.ExecuteAsync(recipeId, imageId, originalKey));
        return Task.CompletedTask;
    }
}

/// <summary>
/// Môi trường Testing/E2E không bật Hangfire server (tránh worker nền + race khi assert DB):
/// chạy job inline ngay sau upload để test deterministic.
/// </summary>
public sealed class InlineImageResizeQueue(ResizeImageJob job) : IImageResizeQueue
{
    public Task EnqueueAsync(Guid recipeId, Guid imageId, string originalKey, CancellationToken ct) =>
        job.ExecuteAsync(recipeId, imageId, originalKey, ct);
}
