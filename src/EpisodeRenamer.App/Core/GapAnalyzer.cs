namespace EpisodeRenamer.Core;

public sealed record MissingAnalysis(int Count, IReadOnlyList<int> Missing, IReadOnlyList<(int Start, int End)> Runs);

public static class GapAnalyzer
{
    public static MissingAnalysis Analyze(IReadOnlyCollection<int> episodeNumbers)
    {
        List<int> sorted = episodeNumbers.Distinct().OrderBy(x => x).ToList();
        if (sorted.Count == 0)
            return new MissingAnalysis(0, Array.Empty<int>(), Array.Empty<(int, int)>());

        int min = sorted[0];
        int max = sorted[^1];

        List<int> missing = new();
        for (int i = min; i <= max; i++)
        {
            if (!sorted.Contains(i))
                missing.Add(i);
        }

        List<(int Start, int End)> runs = new();
        if (missing.Count > 0)
        {
            int start = missing[0];
            int prev = missing[0];
            for (int i = 1; i < missing.Count; i++)
            {
                if (missing[i] == prev + 1)
                {
                    prev = missing[i];
                }
                else
                {
                    runs.Add((start, prev));
                    start = missing[i];
                    prev = missing[i];
                }
            }
            runs.Add((start, prev));
        }

        return new MissingAnalysis(missing.Count, missing, runs);
    }
}
