using System.Text.RegularExpressions;
using Npgsql;

namespace Lab.TV3.Api;

/// <summary>Kết nối + schema riêng của lab (prefix lab_), tách hẳn DB sản phẩm.</summary>
public sealed partial class LabDb(NpgsqlDataSource source)
{
    public NpgsqlDataSource Source { get; } = source;

    public ValueTask<NpgsqlConnection> OpenAsync(CancellationToken ct = default) => Source.OpenConnectionAsync(ct);

    /// <summary>Tạo database lab (lab_tv3 / lab_tv3_test) nếu chưa có.</summary>
    public static async Task EnsureDatabaseAsync(string connectionString)
    {
        var b = new NpgsqlConnectionStringBuilder(connectionString);
        var name = b.Database ?? throw new InvalidOperationException("Connection string thiếu Database");
        if (!SafeName().IsMatch(name)) throw new InvalidOperationException($"Tên database không hợp lệ: {name}");
        b.Database = "postgres";
        await using var c = new NpgsqlConnection(b.ConnectionString);
        await c.OpenAsync();
        await using var check = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @n", c);
        check.Parameters.AddWithValue("n", name);
        if (await check.ExecuteScalarAsync() is not null) return;
        try
        {
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", c);
            await create.ExecuteNonQueryAsync();
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.DuplicateDatabase) { }
    }

    public async Task EnsureSchemaAsync()
    {
        await using var c = await OpenAsync();
        // Advisory lock: nhiều instance/test factory khởi động cùng lúc không đạp nhau khi tạo extension/trigger
        await using (var lk = new NpgsqlCommand("SELECT pg_advisory_lock(733100)", c)) await lk.ExecuteNonQueryAsync();
        try
        {
            await using var cmd = new NpgsqlCommand(Schema, c);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            await using var ul = new NpgsqlCommand("SELECT pg_advisory_unlock(733100)", c);
            await ul.ExecuteNonQueryAsync();
        }
    }

    [GeneratedRegex("^[a-z0-9_]+$")]
    private static partial Regex SafeName();

    private const string Schema = """
        CREATE EXTENSION IF NOT EXISTS unaccent;

        -- ===== L1
        CREATE TABLE IF NOT EXISTS lab_users (
            id uuid PRIMARY KEY,
            email text NOT NULL,
            normalized_email text NOT NULL UNIQUE,
            password_hash text NULL,              -- NULL = tài khoản chỉ đăng nhập Google
            display_name text NOT NULL,
            role text NOT NULL DEFAULT 'Author',
            google_sub text NULL UNIQUE,
            email_confirmed boolean NOT NULL DEFAULT false,
            created_at timestamptz NOT NULL DEFAULT now());

        CREATE TABLE IF NOT EXISTS lab_refresh_tokens (
            id uuid PRIMARY KEY,
            user_id uuid NOT NULL REFERENCES lab_users(id) ON DELETE CASCADE,
            token_hash text NOT NULL UNIQUE,      -- chỉ lưu SHA-256, không lưu token thô
            expires_at timestamptz NOT NULL,
            revoked_at timestamptz NULL,
            created_at timestamptz NOT NULL DEFAULT now());

        -- ===== L3: FTS
        CREATE TABLE IF NOT EXISTS lab_recipes (
            id uuid PRIMARY KEY,
            slug text NOT NULL UNIQUE,
            title text NOT NULL,
            description text NULL,
            ingredients text NULL,
            category text NULL,
            status text NOT NULL,
            created_at timestamptz NOT NULL DEFAULT now(),
            search_vector tsvector NULL);

        -- unaccent() không IMMUTABLE -> bọc lại để dùng trong trigger/index
        CREATE OR REPLACE FUNCTION lab_unaccent(text) RETURNS text
            LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT
            AS $$ SELECT public.unaccent('public.unaccent'::regdictionary, $1) $$;

        -- Trọng số: tiêu đề A > nguyên liệu B > mô tả C
        CREATE OR REPLACE FUNCTION lab_recipes_tsv() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            NEW.search_vector :=
                setweight(to_tsvector('simple', lab_unaccent(coalesce(NEW.title, ''))), 'A') ||
                setweight(to_tsvector('simple', lab_unaccent(coalesce(NEW.ingredients, ''))), 'B') ||
                setweight(to_tsvector('simple', lab_unaccent(coalesce(NEW.description, ''))), 'C');
            RETURN NEW;
        END $$;

        DROP TRIGGER IF EXISTS trg_lab_recipes_tsv ON lab_recipes;
        CREATE TRIGGER trg_lab_recipes_tsv
            BEFORE INSERT OR UPDATE OF title, description, ingredients ON lab_recipes
            FOR EACH ROW EXECUTE FUNCTION lab_recipes_tsv();

        CREATE INDEX IF NOT EXISTS ix_lab_recipes_search ON lab_recipes USING GIN (search_vector);
        -- Không tạo btree (status, created_at): planner sẽ né GIN khi bảng nhỏ -> EXPLAIN không chứng minh được FTS
        DROP INDEX IF EXISTS ix_lab_recipes_status;

        -- ===== L4
        CREATE TABLE IF NOT EXISTS lab_images (
            id uuid PRIMARY KEY,
            recipe_id uuid NOT NULL,
            object_key text NOT NULL,
            content_type text NOT NULL,
            size_bytes bigint NOT NULL,
            medium_key text NULL,
            thumb_key text NULL,
            created_at timestamptz NOT NULL DEFAULT now());

        CREATE TABLE IF NOT EXISTS lab_sitemaps (
            id bigserial PRIMARY KEY,
            generated_at timestamptz NOT NULL DEFAULT now(),
            url_count int NOT NULL,
            xml text NOT NULL);

        -- ===== L10: phân quyền (K10)
        ALTER TABLE lab_users ADD COLUMN IF NOT EXISTS verified_author boolean NOT NULL DEFAULT false;

        CREATE TABLE IF NOT EXISTS lab_posts (
            id uuid PRIMARY KEY,
            owner_id uuid NOT NULL REFERENCES lab_users(id) ON DELETE CASCADE,
            title text NOT NULL,
            status text NOT NULL DEFAULT 'Draft',
            updated_at timestamptz NOT NULL DEFAULT now());

        CREATE TABLE IF NOT EXISTS lab_comments (
            id uuid PRIMARY KEY,
            post_id uuid NOT NULL REFERENCES lab_posts(id) ON DELETE CASCADE,
            user_id uuid NOT NULL REFERENCES lab_users(id) ON DELETE CASCADE,
            body text NOT NULL,
            created_at timestamptz NOT NULL DEFAULT now());

        -- ===== L19: slug cũ -> recipe (301). Lưu recipe_id thay vì slug đích để luôn ra slug hiện tại.
        CREATE TABLE IF NOT EXISTS lab_slug_redirects (
            old_slug text PRIMARY KEY,
            recipe_id uuid NOT NULL REFERENCES lab_recipes(id) ON DELETE CASCADE,
            created_at timestamptz NOT NULL DEFAULT now());
        """;
}

public static class Http
{
    /// <summary>RFC 7807 Problem Details kèm mã lỗi máy đọc được.</summary>
    public static IResult Err(int status, string code, string message) =>
        Results.Problem(statusCode: status, title: message,
            extensions: new Dictionary<string, object?> { ["code"] = code });
}
