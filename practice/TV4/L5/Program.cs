using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Practice.Lab5;

public static class Program
{
    public static async Task<int> Main()
    {
        var logger = LabLog.For("LAB/main");

        Directory.CreateDirectory(LabConfig.OutDir);
        logger.LogInformation("=== LAB L5 bat dau (run {RunId}) ===", LabConfig.RunId);
        logger.LogInformation("Muc tieu: doc bang chung ve caching (SSR/ISR), rollback, anh, SEO, observability, multi-instance.");
        logger.LogInformation("API  = {Api}", LabConfig.ApiBase);
        logger.LogInformation("API2 = {Api2}", LabConfig.ApiBase2);
        logger.LogInformation("Web  = {Web}", LabConfig.WebBase);
        logger.LogInformation("Log  = {Log}", LabConfig.LogFile);

        if (!Directory.Exists(LabConfig.OutDir))
        {
            logger.LogError("Khong tao duoc thu muc out: {Dir}", LabConfig.OutDir);
            return 2;
        }

        var results = new List<(string Phase, Func<Task<PhaseResult>> Run)>
        {
            (SearchSsrPhase.Phase, SearchSsrPhase.RunAsync),
            (IsrDetailPhase.Phase, IsrDetailPhase.RunAsync),
            (QueryRollbackPhase.Phase, QueryRollbackPhase.RunAsync),
            (ImageOptPhase.Phase, ImageOptPhase.RunAsync),
            (SeoPhase.Phase, SeoPhase.RunAsync),
            (ObservabilityPhase.Phase, ObservabilityPhase.RunAsync),
            (MultiInstancePhase.Phase, MultiInstancePhase.RunAsync)
        };

        foreach (var (phase, run) in results)
        {
            logger.LogInformation("--- Bat dau phase: {Phase} ---", phase);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            PhaseResult result;
            try
            {
                result = await run();
            }
            catch (Exception ex)
            {
                // Phase lỗi không được làm hỏng cả lab: ghi nhận rồi sang phase sau.
                result = PhaseResult.From(phase,
                    [new Check($"phase {phase} chạy không lỗi ngoại lệ", false,
                        $"{ex.GetType().Name}: {ex.Message}")]);
            }
            sw.Stop();

            logger.LogInformation("--- Ket thuc phase {Phase} ({Status}, {Elapsed} ms) ---",
                phase, result.Passed ? "PASS" : "FAIL", sw.ElapsedMilliseconds);
            LabResults.Add(result);
        }

        var ok = LabResults.WriteSummary();
        logger.LogInformation("=== LAB L5 ket thuc: {Status} ===", ok ? "PASS" : "FAIL");
        logger.LogInformation("File log: {Log}", LabConfig.LogFile);

        return ok ? 0 : 1;
    }
}