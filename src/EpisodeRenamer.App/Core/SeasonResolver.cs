using System.Text.RegularExpressions;

namespace EpisodeRenamer.Core;

public static class SeasonResolver
{
    private static readonly Dictionary<string, int> EnglishWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
        ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10,
        ["eleven"] = 11, ["twelve"] = 12, ["thirteen"] = 13, ["fourteen"] = 14,
        ["fifteen"] = 15, ["sixteen"] = 16, ["seventeen"] = 17, ["eighteen"] = 18,
        ["nineteen"] = 19, ["twenty"] = 20,
    };

    private static readonly Dictionary<string, int> ArabicWords = new(StringComparer.Ordinal)
    {
        ["\u0627\u0644\u0623\u0648\u0644"] = 1,
        ["\u0627\u0644\u062b\u0627\u0646\u064a"] = 2,
        ["\u0627\u0644\u062b\u0627\u0644\u062b"] = 3,
        ["\u0627\u0644\u0631\u0627\u0628\u0639"] = 4,
        ["\u0627\u0644\u062e\u0627\u0645\u0633"] = 5,
        ["\u0627\u0644\u0633\u0627\u062f\u0633"] = 6,
        ["\u0627\u0644\u0633\u0627\u0628\u0639"] = 7,
        ["\u0627\u0644\u062b\u0627\u0645\u0646"] = 8,
        ["\u0627\u0644\u062a\u0627\u0633\u0639"] = 9,
        ["\u0627\u0644\u0639\u0627\u0634\u0631"] = 10,
        ["\u0627\u0644\u062d\u0627\u062f\u064a \u0639\u0634\u0631"] = 11,
        ["\u0627\u0644\u062b\u0627\u0646\u064a \u0639\u0634\u0631"] = 12,
        ["\u0627\u0644\u062b\u0627\u0644\u062b \u0639\u0634\u0631"] = 13,
        ["\u0627\u0644\u0631\u0627\u0628\u0639 \u0639\u0634\u0631"] = 14,
        ["\u0627\u0644\u062e\u0627\u0645\u0633 \u0639\u0634\u0631"] = 15,
        ["\u0627\u0644\u0633\u0627\u062f\u0633 \u0639\u0634\u0631"] = 16,
        ["\u0627\u0644\u0633\u0627\u0628\u0639 \u0639\u0634\u0631"] = 17,
        ["\u0627\u0644\u062b\u0627\u0645\u0646 \u0639\u0634\u0631"] = 18,
        ["\u0627\u0644\u062a\u0627\u0633\u0639 \u0639\u0634\u0631"] = 19,
        ["\u0627\u0644\u0639\u0634\u0631\u0648\u0646"] = 20,
    };

    public static int? Resolve(string? name, bool allowGeneric = false)
    {
        if (string.IsNullOrEmpty(name)) return null;
        string n = TextNormalizer.Normalize(name);
        if (string.IsNullOrEmpty(n)) return null;

        const string S = @"[\s._-]*";
        const RegexOptions IC = RegexOptions.IgnoreCase;

        // 1) Season N
        var m = Regex.Match(n, $@"Season{S}([0-9]+)", IC);
        if (m.Success) return int.Parse(m.Groups[1].Value);

        // 2) Season Roman
        m = Regex.Match(n, $@"Season{S}([IVXLCDM]+)$", IC);
        if (m.Success)
        {
            int? r = RomanNumeralConverter.Convert(m.Groups[1].Value);
            if (r is not null && r > 0) return r;
        }

        // 3) Season English word
        m = Regex.Match(n, $@"Season{S}(Twenty|Nineteen|Eighteen|Seventeen|Sixteen|Fifteen|Fourteen|Thirteen|Twelve|Eleven|Ten|Nine|Eight|Seven|Six|Five|Four|Three|Two|One)", IC);
        if (m.Success && EnglishWords.TryGetValue(m.Groups[1].Value, out int w)) return w;

        // 4) الموسم|موسم Arabic-Indic digits
        m = Regex.Match(n, $@"(?:\u0627\u0644\u0645\u0648\u0633\u0645|\u0645\u0648\u0633\u0645){S}([\u0660-\u0669]+)", IC);
        if (m.Success) return int.Parse(TextNormalizer.ConvertLatinDigits(m.Groups[1].Value));

        // 5) الموسم|موسم Latin digits
        m = Regex.Match(n, $@"(?:\u0627\u0644\u0645\u0648\u0633\u0645|\u0645\u0648\u0633\u0645){S}([0-9]+)", IC);
        if (m.Success) return int.Parse(m.Groups[1].Value);

        // 6) الموسم|موسم Arabic word
        string arSeasonWords = $@"\u0627\u0644\u062d\u0627\u062f\u064a \u0639\u0634\u0631|\u0627\u0644\u062b\u0627\u0646\u064a \u0639\u0634\u0631|\u0627\u0644\u062b\u0627\u0644\u062b \u0639\u0634\u0631|\u0627\u0644\u0631\u0627\u0628\u0639 \u0639\u0634\u0631|\u0627\u0644\u062e\u0627\u0645\u0633 \u0639\u0634\u0631|\u0627\u0644\u0633\u0627\u062f\u0633 \u0639\u0634\u0631|\u0627\u0644\u0633\u0627\u0628\u0639 \u0639\u0634\u0631|\u0627\u0644\u062b\u0627\u0645\u0646 \u0639\u0634\u0631|\u0627\u0644\u062a\u0627\u0633\u0639 \u0639\u0634\u0631|\u0627\u0644\u0639\u0634\u0631\u0648\u0646|\u0627\u0644\u0623\u0648\u0644|\u0627\u0644\u062b\u0627\u0646\u064a|\u0627\u0644\u062b\u0627\u0644\u062b|\u0627\u0644\u0631\u0627\u0628\u0639|\u0627\u0644\u062e\u0627\u0645\u0633|\u0627\u0644\u0633\u0627\u062f\u0633|\u0627\u0644\u0633\u0627\u0628\u0639|\u0627\u0644\u062b\u0627\u0645\u0646|\u0627\u0644\u062a\u0627\u0633\u0639|\u0627\u0644\u0639\u0627\u0634\u0631";
        m = Regex.Match(n, $@"(?:\u0627\u0644\u0645\u0648\u0633\u0645|\u0645\u0648\u0633\u0645){S}({arSeasonWords})", IC);
        if (m.Success && ArabicWords.TryGetValue(m.Groups[1].Value, out int aw)) return aw;

        // 7) الجزء|جزء Arabic-Indic digits
        m = Regex.Match(n, $@"(?:\u0627\u0644\u062c\u0632\u0621|\u062c\u0632\u0621){S}([\u0660-\u0669]+)", IC);
        if (m.Success) return int.Parse(TextNormalizer.ConvertLatinDigits(m.Groups[1].Value));

        // 8) الجزء|جزء Latin digits
        m = Regex.Match(n, $@"(?:\u0627\u0644\u062c\u0632\u0621|\u062c\u0632\u0621){S}([0-9]+)", IC);
        if (m.Success) return int.Parse(m.Groups[1].Value);

        // 9) الجزء|جزء Arabic word
        m = Regex.Match(n, $@"(?:\u0627\u0644\u062c\u0632\u0621|\u062c\u0632\u0621){S}({arSeasonWords})", IC);
        if (m.Success && ArabicWords.TryGetValue(m.Groups[1].Value, out int aw2)) return aw2;

        // 10) م|موسم shortcut
        m = Regex.Match(n, @"(?:^|[\s._-])(?:\u0645|\u0645\u0648\u0633\u0645)[\s._-]*0*([0-9]+)");
        if (m.Success) return int.Parse(m.Groups[1].Value);

        // 11) ج|جزء shortcut
        m = Regex.Match(n, @"(?:^|[\s._-])(?:\u062c|\u062c\u0632\u0621)[\s._-]*0*([0-9]+)");
        if (m.Success) return int.Parse(m.Groups[1].Value);

        // 12) S shortcut
        m = Regex.Match(n, @"(?:^|[\s._-])[sS][\s._-]*0*([0-9]+)");
        if (m.Success) return int.Parse(m.Groups[1].Value);

        // 13) Nth Season
        m = Regex.Match(n, @"([0-9]+)(?:st|nd|rd|th)Season", IC);
        if (m.Success) return int.Parse(m.Groups[1].Value);

        // 14) Sx or SEx standalone
        m = Regex.Match(n, @"^[sS][eE]?[\s._-]*0*([0-9]+)$", IC);
        if (m.Success) return int.Parse(m.Groups[1].Value);

        // 15) Part/Pt/Cour/Cours
        m = Regex.Match(n, @"\b(?:Part|Pt\.?|Cour|Cours)[\s._-]*0*([0-9]+)");
        if (m.Success) return int.Parse(m.Groups[1].Value);

        // 16) pure 1-2 digit folder name
        m = Regex.Match(n, @"^\d{1,2}$");
        if (m.Success) return int.Parse(m.Value);

        // 17) allow generic: first number in string
        if (allowGeneric)
        {
            m = Regex.Match(n, @"0*(\d+)");
            if (m.Success) return int.Parse(m.Groups[1].Value);
        }

        return null;
    }
}
