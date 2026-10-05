using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Practice.Lab5;

public sealed record Check(string Name, bool Passed, string Detail);

public sealed record PhaseResult(string Phase, List<Check> Checks)
{
    public bool Passed => Checks.All(c => c.Passed);

    /// <param name="detail">
    /// Các dòng số liệu quan sát thêm (header, độ dài, thời gian) — ghi vào log để
    /// người review đối chiếu được mà không phải chạy lại lab.
    /// </param>
    public static PhaseResult From(string phase, List<Check> checks, IEnumerable<string>? detail = null)
    {
        var result = new PhaseResult(phase, checks);
        var logger = LabLog.For("LAB/result");
        foreach (var check in checks)
            logger.Log(check.Passed ? LogLevel.Information : LogLevel.Error,
                "[{Phase}] {Mark} {Name} - {Detail}", phase, check.Passed ? "PASS" : "FAIL", check.Name, check.Detail);

        if (detail is not null)
            foreach (var line in detail)
                logger.LogInformation("[{Phase}] INFO {Line}", phase, line);

        logger.LogInformation("[{Phase}] {Summary} - {Passed}/{Total} check OK", phase,
            result.Passed ? "PASS" : "FAIL", checks.Count(c => c.Passed), checks.Count);
        return result;
    }
}

/// <summary>Tổng hợp kết quả các phase (bằng chứng chính của lab L5).</summary>
public static class LabResults
{
    private static readonly List<PhaseResult> Phases = [];

    public static PhaseResult? Last => Phases.Count > 0 ? Phases[^1] : null;

    public static void Add(PhaseResult result)
    {
        Phases.Add(result);
        LabLog.For("LAB/summary").LogInformation("SUMMARY {Phase} {Status} ({Passed}/{Total})", result.Phase,
            result.Passed ? "PASS" : "FAIL", result.Checks.Count(c => c.Passed), result.Checks.Count);
    }

    public static bool WriteSummary()
    {
        var logger = LabLog.For("LAB/summary");
        var overall = Phases.All(p => p.Passed);
        logger.LogInformation("=== LAB L5 {Status} - {Phases} phase, {Checks} check ===", overall ? "PASS" : "FAIL",
            Phases.Count, Phases.Sum(p => p.Checks.Count));
        return overall;
    }
}