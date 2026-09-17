using System.IO;
using EpisodeRenamer.Core;

namespace EpisodeRenamer.App.UI;

/// <summary>
/// Semantic kinds for the persistent inline status strip (F-01).
/// </summary>
internal enum RunStatusKind
{
    Idle,
    Busy,
    Success,
    Empty,
    Invalid,
    ReportWarning,
    Cancelled
}

/// <summary>
/// A single resolved status: what to show (ShortText) plus full detail for the tooltip.
/// </summary>
internal sealed record RunStatus(RunStatusKind Kind, string ShortText, string? Detail);

/// <summary>
/// Presentation-side resolver mapping a completed <see cref="RunResult"/>
/// to user-facing status. Pure logic, no UI dependencies.
/// Precedence: Invalid &gt; Cancelled &gt; ReportWarning &gt; Empty &gt; Success.
/// </summary>
internal static class RunStatusResolver
{
    internal static RunStatus Idle() => new(
        RunStatusKind.Idle,
        "\u0627\u062e\u062a\u0631 \u0645\u062c\u0644\u062f\u064b\u0627 \u062b\u0645 \u0627\u0636\u063a\u0637 \u0645\u0639\u0627\u064a\u0646\u0629.",
        null);

    internal static RunStatus Busy(bool previewOnly) => new(
        RunStatusKind.Busy,
        previewOnly
            ? "\u23f3 \u062c\u0627\u0631\u064d \u062a\u0646\u0641\u064a\u0630 \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629\u2026"
            : "\u23f3 \u062c\u0627\u0631\u064d \u062a\u0646\u0641\u064a\u0630 \u0625\u0639\u0627\u062f\u0629 \u0627\u0644\u062a\u0633\u0645\u064a\u0629\u2026",
        null);

    internal static RunStatus Resolve(RunResult result, string? attemptedPath)
    {
        string realPath = attemptedPath?.Trim() ?? string.Empty;

        // Invalid mirrors the engine's own guard (Directory.Exists) instead of
        // fragile output-text matching, so Invalid and Empty can never merge.
        if (!Directory.Exists(realPath))
        {
            return new RunStatus(
                RunStatusKind.Invalid,
                "\u26a0\ufe0f \u0627\u0644\u0645\u0633\u0627\u0631 \u063a\u064a\u0631 \u0635\u0627\u0644\u062d \u0623\u0648 \u063a\u064a\u0631 \u0645\u0648\u062c\u0648\u062f \u2014 \u062a\u062d\u0642\u0642 \u0645\u0646 \u0627\u0644\u0645\u0633\u0627\u0631 \u062b\u0645 \u0623\u0639\u062f \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629.",
                string.IsNullOrEmpty(realPath) ? null : realPath);
        }

        if (result.Cancelled)
        {
            return new RunStatus(
                RunStatusKind.Cancelled,
                "\u23f9 \u062a\u0645 \u0625\u064a\u0642\u0627\u0641 \u0627\u0644\u0639\u0645\u0644\u064a\u0629.",
                null);
        }

        bool preview = result.Mode != ReportExporter.ModeExecute;

        // Report warning only when gaps actually exist but no file was saved,
        // so a read-only folder without gaps never raises a false warning.
        if (result.MissingRunsBySeason.Count > 0 && result.MissingFile is null)
        {
            // Keep in sync with MissingEpisodesWriter target file name.
            string targetFile = Path.Combine(realPath, "\u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629.txt");
            return new RunStatus(
                RunStatusKind.ReportWarning,
                preview
                    ? "\u26a0\ufe0f \u0627\u0643\u062a\u0645\u0644\u062a \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629 \u0628\u0646\u062c\u0627\u062d\u060c \u0644\u0643\u0646 \u062a\u0639\u0630\u0631 \u062d\u0641\u0638 \u0645\u0644\u0641 \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629 \u2014 \u062a\u062d\u0642\u0642 \u0645\u0646 \u0635\u0644\u0627\u062d\u064a\u0627\u062a \u0627\u0644\u0643\u062a\u0627\u0628\u0629."
                    : "\u26a0\ufe0f \u0627\u0643\u062a\u0645\u0644 \u0627\u0644\u062a\u0646\u0641\u064a\u0630 \u0628\u0646\u062c\u0627\u062d\u060c \u0644\u0643\u0646 \u062a\u0639\u0630\u0631 \u062d\u0641\u0638 \u0645\u0644\u0641 \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629 \u2014 \u062a\u062d\u0642\u0642 \u0645\u0646 \u0635\u0644\u0627\u062d\u064a\u0627\u062a \u0627\u0644\u0643\u062a\u0627\u0628\u0629.",
                targetFile);
        }

        // Empty is a property of the actual result (no rows at all) on an
        // existing directory — provably distinct from Invalid above.
        if (result.Log.Count == 0)
        {
            return new RunStatus(
                RunStatusKind.Empty,
                "\u2139\ufe0f \u0644\u0627 \u062a\u0648\u062c\u062f \u0645\u0644\u0641\u0627\u062a \u0641\u064a\u062f\u064a\u0648 \u0641\u064a \u0627\u0644\u0645\u062c\u0644\u062f \u0627\u0644\u0645\u062d\u062f\u062f.",
                realPath);
        }

        RunStats stats = result.Stats;
        return new RunStatus(
            RunStatusKind.Success,
            preview
                ? "\u2705 \u0627\u0643\u062a\u0645\u0644\u062a \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629: " + stats.Mapped + " \u0633\u064a\u064f\u0639\u062f\u064e\u0651\u0644\u060c " + stats.Correct + " \u0635\u062d\u064a\u062d\u060c " + stats.Ignored + " \u0645\u062a\u062c\u0627\u0647\u0644."
                : "\u2705 \u0627\u0643\u062a\u0645\u0644\u062a \u0627\u0639\u0627\u062f\u0629 \u0627\u0644\u062a\u0633\u0645\u064a\u0629: " + stats.Mapped + " \u0639\u064f\u062f\u0650\u0651\u0644\u060c " + stats.Correct + " \u0635\u062d\u064a\u062d\u060c " + stats.Ignored + " \u0645\u062a\u062c\u0627\u0647\u0644.",
            null);
    }
}
