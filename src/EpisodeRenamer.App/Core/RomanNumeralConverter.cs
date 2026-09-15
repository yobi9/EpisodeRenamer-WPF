namespace EpisodeRenamer.Core;

public static class RomanNumeralConverter
{
    public static int? Convert(string? s)
    {
        if (string.IsNullOrEmpty(s)) return 0;

        int total = 0;
        int prev = 0;
        for (int i = s.Length - 1; i >= 0; i--)
        {
            char ch = char.ToUpperInvariant(s[i]);
            int v = ch switch
            {
                'I' => 1, 'V' => 5, 'X' => 10, 'L' => 50,
                'C' => 100, 'D' => 500, 'M' => 1000,
                _ => -1,
            };
            if (v < 0) return null;
            total += v < prev ? -v : v;
            prev = v;
        }
        return total;
    }
}
