using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Practice.Lab4;

public sealed record Check(string Name, bool Passed, string Detail);

public sealed record PhaseResult(string Phase, List<Check> Checks)
{
    public bool Passed => Checks.All(c => c.Passed);

    public static PhaseResult From(string phase, List<Check> checks)
    {
        var result = new PhaseResult(phase, checks);
        var logger = LabLog.For("LAB/result");
        foreach (var check in checks)
            logger.Log(check.Passed ? LogLevel.Information : LogLevel.Error,
                "[{Phase}] {Mark} {Name} — {Detail}", phase, check.Passed ? "PASS" : "FAIL", check.Name, check.Detail);
        logger.LogInformation("[{Phase}] {Summary} — {Passed}/{Total} check OK", phase,
            result.Passed ? "PASS" : "FAIL", checks.Count(c => c.Passed), checks.Count);
        return result;
    }
}

/// <summary>Tổng hợp kết quả các phase (bằng chứng chính của lab L4).</summary>
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
        logger.LogInformation("=== LAB L4 {Status} — {Phases} phase, {Checks} check ===", overall ? "PASS" : "FAIL",
            Phases.Count, Phases.Sum(p => p.Checks.Count));
        return overall;
    }
}
