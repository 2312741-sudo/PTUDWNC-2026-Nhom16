using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Lab.TV3.Api.L3;

namespace Lab.TV3.Api.L16;

/// <summary>
/// LAB K16 (L5 SSR search) — server-side rendering: server chạy truy vấn FTS rồi trả về HTML hoàn chỉnh,
/// trình duyệt/crawler đọc được kết quả ngay cả khi tắt JavaScript. Dùng lại câu SQL của L3 (chỉ Published).
/// </summary>
public static class SsrSearchEndpoints
{
    // Giữ nguyên chữ tiếng Việt (UnicodeRanges.All) nhưng vẫn encode < > & " ' để chống XSS
    private static readonly HtmlEncoder Html = HtmlEncoder.Create(UnicodeRanges.All);

    public static void MapL16SsrSearch(this WebApplication app)
    {
        app.MapGet("/lab/l16/search", async (string? q, int? page, int? pageSize, LabDb db, CancellationToken ct) =>
        {
            var query = q?.Trim() ?? "";
            if (query.Length > 200) query = query[..200];
            var p = Math.Max(1, page ?? 1);
            var size = Math.Clamp(pageSize ?? 10, 1, 50);

            SearchPage? result = query.Length == 0 ? null : await SearchEndpoints.SearchDb(db, query, null, p, size, ct);
            return Results.Content(Render(query, result), "text/html; charset=utf-8");
        });
    }

    public static string Render(string q, SearchPage? result)
    {
        var e = Html.Encode(q);
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"vi\"><head><meta charset=\"utf-8\">")
          .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
          // Trang kết quả tìm kiếm không nên vào index (nội dung trùng/vô hạn), nhưng vẫn cho crawler đi theo link
          .Append("<meta name=\"robots\" content=\"noindex,follow\">")
          .Append("<title>").Append(q.Length > 0 ? $"Tìm “{e}” – " : "").Append("Culinary Lab</title></head><body><main>")
          .Append("<h1>Tìm công thức</h1>")
          .Append("<form method=\"get\" action=\"/lab/l16/search\" role=\"search\">")
          .Append("<label for=\"q\">Từ khoá</label> <input id=\"q\" name=\"q\" type=\"search\" value=\"").Append(e).Append("\">")
          .Append(" <button type=\"submit\">Tìm</button></form>");

        if (result is not null)
        {
            if (result.TotalCount == 0)
            {
                sb.Append("<p>Không tìm thấy công thức nào cho “").Append(e).Append("”.</p>");
            }
            else
            {
                sb.Append("<p>").Append(result.TotalCount).Append(" kết quả</p><ol class=\"hits\">");
                foreach (var h in result.Items)
                {
                    sb.Append("<li class=\"hit\"><a href=\"/lab/l19/recipes/").Append(Html.Encode(h.Slug)).Append("\">")
                      .Append(Html.Encode(h.Title)).Append("</a>");
                    if (h.Category is not null) sb.Append(" <small>").Append(Html.Encode(h.Category)).Append("</small>");
                    sb.Append("</li>");
                }
                sb.Append("</ol>");
                AppendPager(sb, q, result);
            }
        }

        return sb.Append("</main></body></html>").ToString();
    }

    private static void AppendPager(StringBuilder sb, string q, SearchPage r)
    {
        var pages = (int)Math.Ceiling(r.TotalCount / (double)r.PageSize);
        if (pages <= 1) return;
        sb.Append("<nav aria-label=\"Phân trang\">");
        if (r.Page > 1) sb.Append(Link(q, r.Page - 1, r.PageSize, "« Trước"));
        sb.Append(" Trang ").Append(r.Page).Append('/').Append(pages).Append(' ');
        if (r.Page < pages) sb.Append(Link(q, r.Page + 1, r.PageSize, "Sau »"));
        sb.Append("</nav>");
    }

    private static string Link(string q, int page, int size, string text) =>
        $"<a href=\"{Html.Encode($"/lab/l16/search?q={Uri.EscapeDataString(q)}&page={page}&pageSize={size}")}\">{text}</a>";
}
