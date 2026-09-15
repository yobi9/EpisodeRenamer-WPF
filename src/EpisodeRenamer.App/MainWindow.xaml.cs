using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using EpisodeRenamer.App.UI;
using EpisodeRenamer.Core;

namespace EpisodeRenamer.App;

public partial class MainWindow : Window
{
    internal static readonly string[] StyleItems = new[]
    {
        "S01E01 (\u0646\u0645\u0637 \u0628\u0644\u064a\u0643\u0633 \u0627\u0644\u0642\u064a\u0627\u0633\u064a)",
        "S01.E01 (\u0646\u0645\u0637 \u0627\u0644\u062a\u0648\u0631\u0646\u062a)",
        "Season 01 Episode 01 (\u0648\u0635\u0641\u064a)",
        "EP 001 (\u0623\u0646\u0645\u064a - 3 \u0623\u0631\u0642\u0627\u0645)",
        "#01 (\u0623\u0646\u0645\u064a \u0645\u062e\u062a\u0635\u0631)",
        "\u0627\u0644\u0645\u0648\u0633\u0645 01 - \u0627\u0644\u062d\u0644\u0642\u0629 01 (\u0639\u0631\u0628\u064a)",
        "S1E1 (\u0628\u062f\u0648\u0646 \u0623\u0635\u0641\u0627\u0631)",
        "EP01 (\u0623\u0646\u0645\u064a - \u0631\u0642\u0645\u064a\u0646)",
        "\u0642\u0627\u0644\u0628 \u0645\u062e\u0635\u0635 (Custom...)"
    };

    internal const int CustomStyleIndex = 8;

    internal const string HeadlessMarker = "[headless-mode]";

    private readonly string _settingsPath;
    private readonly string _undoPath;
    private readonly SettingsManager _settings;
    private readonly UndoLogManager _undo;
    private readonly FileProcessingEngine _engine;
    private readonly DispatcherTimer _liveTimer;
    private readonly ObservableCollection<PreviewRow> _previewRows = new();

    private CancellationTokenSource? _cts;
    private bool _suppressLive;
    private bool _isRunning;
    private bool _isDark;
    private string? _lastRunMode;
    private RunStats? _lastRunStats;
    private List<ReportItem> _lastRunLog = new();
    private List<(int Start, int End)> _lastMissingRuns = new();

    public MainWindow() : this(null, null)
    {
    }

    internal MainWindow(string? settingsPath, string? undoPath)
    {
        InitializeComponent();
        previewGrid.ItemsSource = _previewRows;
        _settingsPath = settingsPath ?? Path.Combine(ResolveAppRoot(), "EpisodeRenamer.settings.json");
        _undoPath = undoPath ?? Path.Combine(ResolveAppRoot(), "EpisodeRenamer.undo.json");
        _settings = new SettingsManager(_settingsPath);
        _undo = new UndoLogManager(_undoPath);
        _undo.Load();
        _engine = new FileProcessingEngine(_undo);
        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _liveTimer.Tick += LivePreviewTick;

        AppSettings? saved = _settings.Load();
        _suppressLive = true;
        try
        {
            styleDropdown.ItemsSource = StyleItems;
            styleDropdown.SelectedIndex = 0;
            ApplySavedSettings(saved);
        }
        finally
        {
            _suppressLive = false;
        }
        WindowBoundsHelper.Apply(this, saved);
        ApplyTheme(ThemeManager.IsDark(saved));
    }

    internal static bool IsHeadlessMode =>
        Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") == "1";

    internal string OutputText => output.Text;

    internal string PatternBoxText => patternBox.Text.Trim();
    internal string IgnoreBoxText => ignoreBox.Text.Trim();
    internal bool SubtitleCheckChecked => subtitleCheck.IsChecked ?? false;

    private static string ResolveAppRoot()
    {
        try
        {
            string baseDir = AppContext.BaseDirectory;
            if (Directory.Exists(baseDir)) return baseDir;
        }
        catch { }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EpisodeRenamer");
    }

    private void ApplySavedSettings(AppSettings? s)
    {
        if (s is null) return;
        if (!string.IsNullOrWhiteSpace(s.Path)) pathBox.Text = s.Path;
        if (!string.IsNullOrWhiteSpace(s.Show)) showBox.Text = s.Show!;
        if (s.Style >= 0 && s.Style < StyleItems.Length) styleDropdown.SelectedIndex = s.Style;
        if (s.Recurse.HasValue) recurseCheck.IsChecked = s.Recurse.Value;
        if (s.CleanTags.HasValue) cleanTagsCheck.IsChecked = s.CleanTags.Value;
        if (s.CustomPattern is not null) patternBox.Text = s.CustomPattern;
        if (s.IgnorePatterns is not null) ignoreBox.Text = s.IgnorePatterns;
        if (s.RenameSubtitles.HasValue) subtitleCheck.IsChecked = s.RenameSubtitles.Value;
    }

    private AppSettings CurrentSettings() => new AppSettings
    {
        Path = pathBox.Text,
        Show = showBox.Text,
        Style = styleDropdown.SelectedIndex,
        Recurse = recurseCheck.IsChecked ?? false,
        CleanTags = cleanTagsCheck.IsChecked ?? false,
        CustomPattern = patternBox.Text,
        IgnorePatterns = ignoreBox.Text,
        RenameSubtitles = subtitleCheck.IsChecked ?? false,
        DarkTheme = _isDark
    };

    internal void ApplyTheme(bool dark)
    {
        _isDark = dark;
        ThemeManager.Apply(this, dark);
        themeBtn.Content = dark ? ThemeManager.DarkGlyph : ThemeManager.LightGlyph;
    }

    private void ThemeBtn_Click(object sender, RoutedEventArgs e)
    {
        if (IsHeadlessMode)
        {
            AppendLine(HeadlessMarker + " toggle theme (stubbed)");
            return;
        }
        bool dark = !_isDark;
        ApplyTheme(dark);
        _settings.Save(CurrentSettings());
        AppendLine(dark
            ? "\uD83C\uDF19 \u062A\u0645 \u062A\u0641\u0639\u064A\u0644 \u0627\u0644\u0648\u0636\u0639 \u0627\u0644\u0644\u064A\u0644\u064A"
            : "\u2600\uFE0F \u062A\u0645 \u062A\u0641\u0639\u064A\u0644 \u0627\u0644\u0648\u0636\u0639 \u0627\u0644\u0646\u0647\u0627\u0631\u064A");
    }

    private void AppendLine(string text)
    {
        output.AppendText(text + Environment.NewLine);
        output.ScrollToEnd();
    }

    private void SetBusy(bool busy)
    {
        previewBtn.IsEnabled = !busy;
        renameBtn.IsEnabled = !busy;
        undoBtn.IsEnabled = !busy;
        exportBtn.IsEnabled = !busy;
        stopBtn.IsEnabled = busy;
        exportSettingsBtn.IsEnabled = !busy;
        importSettingsBtn.IsEnabled = !busy;
    }

    private void AutoFillShowName(string path)
    {
        string? candidate = ShowNameDetector.Detect(path);
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            _suppressLive = true;
            try { showBox.Text = candidate; }
            finally { _suppressLive = false; }
        }
    }

    private void BrowseBtn_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        try
        {
            if (IsHeadlessMode)
            {
                AppendLine(HeadlessMarker + " browse folder (stubbed)");
                return;
            }
            string? picked = NativeFolderPicker.PickFolder(this, pathBox.Text.Trim());
            if (string.IsNullOrWhiteSpace(picked)) return;
            if (Directory.Exists(picked))
            {
                pathBox.Text = picked;
                showBox.Clear();
                AutoFillShowName(picked);
                AppendLine("[browse] " + picked);
            }
        }
        finally { SetBusy(false); }
    }

    private void SuggestBtn_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        try
        {
            if (IsHeadlessMode)
            {
                AppendLine(HeadlessMarker + " suggest name (stubbed)");
                return;
            }
            string? initial = ShowNameDetector.Detect(pathBox.Text.Trim());
            var dialog = new ShowNameDialog(initial) { Owner = this };
            bool? ok = dialog.ShowDialog();
            if (ok == true && !string.IsNullOrWhiteSpace(dialog.ResultText))
            {
                showBox.Text = dialog.ResultText!;
                AppendLine("[suggest] " + dialog.ResultText);
            }
        }
        finally { SetBusy(false); }
    }

    private void PreviewBtn_Click(object sender, RoutedEventArgs e)
    {
        if (IsHeadlessMode)
        {
            AppendLine(HeadlessMarker + " preview (stubbed)");
            return;
        }
        ProcessFiles(previewOnly: true);
    }

    private void RenameBtn_Click(object sender, RoutedEventArgs e)
    {
        if (IsHeadlessMode)
        {
            AppendLine(HeadlessMarker + " rename (stubbed)");
            return;
        }
        if (!DialogService.Confirm(this, "\u0647\u0644 \u0623\u0646\u062a \u0645\u062a\u0623\u0643\u062f \u0645\u0646 \u0625\u0639\u0627\u062f\u0629 \u062a\u0633\u0645\u064a\u0629 \u0627\u0644\u0645\u0644\u0641\u0627\u062a\u061f", "\u062a\u0623\u0643\u064a\u062f \u0627\u0644\u0639\u0645\u0644\u064a\u0629"))
            return;
        ProcessFiles(previewOnly: false);
    }

    private void StopBtn_Click(object sender, RoutedEventArgs e)
    {
        if (IsHeadlessMode)
        {
            AppendLine(HeadlessMarker + " stop (stubbed)");
            return;
        }
        _cts?.Cancel();
        AppendLine("\u23f9\ufe0f \u062c\u0627\u0631\u064d \u0625\u064a\u0642\u0627\u0641 \u0627\u0644\u0639\u0645\u0644\u064a\u0629\u2026");
    }

    private void ExportSettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        try
        {
            if (IsHeadlessMode)
            {
                AppendLine(HeadlessMarker + " export settings (stubbed)");
                return;
            }
            string defaultName = "EpisodeRenamer-settings-" + DateTime.Now.ToString("yyyy-MM-dd") + ".json";
            string? savePath = DialogService.PickSavePath(this, Directory.Exists(pathBox.Text.Trim()) ? pathBox.Text.Trim() : null,
                defaultName, "JSON file (*.json)|*.json|All files (*.*)|*.*");
            if (string.IsNullOrWhiteSpace(savePath)) return;
            _settings.SaveTo(savePath, CurrentSettings());
            AppendLine("\ud83d\udcbe \u062a\u0645 \u062a\u0635\u062f\u064a\u0631 \u0627\u0644\u0625\u0639\u062f\u0627\u062f\u0627\u062a: " + savePath);
        }
        catch (Exception ex)
        {
            AppendLine("\u274c \u0641\u0634\u0644 \u0627\u0644\u062a\u0635\u062f\u064a\u0631: " + ex.Message);
        }
        finally { SetBusy(false); }
    }

    private void ImportSettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        try
        {
            if (IsHeadlessMode)
            {
                AppendLine(HeadlessMarker + " import settings (stubbed)");
                return;
            }
            string? openPath = DialogService.PickOpenPath(this, Directory.Exists(pathBox.Text.Trim()) ? pathBox.Text.Trim() : null,
                "JSON file (*.json)|*.json|All files (*.*)|*.*");
            if (string.IsNullOrWhiteSpace(openPath)) return;
            AppSettings? imported = _settings.LoadFrom(openPath);
            if (imported is null)
            {
                AppendLine("\u274c \u0641\u0634\u0644 \u0627\u0644\u0627\u0633\u062a\u064a\u0631\u0627\u062f: \u0645\u0644\u0641 \u063a\u064a\u0631 \u0635\u0627\u0644\u062d");
                return;
            }
            _suppressLive = true;
            try { ApplySavedSettings(imported); }
            finally { _suppressLive = false; }
            ApplyTheme(ThemeManager.IsDark(imported));
            _settings.Save(CurrentSettings());
            AppendLine("\u2705 \u062a\u0645 \u0627\u0633\u062a\u064a\u0631\u0627\u062f \u0627\u0644\u0625\u0639\u062f\u0627\u062f\u0627\u062a: " + openPath);
        }
        finally { SetBusy(false); }
    }

    private void UndoBtn_Click(object sender, RoutedEventArgs e)
    {
        if (IsHeadlessMode)
        {
            AppendLine(HeadlessMarker + " undo (stubbed)");
            return;
        }
        SetBusy(true);
        try
        {
            if (_undo.Log.Count == 0)
            {
                AppendLine("\uD83D\uDED1 \u0644\u0627 \u062A\u0648\u062C\u062F \u0639\u0645\u0644\u064A\u0627\u062A \u0644\u0644\u062A\u0631\u0627\u062C\u0639 \u0639\u0646\u0647\u0627!");
                return;
            }
            UndoAllAndNotify();
        }
        finally { SetBusy(false); }
    }

    private void ExportBtn_Click(object sender, RoutedEventArgs e)
    {
        if (IsHeadlessMode)
        {
            AppendLine(HeadlessMarker + " export (stubbed)");
            return;
        }
        try
        {
            if (_lastRunLog.Count == 0)
            {
                AppendLine("\uD83D\uDED1 \u0644\u0627 \u062A\u0648\u062C\u062F \u0646\u062A\u0627\u0626\u062C \u0644\u062A\u0635\u062F\u064A\u0631\u0647\u0627");
                return;
            }
            string mode = string.IsNullOrEmpty(_lastRunMode) ? ReportExporter.ModePreview : _lastRunMode!;
            string defaultName = ReportExporter.GetDefaultFileName(mode);
            string? initialDir = Directory.Exists(pathBox.Text.Trim()) ? pathBox.Text.Trim() : null;
            string? savePath = DialogService.PickSavePath(this, initialDir, defaultName,
                "Text file (*.txt)|*.txt|CSV file (*.csv)|*.csv");
            if (string.IsNullOrWhiteSpace(savePath)) return;
            var lines = ReportExporter.BuildLines(mode, _lastRunStats, _lastMissingRuns, _lastRunLog);
            ReportExporter.WriteReport(savePath, lines);
            AppendLine("\uD83D\uDCBE \u062A\u0645 \u062D\u0641\u0638 \u0627\u0644\u062A\u0642\u0631\u064A\u0631: " + savePath);
        }
        catch (Exception ex)
        {
            AppendLine("\u274C \u0641\u0634\u0644 \u0627\u0644\u062A\u0635\u062F\u064A\u0631: " + ex.Message);
        }
    }

    private void ProcessFiles(bool previewOnly)
    {
        if (_isRunning) return;
        _isRunning = true;
        SetBusy(true);
        output.Clear();
        progress.Value = 0;

        string path = pathBox.Text.Trim();
        int selectedStyle = styleDropdown.SelectedIndex >= 0 && styleDropdown.SelectedIndex < StyleItems.Length
            ? styleDropdown.SelectedIndex
            : 0;
        string style = selectedStyle == CustomStyleIndex
            ? StyleItems[CustomStyleIndex]
            : StyleItems[selectedStyle];
        bool recurse = recurseCheck.IsChecked ?? false;
        bool cleanTags = cleanTagsCheck.IsChecked ?? false;
        string showName = showBox.Text.Trim();
        string? customPattern = selectedStyle == CustomStyleIndex ? patternBox.Text.Trim() : null;
        string? ignorePatterns = string.IsNullOrWhiteSpace(ignoreBox.Text) ? null : ignoreBox.Text.Trim();
        bool renameSubtitles = subtitleCheck.IsChecked ?? false;

        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (PreviewRow row in _previewRows)
        {
            if (row.Editable && !string.IsNullOrWhiteSpace(row.NewName)
                && !string.Equals(row.OldName, row.NewName.Trim(), StringComparison.Ordinal))
                overrides[row.OldName] = row.NewName.Trim();
        }

        _cts = new CancellationTokenSource();
        CancellationToken ct = _cts.Token;

        Task.Run(() =>
        {
            RunResult result = _engine.Run(path, previewOnly, recurse, style, showName, cleanTags,
                (current, total) =>
                {
                    int pct = total > 0 ? (int)((long)current * 100 / total) : 0;
                    progress.Dispatcher.BeginInvoke(new Action(() => progress.Value = pct));
                },
                customPattern, ignorePatterns, renameSubtitles, ct, overrides);

            progress.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (string line in result.OutputLines) AppendLine(line);
                progress.Value = 100;
                _lastRunMode = result.Mode;
                _lastRunStats = result.Stats;
                _lastRunLog = result.Log;
                _lastMissingRuns = result.MissingRuns;
                PopulatePreviewGrid(result);
                if (!previewOnly) _settings.Save(CurrentSettings());
                _cts = null;
                _isRunning = false;
                SetBusy(false);
                if (!previewOnly && !result.Cancelled && result.Stats.Mapped > 0 && !IsHeadlessMode)
                    ShowToast("\u2705 \u062a\u0645 \u0625\u0639\u0627\u062f\u0629 \u062a\u0633\u0645\u064a\u0629 " + result.Stats.Mapped + " \u0645\u0644\u0641\u0627\u064b");
            }));
        });
    }

    private void PopulatePreviewGrid(RunResult result)
    {
        _previewRows.Clear();
        foreach (ReportItem item in result.Log)
        {
            bool editable = item.Type == "\u0645\u0639\u062f\u0644";
            string status = item.Type switch
            {
                "\u0645\u0639\u062f\u0644" => "\u0633\u064a\u0639\u062f\u0651\u0644",
                "\u0635\u062d\u064a\u062d" => "\u0635\u062d\u064a\u062d",
                _ => "\u0645\u062a\u062c\u0627\u0647\u0644"
            };
            string oldName = item.Old ?? item.Name ?? "";
            string newName = item.New ?? "";
            _previewRows.Add(new PreviewRow
            {
                Status = status,
                OldName = oldName,
                NewName = newName,
                Editable = editable
            });
        }
    }

    private void ShowToast(string message)
    {
        try
        {
            var toast = new ToastWindow(message) { Owner = this };
            toast.Show();
        }
        catch
        {
        }
    }

    private void StyleDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressLive) return;
        ScheduleLivePreview();
    }

    private void ShowBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressLive) return;
        ScheduleLivePreview();
    }

    private void ScheduleLivePreview()
    {
        _liveTimer.Stop();
        _liveTimer.Start();
    }

    private void LivePreviewTick(object? sender, EventArgs e)
    {
        _liveTimer.Stop();
        if (IsHeadlessMode) return;
        if (string.IsNullOrWhiteSpace(pathBox.Text.Trim())) return;
        ProcessFiles(previewOnly: true);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        switch (e.Key)
        {
            case Key.B:
                e.Handled = true;
                BrowseBtn_Click(this, new RoutedEventArgs());
                break;
            case Key.E:
                e.Handled = true;
                PreviewBtn_Click(this, new RoutedEventArgs());
                break;
            case Key.R:
                e.Handled = true;
                RenameBtn_Click(this, new RoutedEventArgs());
                break;
            case Key.Z:
                e.Handled = true;
                UndoBtn_Click(this, new RoutedEventArgs());
                break;
        }
    }

    private void PreviewGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_previewRows.Count == 0) return;
        if (previewGrid.SelectedItem is PreviewRow row && row.Editable)
        {
            previewGrid.BeginEdit();
        }
    }

    private void Form_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Form_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Form_DragDrop(object sender, DragEventArgs e)
    {
        SetBusy(true);
        try
        {
            if (IsHeadlessMode)
            {
                AppendLine(HeadlessMarker + " drop (stubbed)");
                return;
            }
            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                e.Data.GetData(DataFormats.FileDrop) is IList<string> files)
            {
                foreach (string f in files)
                {
                    if (Directory.Exists(f))
                    {
                        pathBox.Text = f;
                        AutoFillShowName(f);
                        break;
                    }
                }
            }
        }
        finally { SetBusy(false); }
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        PromptPendingUndo();
    }

    internal void PromptPendingUndo()
    {
        int count = _undo.Log.Count;
        if (count == 0) return;
        AppendLine("\u26A0\uFE0F \u062A\u0648\u062C\u062F " + count + " \u0639\u0645\u0644\u064A\u0629 \u0625\u0639\u0627\u062F\u0629 \u062A\u0633\u0645\u064A\u0629 \u0635\u0627\u0628\u0642\u0629 \u0644\u0645 \u064A\u062A\u0645 \u0627\u0644\u062A\u0631\u0627\u062C\u0639 \u0639\u0646\u0647\u0627.");
        if (IsHeadlessMode || DialogService.Confirm(this,
            "\u0648\u062C\u062F\u0646\u0627 \u0639\u0645\u0644\u064A\u0627\u062A \u0625\u0639\u0627\u062F\u0629 \u062A\u0633\u0645\u064A\u0629 \u0645\u0646 \u062C\u0644\u0633\u0629 \u0633\u0627\u0628\u0642\u0629.\n\u0647\u0644 \u062A\u0631\u064A\u062F \u0627\u0644\u062A\u0631\u0627\u062C\u0639 \u0639\u0646\u0647\u0627 \u0627\u0644\u0622\u0646\u061F",
            "\u0627\u0633\u062A\u0631\u062C\u0627\u0639 \u0627\u0644\u062A\u0631\u0627\u062C\u0639"))
            UndoAllAndNotify();
    }

    private void UndoAllAndNotify()
    {
        var results = _undo.UndoAll();
        foreach (var (entry, success) in results)
        {
            if (success)
                AppendLine("\u21A9 \u062A\u0645 \u0627\u0644\u062A\u0631\u0627\u062C\u0639 \u0639\u0646: " + entry.New + " -> " + entry.Old);
            else
                AppendLine("\u274C \u0641\u0634\u0644 \u0627\u0644\u062A\u0631\u0627\u062C\u0639 \u0639\u0646: " + entry.New);
        }
        AppendLine("\u2705 \u0627\u0643\u062A\u0645\u0644 \u0627\u0644\u062A\u0631\u0627\u062C\u0639");
        _settings.Save(CurrentSettings());
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        if (IsHeadlessMode) return;
        AppSettings settings = CurrentSettings();
        WindowBoundsHelper.Capture(this, settings);
        _settings.Save(settings);
    }
}