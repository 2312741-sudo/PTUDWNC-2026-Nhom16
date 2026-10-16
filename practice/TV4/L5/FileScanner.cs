namespace CulinaryBlog.Practice.Lab5;

/// <summary>Quét tĩnh mã nguồn — phase này cần đọc code, không chỉ gọi HTTP.</summary>
public static class FileScanner
{
    private static readonly string[] Extensions = [".ts", ".tsx", ".js", ".jsx", ".json"];

    private static readonly string[] SkipDirs = ["node_modules", ".next", "out", "bin", "obj", ".git"];

    public static IReadOnlyList<string> Search(string root, params string[] needles)
    {
        var found = new List<string>();
        foreach (var file in Enumerate(root))
        {
            string text;
            try
            {
                text = File.ReadAllText(file);
            }
            catch
            {
                continue;
            }

            var lines = text.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (!needles.Any(n => lines[i].Contains(n, StringComparison.OrdinalIgnoreCase))) continue;
                var rel = Path.GetRelativePath(root, file).Replace('\\', '/');
                found.Add($"{rel}:{i + 1}: {lines[i].Trim()}");
            }
        }
        return found;
    }

    /// <summary>Đếm số file dùng một khái niệm — ví dụ <c>next/image</c> so với thẻ <c>&lt;img&gt;</c> thô.</summary>
    public static (int NextImage, int RawImg) CountImageUsage(string root)
    {
        var nextImage = 0;
        var rawImg = 0;
        foreach (var file in Enumerate(root))
        {
            if (!file.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase)) continue;
            string text;
            try
            {
                text = File.ReadAllText(file);
            }
            catch
            {
                continue;
            }
            if (text.Contains("from 'next/image'", StringComparison.Ordinal) ||
                text.Contains("from \"next/image\"", StringComparison.Ordinal)) nextImage++;
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"<img[\s/>]")) rawImg++;
        }
        return (nextImage, rawImg);
    }

    private static IEnumerable<string> Enumerate(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var dir = pending.Pop();
            string[] subdirs;
            string[] files;
            try
            {
                subdirs = Directory.GetDirectories(dir);
                files = Directory.GetFiles(dir);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                if (Extensions.Contains(Path.GetExtension(file))) yield return file;
            }
            foreach (var sub in subdirs)
            {
                if (!SkipDirs.Contains(Path.GetFileName(sub))) pending.Push(sub);
            }
        }
    }
}