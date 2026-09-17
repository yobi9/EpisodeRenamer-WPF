using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using EpisodeRenamer.Core;

namespace EpisodeRenamer.App.UI;

internal static class ThemeManager
{
    internal const string KeyWindowBackground = "WindowBackground";
    internal const string KeyInputBackground = "InputBackground";
    internal const string KeyTextBrush = "TextBrush";
    internal const string KeyMutedTextBrush = "MutedTextBrush";
    internal const string KeyBorderBrush = "BorderBrush";
    internal const string KeyGridBackground = "GridBackground";
    internal const string KeyGridHeaderBackground = "GridHeaderBackground";
    internal const string KeyGridHeaderForeground = "GridHeaderForeground";
    internal const string KeyGridRowForeground = "GridRowForeground";
    internal const string KeyStatusModifiedBrush = "StatusModifiedBrush";
    internal const string KeyStatusCorrectBrush = "StatusCorrectBrush";
    internal const string KeyStatusIgnoredBrush = "StatusIgnoredBrush";
    internal const string KeyThemeButtonBackground = "ThemeButtonBackground";

    internal const string LightGlyph = "\u2600\uFE0F";
    internal const string DarkGlyph = "\uD83C\uDF19";

    internal static bool IsDark(AppSettings? settings) => settings?.DarkTheme == true;

    internal static void Apply(Window window, bool dark)
    {
        var palette = dark ? DarkPalette() : LightPalette();
        var target = Application.Current?.Resources ?? window.Resources;
        foreach (var pair in palette)
            target[pair.Key] = pair.Value;
    }

    private static IReadOnlyDictionary<string, SolidColorBrush> LightPalette() => new Dictionary<string, SolidColorBrush>
    {
        [KeyWindowBackground] = Brush(0xF5, 0xF7, 0xFA),
        [KeyInputBackground] = Brush(0xFF, 0xFF, 0xFF),
        [KeyTextBrush] = Brush(0x1A, 0x1A, 0x1A),
        [KeyMutedTextBrush] = Brush(0x7F, 0x8C, 0x8D),
        [KeyBorderBrush] = Brush(0xD0, 0xD0, 0xD0),
        [KeyGridBackground] = Brush(0xFF, 0xFF, 0xFF),
        [KeyGridHeaderBackground] = Brush(0xEC, 0xF0, 0xF1),
        [KeyGridHeaderForeground] = Brush(0x2C, 0x3E, 0x50),
        [KeyGridRowForeground] = Brush(0x2C, 0x3E, 0x50),
        [KeyStatusModifiedBrush] = Brush(0xE8, 0xF8, 0xF5),
        [KeyStatusCorrectBrush] = Brush(0xF4, 0xF4, 0xF4),
        [KeyStatusIgnoredBrush] = Brush(0xFD, 0xEC, 0xEA),
        [KeyThemeButtonBackground] = Brush(0xFF, 0xFF, 0xFF)
    };

    private static IReadOnlyDictionary<string, SolidColorBrush> DarkPalette() => new Dictionary<string, SolidColorBrush>
    {
        [KeyWindowBackground] = Brush(0x1E, 0x1E, 0x1E),
        [KeyInputBackground] = Brush(0x2D, 0x2D, 0x30),
        [KeyTextBrush] = Brush(0xF0, 0xF0, 0xF0),
        [KeyMutedTextBrush] = Brush(0x9E, 0x9E, 0x9E),
        [KeyBorderBrush] = Brush(0x3F, 0x3F, 0x46),
        [KeyGridBackground] = Brush(0x25, 0x25, 0x26),
        [KeyGridHeaderBackground] = Brush(0x33, 0x33, 0x33),
        [KeyGridHeaderForeground] = Brush(0xCC, 0xCC, 0xCC),
        [KeyGridRowForeground] = Brush(0xE8, 0xE8, 0xE8),
        [KeyStatusModifiedBrush] = Brush(0x1E, 0x4A, 0x38),
        [KeyStatusCorrectBrush] = Brush(0x2B, 0x2B, 0x2B),
        [KeyStatusIgnoredBrush] = Brush(0x4A, 0x2E, 0x2E),
        [KeyThemeButtonBackground] = Brush(0x40, 0x40, 0x40)
    };

    private static SolidColorBrush Brush(byte r, byte g, byte b) =>
        new SolidColorBrush(Color.FromRgb(r, g, b));
}