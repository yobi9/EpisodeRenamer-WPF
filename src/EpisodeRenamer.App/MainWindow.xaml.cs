using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        "EP01 (\u0623\u0646\u0645\u064a - \u0631\u0642\u0645\u064a\u0646)"
    };

    internal const string HeadlessMarker = "[headless-mode]";

    private readonly string _settingsPath;
    private readonly string _undoPath;
    private readonly SettingsManager _settings;
    private readonly UndoLogManager _undo;
    private readonly FileProcessingEngine _engine;
    private readonly DispatcherTimer _liveTimer;

    private bool _suppressLive;
    private bool _isRunning;
    private string? _lastRunMode;
    private RunStats? _lastRunStats;
    private List<ReportItem> _lastRunLog = new();
    private List<(int Start, int End)> _lastMissingRuns = new();

    public MainWindow()
    {
        InitializeComponent();
        _settingsPath = Path.Combine(ResolveAppRoot(), "EpisodeRenamer.settings.json");
        _undoPath = Path.Combine(ResolveAppRoot(), "EpisodeRenamer.undo.json");
        _settings = new SettingsManager(_settingsPath);
        _undo = new UndoLogManager(_undoPath);
        _undo.Load();
        _engine = new FileProcessingEngine(_undo);
        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _liveTimer.Tick += LivePreviewTick;

        _suppressLive = true;
        try
        {
            styleDropdown.ItemsSource = StyleItems;
            styleDropdown.SelectedIndex = 0;
            ApplySavedSettings(_settings.Load());
        }
        finally
        {
            _suppressLive = false;
        }
    }

    internal static bool IsHeadlessMode =>
        Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") == "1";

    internal string OutputText => output.Text;

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
    }

    private AppSettings CurrentSettings() => new AppSettings
    {
        Path = pathBox.Text,
        Show = showBox.Text,
        Style = styleDropdown.SelectedIndex,
        Recurse = recurseCheck.IsChecked ?? false,
        CleanTags = cleanTagsCheck.IsChecked ?? false
    };

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
        string style = styleDropdown.SelectedIndex >= 0 && styleDropdown.SelectedIndex < StyleItems.Length
            ? StyleItems[styleDropdown.SelectedIndex]
            : StyleItems[0];
        bool recurse = recurseCheck.IsChecked ?? false;
        bool cleanTags = cleanTagsCheck.IsChecked ?? false;
        string showName = showBox.Text.Trim();

        Task.Run(() =>
        {
            RunResult result = _engine.Run(path, previewOnly, recurse, style, showName, cleanTags,
                (current, total) =>
                {
                    int pct = total > 0 ? (int)((long)current * 100 / total) : 0;
                    progress.Dispatcher.BeginInvoke(new Action(() => progress.Value = pct));
                });

            progress.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (string line in result.OutputLines) AppendLine(line);
                progress.Value = 100;
                _lastRunMode = result.Mode;
                _lastRunStats = result.Stats;
                _lastRunLog = result.Log;
                _lastMissingRuns = result.MissingRuns;
                if (!previewOnly) _settings.Save(CurrentSettings());
                _isRunning = false;
                SetBusy(false);
            }));
        });
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

    private void Window_Closed(object? sender, EventArgs e)
    {
        if (IsHeadlessMode) return;
        _settings.Save(CurrentSettings());
    }
}