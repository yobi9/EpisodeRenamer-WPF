namespace EpisodeRenamer.Core;

public sealed record RunStats(int Mapped, int Correct, int Ignored, int Missing);

public sealed record ReportItem(string Type, string? Name, string? Old, string? New, string? Reason)
{
    public int? Season { get; init; }
    public int? Episode { get; init; }
}

public sealed class RunResult
{
    public string Mode { get; set; } = "";
    public RunStats Stats { get; set; } = new(0, 0, 0, 0);
    public List<ReportItem> Log { get; } = new();
    public List<(int Start, int End)> MissingRuns { get; } = new();
    public List<(int Season, int Start, int End)> MissingRunsBySeason { get; } = new();
    public string? MissingFile { get; set; }
    public List<string> OutputLines { get; } = new();
    public bool Cancelled { get; set; }
}