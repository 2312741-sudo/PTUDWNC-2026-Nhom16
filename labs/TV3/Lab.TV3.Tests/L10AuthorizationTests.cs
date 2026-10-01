using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using Xunit;
using static Lab.TV3.Tests.LabHttp;

namespace Lab.TV3.Tests;

/// <summary>
/// LAB L10 — K10: policy Admin/VerifiedAuthor, kiểm quyền chủ sở hữu (resource-based), rate limit 429.
/// JWT thật do app lab phát hành, DB thật lab_tv3_test; vai trò được đổi trực tiếp trong DB rồi đăng nhập lại.
/// </summary>
[Collection("lab")]
public sealed class L10AuthorizationTests(LabFactory f)
{
    /// <summary>Đặt role/verified trong DB rồi login lại để JWT mới mang claim tương ứng.</summary>
    private async Task<HttpClient> As(string role, bool verified = false)
    {
        var (client, _, email) = await AuthorAsync(f);
        if (role == "Author" && !verified) return client;

        await Sql("UPDATE lab_users SET role = @role, verified_author = @verified WHERE email = @email",
            ("role", role), ("verified", verified), ("email", email));

        var res = await client.PostAsJsonAsync("/lab/l1/login", new { email, password = Password });
        await Expect(HttpStatusCode.OK, res);
        var token = (await Data(res)).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Chạy SQL thẳng vào DB test lab (không qua API) để chuẩn bị/kiểm dữ liệu.</summary>
    private static async Task<object?> Sql(string sql, params (string Name, object Value)[] args)
    {
        await using var c = new NpgsqlConnection($"{LabFactory.Pg};Database=lab_tv3_test");
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, c);
        foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value);
        return await cmd.ExecuteScalarAsync();
    }

    private static async Task<JsonElement> NewPost(HttpClient c, string title = "Phở bò Nam Định")
    {
        var res = await c.PostAsJsonAsync("/lab/l10/posts", new { title });
        await Expect(HttpStatusCode.Created, res);
        return await Data(res);
    }

    private static string Id(JsonElement post) => post.GetProperty("id").GetString()!;

    [Fact]
    public async Task Guest_khong_co_token_bi_401()
    {
        var guest = f.CreateClient();
        var author = await As("Author");
        var post = await NewPost(author);

        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsJsonAsync("/lab/l10/posts", new { title = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PutAsJsonAsync($"/lab/l10/posts/{Id(post)}", new { title = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsync($"/lab/l10/posts/{Id(post)}/publish", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync("/lab/l10/admin/stats")).StatusCode);
    }

    [Fact]
    public async Task Policy_Admin_chi_cho_role_Admin()
    {
        var author = await As("Author");
        var verified = await As("Author", verified: true);
        var admin = await As("Admin");

        Assert.Equal(HttpStatusCode.Forbidden, (await author.GetAsync("/lab/l10/admin/stats")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await verified.GetAsync("/lab/l10/admin/stats")).StatusCode);

        var res = await admin.GetAsync("/lab/l10/admin/stats");
        await Expect(HttpStatusCode.OK, res);
        Assert.True((await Data(res)).GetProperty("users").GetInt64() >= 3);
    }

    [Fact]
    public async Task Owner_sua_duoc_nonOwner_403_Admin_sua_duoc_bai_nguoi_khac()
    {
        var owner = await As("Author");
        var other = await As("Author");
        var admin = await As("Admin");
        var post = await NewPost(owner);

        var mine = await owner.PutAsJsonAsync($"/lab/l10/posts/{Id(post)}", new { title = "Phở bò (chủ sửa)" });
        await Expect(HttpStatusCode.OK, mine);
        Assert.Equal("Phở bò (chủ sửa)", (await Data(mine)).GetProperty("title").GetString());

        var stranger = await other.PutAsJsonAsync($"/lab/l10/posts/{Id(post)}", new { title = "chiếm bài" });
        Assert.Equal(HttpStatusCode.Forbidden, stranger.StatusCode);

        // Request bị 403 không được ghi DB
        Assert.Equal("Phở bò (chủ sửa)", await Sql("SELECT title FROM lab_posts WHERE id = @id", ("id", Guid.Parse(Id(post)))));

        var byAdmin = await admin.PutAsJsonAsync($"/lab/l10/posts/{Id(post)}", new { title = "Phở bò (admin sửa)" });
        await Expect(HttpStatusCode.OK, byAdmin);
        Assert.Equal("Phở bò (admin sửa)", (await Data(byAdmin)).GetProperty("title").GetString());

        var missing = await owner.PutAsJsonAsync($"/lab/l10/posts/{Guid.NewGuid()}", new { title = "x" });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Publish_can_VerifiedAuthor_va_phai_la_chu_bai()
    {
        var plain = await As("Author");
        var verified = await As("Author", verified: true);
        var otherVerified = await As("Author", verified: true);

        var plainPost = await NewPost(plain);
        Assert.Equal(HttpStatusCode.Forbidden, (await plain.PostAsync($"/lab/l10/posts/{Id(plainPost)}/publish", null)).StatusCode);

        var post = await NewPost(verified);
        Assert.Equal("Draft", post.GetProperty("status").GetString());

        // VerifiedAuthor nhưng không phải chủ -> vẫn 403 (policy + resource đều phải qua)
        Assert.Equal(HttpStatusCode.Forbidden, (await otherVerified.PostAsync($"/lab/l10/posts/{Id(post)}/publish", null)).StatusCode);

        var ok = await verified.PostAsync($"/lab/l10/posts/{Id(post)}/publish", null);
        await Expect(HttpStatusCode.OK, ok);
        Assert.Equal("Published", (await Data(ok)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Admin_qua_policy_VerifiedAuthor_va_publish_bai_nguoi_khac()
    {
        var verified = await As("Author", verified: true);
        var admin = await As("Admin");
        var post = await NewPost(verified);

        var ok = await admin.PostAsync($"/lab/l10/posts/{Id(post)}/publish", null);
        await Expect(HttpStatusCode.OK, ok);
    }

    [Fact]
    public async Task Vuot_gioi_han_binh_luan_tra_429_kem_RetryAfter_va_khong_anh_huong_user_khac()
    {
        var spammer = await As("Author");
        var other = await As("Author");
        var post = await NewPost(spammer);
        var url = $"/lab/l10/posts/{Id(post)}/comments";

        for (var i = 1; i <= 5; i++)
            await Expect(HttpStatusCode.Created, await spammer.PostAsJsonAsync(url, new { body = $"Bình luận {i}" }));

        var limited = await spammer.PostAsJsonAsync(url, new { body = "Bình luận 6" });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.RetryAfter?.Delta > TimeSpan.Zero, "Thiếu header Retry-After");
        Assert.Equal("RATE_LIMITED", await Code(limited));

        // Phân vùng theo user: người khác vẫn bình luận được
        await Expect(HttpStatusCode.Created, await other.PostAsJsonAsync(url, new { body = "Tôi chưa bị chặn" }));

        // Request bị chặn không được ghi vào DB
        Assert.Equal(6L, await Sql("SELECT count(*) FROM lab_comments WHERE post_id = @id", ("id", Guid.Parse(Id(post)))));
    }
}
