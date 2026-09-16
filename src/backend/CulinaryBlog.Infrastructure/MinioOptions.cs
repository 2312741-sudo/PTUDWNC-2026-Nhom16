namespace CulinaryBlog.Infrastructure;

public sealed class MinioOptions
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string Bucket { get; set; } = "culinary-blog";
    public bool UseSsl { get; set; }
}
