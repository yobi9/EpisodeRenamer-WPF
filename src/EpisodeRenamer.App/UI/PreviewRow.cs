namespace EpisodeRenamer.App.UI;

internal sealed class PreviewRow
{
    public string Status { get; set; } = "";
    public string OldName { get; set; } = "";
    public string NewName { get; set; } = "";
    public bool Editable { get; set; }
}