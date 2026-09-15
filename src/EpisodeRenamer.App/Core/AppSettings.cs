namespace EpisodeRenamer.Core;

public sealed class AppSettings
{
    public string? Path { get; set; }
    public string? Show { get; set; }
    public int Style { get; set; } = -1;
    public bool? Recurse { get; set; }
    public bool? CleanTags { get; set; }
    public string? CustomPattern { get; set; }
    public string? IgnorePatterns { get; set; }
    public bool? RenameSubtitles { get; set; }
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public bool? WindowMaximized { get; set; }
}