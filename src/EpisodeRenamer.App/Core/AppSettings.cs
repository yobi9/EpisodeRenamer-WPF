namespace EpisodeRenamer.Core;

public sealed class AppSettings
{
    public string? Path { get; set; }
    public string? Show { get; set; }
    public int Style { get; set; } = -1;
    public bool? Recurse { get; set; }
    public bool? CleanTags { get; set; }
}