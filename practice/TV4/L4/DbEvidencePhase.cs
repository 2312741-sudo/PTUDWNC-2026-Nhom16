using Microsoft.Extensions.Logging;
using Npgsql;

namespace CulinaryBlog.Practice.Lab4;

/// <summary>
/// Phase DB (diagnostic, không tính vào 4 phase yêu cầu của L4): dump trạng thái bảng Hangfire trong
/// database lab <c>culinary_lab</c> để đối chiếu bằng chứng sau khi chạy phase <c>jobs</c>.
/// Chạy: <c>dotnet run --project practice/TV4/L4 -- db</c>. Ghi ra <c>out/lab_hangfire_db.txt</c>.
/// </summary>
public static class DbEvidencePhase
{
    public const string OutputFileName = "lab_hangfire_db.txt";

    public static async Task<int> RunAsync(ILogger logger)
    {
        var lines = new List<string>
        {
            "LAB L4 (TV4) — bang chung truy van truc tiep database lab culinary_lab",
            $"Run id: {LabConfig.RunId} · Ghi luc: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            "Nguon: Ngsql (Npgsql) trong chinh process .NET 10 cua lab — khong dung psql.",
            "Luu y: KHONG ghi user/mat khau/connection string vao file nay (lay tu bien moi truong ngoai).",
            ""
        };

        await using var connection = new NpgsqlConnection(LabConfig.LabConnection);
        await connection.OpenAsync();

        lines.Add("== 1) Hangfire schema duoc tao tu dong trong culinary_lab ==");
        lines.AddRange(await RowsAsync(connection, """
            SELECT count(*)::text AS so_bang
            FROM information_schema.tables WHERE table_schema = 'hangfire'
            """));
        lines.AddRange(await RowsAsync(connection, """
            SELECT table_name FROM information_schema.tables
            WHERE table_schema = 'hangfire' AND table_name IN ('job','state','jobqueue','hash','server')
            ORDER BY table_name
            """));
        lines.Add("");

        lines.Add("== 2) Job da enqueue (3 job moi nhat) ==");
        lines.Add("Luu y: Hangfire xoa dong khoi hangfire.jobqueue ngay khi job bat dau chay, nen queue = '-'");
        lines.Add("voi job da Succeeded la dung thoi (khong phai job mat noi).");
        lines.Add("hangfire.job.statename la trang thai HIEN TAI (bang state la log lich su, khong dung de dem trang thai con lai).");
        lines.Add("so_lan_chay dem tu cac dong state='Processing' cua job.");
        lines.Add("id | statename | queue | so_lan_chay | createdat");
        lines.AddRange(await RowsAsync(connection, """
            SELECT j.id::text,
                   j.statename,
                   COALESCE((SELECT q.queue FROM hangfire.jobqueue q WHERE q.jobid = j.id LIMIT 1), '-'),
                   (SELECT count(*) FROM hangfire.state p WHERE p.jobid = j.id AND p.name = 'Processing')::text,
                   j.createdat::text
            FROM hangfire.job j
            ORDER BY j.id DESC LIMIT 3
            """));
        lines.Add("");

        lines.Add("== 3) Lich su trang thai 4 job cuoi (bang state — log lich su ap dung) ==");
        lines.Add("jobid | state | reason | createdat");
        lines.AddRange(await RowsAsync(connection, """
            SELECT jobid::text, name, COALESCE(reason, '-'), createdat::text
            FROM hangfire.state
            WHERE jobid IN (SELECT id FROM hangfire.job ORDER BY id DESC LIMIT 4)
            ORDER BY jobid, id
            """));
        lines.Add("");

        lines.Add("== 4) Trang thai HIEN TAI cua moi job (bang hangfire.job.statename) ==");
        lines.AddRange(await RowsAsync(connection, """
            SELECT statename, count(*)::text FROM hangfire.job GROUP BY statename ORDER BY statename
            """));
        lines.Add("Cac job o trang thai Enqueued/Scheduled deu la job CHUA duoc worker xu ly (con nam trong jobqueue).");
        lines.Add("Neu con so > 0 sau khi lab dung worker, do nguyen nhan queue mismatch (worker nghe queue khac job).");
        lines.AddRange(await RowsAsync(connection, """
            SELECT 'so_job_Failed_cuoi', count(*)::text FROM hangfire.job WHERE statename = 'Failed'
            """));
        lines.AddRange(await RowsAsync(connection, """
            SELECT 'hangfire.jobqueue_con_lai', count(*)::text FROM hangfire.jobqueue
            """));
        lines.Add("");

        lines.Add("== 5) Recurring hash sau khi don dep (RemoveIfExists) ==");
        var hash = await RowsAsync(connection, "SELECT key, value FROM hangfire.hash ORDER BY key");
        lines.AddRange(hash.Count > 0 ? hash : ["(bang hangfire.hash trong — RemoveIfExists('lab-sitemap') da chay)"]);
        lines.Add("");

        lines.Add("== 6) Cot that cua hangfire.job (de kiem chung so lieu o tren) ==");
        lines.AddRange(await RowsAsync(connection, """
            SELECT string_agg(column_name, ', ' ORDER BY ordinal_position)
            FROM information_schema.columns
            WHERE table_schema = 'hangfire' AND table_name = 'job'
            """));
        lines.Add("");

        if (LabConfig.AppConnection is { } appConnection)
        {
            lines.Add("== 7) Sitemap lay tu recipe Published trong culinary_test (chi doc) ==");
            await using var app = new NpgsqlConnection(appConnection);
            await app.OpenAsync();
            lines.AddRange(await RowsAsync(app, """
                SELECT CASE r."Status"
                           WHEN 0 THEN 'Draft'
                           WHEN 1 THEN 'Published'
                           WHEN 2 THEN 'Archived'
                           ELSE 'Khac'
                       END,
                       count(*)::text
                FROM "Recipes" r GROUP BY 1 ORDER BY 1
                """));
        }

        var text = string.Join(Environment.NewLine, lines) + Environment.NewLine;
        var path = Path.Combine(LabConfig.OutDir, OutputFileName);
        await File.WriteAllTextAsync(path, text, new System.Text.UTF8Encoding(false));
        logger.LogInformation("WROTE {Path} ({Lines} dong)", path, lines.Count);
        foreach (var line in lines.Where(l => l.Length > 0)) logger.LogInformation("[LAB/db] {Line}", line);
        return 0;
    }

    private static async Task<List<string>> RowsAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
        {
            var cells = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
                cells[i] = reader.IsDBNull(i) ? "-" : reader.GetValue(i).ToString() ?? "-";
            rows.Add("  " + string.Join(" | ", cells));
        }

        return rows;
    }
}

/// <summary>
/// Phase purge (diagnostic, không tính vào 4 phase yêu cầu): xoá các job còn nằm trong
/// <c>Enqueued</c>/<c>Scheduled</c> trong database lab — tức là job worker chưa bao giờ xử lý được.
/// Chỉ chạy trên DB lab (<c>culinary_lab</c>); sản phẩm dùng Hangfire Dashboard để quản lý job.
/// </summary>
public static class PurgePhase
{
    public static async Task<int> RunAsync(ILogger logger)
    {
        var logger2 = LabLog.For("LAB/purge");
        logger2.LogWarning("Xoá job chưa được xử lý trong DB lab {Db} — KHÔNG chạy trên DB sản phẩm.",
            LabConfig.LabDatabase);
        await using var connection = new NpgsqlConnection(LabConfig.LabConnection);
        await connection.OpenAsync();

        var ids = await ScalarAsync(connection,
            "SELECT string_agg(id::text, ',') FROM hangfire.job WHERE statename IN ('Enqueued','Scheduled')");
        if (string.IsNullOrEmpty(ids))
        {
            logger2.LogInformation("Khong co job Enqueued/Scheduled can xoa.");
            return 0;
        }

        foreach (var sql in new[]
                 {
                     $"DELETE FROM hangfire.jobqueue WHERE jobid IN ({ids})",
                     $"DELETE FROM hangfire.state WHERE jobid IN ({ids})",
                     $"DELETE FROM hangfire.job WHERE id IN ({ids})"
                 })
        {
            await using var command = new NpgsqlCommand(sql, connection);
            logger2.LogInformation("Xoa {Count} dong: {Sql}", await command.ExecuteNonQueryAsync(),
                sql.Split("WHERE")[0].Trim());
        }

        logger2.LogInformation("Da don xong job chua xu ly (ids: {Ids}).", ids);
        logger.LogInformation("Phase purge xong.");
        return 0;
    }

    private static async Task<string?> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync();
        return value is DBNull or null ? null : value.ToString();
    }
}
