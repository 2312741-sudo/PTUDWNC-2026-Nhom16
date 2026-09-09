namespace CulinaryBlog.Domain;

public sealed record DisplayName
{
    public string Value { get; }
    public DisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 100 || value.Any(char.IsControl) || value.Contains('<') || value.Contains('>'))
            throw new ArgumentException("Tên hiển thị phải có 1–100 ký tự văn bản.", nameof(value));
        Value = value.Trim();
    }
}
public static class Roles
{
    public const string Author = "Author";
    public const string Admin = "Admin";
}
