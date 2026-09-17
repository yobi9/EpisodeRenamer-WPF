namespace EpisodeRenamer.Core;

public sealed record MissingAnalysis(int Count, IReadOnlyList<int> Missing, IReadOnlyList<(int Start, int End)> Runs);

public static class GapAnalyzer
{
    public static MissingAnalysis Analyze(
        IReadOnlyCollection<int> episodeNumbers,
        CancellationToken cancellationToken = default)
    {
        List<int> sorted = episodeNumbers.Distinct().OrderBy(x => x).ToList();
        if (sorted.Count == 0)
            return new MissingAnalysis(0, Array.Empty<int>(), Array.Empty<(int, int)>());

        const int MaxMissingItems = 10000;

        List<int> missing = new();
        List<(int Start, int End)> runs = new();
        long totalCount = 0;

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            int a = sorted[i];
            int b = sorted[i + 1];
            if (b - a <= 1)
                continue;

            int start = a + 1;
            int end = b - 1;
            runs.Add((start, end));

            long gapSize = (long)b - a - 1;
            totalCount += gapSize;

            if (missing.Count < MaxMissingItems)
            {
                int toAdd = (int)Math.Min(gapSize, MaxMissingItems - missing.Count);
                for (int m = start; m < start + toAdd; m++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    missing.Add(m);
                }
            }
        }

        int count = totalCount > int.MaxValue ? int.MaxValue : (int)totalCount;
        // للفجوات الصغيرة نحافظ على التوافق: Count == Missing.Count
        // للفجوات الهائلة (مادة عربية برقم كبير) نعيد Count الحقيقي لكن Missing مقتطعة
        if (totalCount <= MaxMissingItems)
            count = missing.Count;

        return new MissingAnalysis(count, missing, runs);
    }
}
