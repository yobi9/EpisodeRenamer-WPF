# EpisodeRenamer-WPF -- Migration Plan & Architectural Blueprint

## 0. Constraints & Global Rules

| Rule | Value |
|---|---|
| Language | C# 12, .NET 8.0 |
| UI Framework | WPF (XAML) -- no WinForms |
| External Dependencies | None (zero NuGet packages beyond System.Text.Json inbox) |
| Output | Single-file self-contained EXE via dotnet publish |
| Target Architecture | win-x86 |
| UI Direction | Arabic RTL, full, beginner-oriented |
| Encoding | UTF-8 with BOM for all generated text files (missing-episodes log, reports) |

---

## 1. Full Prototype Functional Audit

### 1.1 Global State Variables

| Variable | Scope | Type | Purpose |
|---|---|---|---|
| $global:renameLog | global | ArrayList of PSCustomObject {Old, New} | Tracks every rename for undo; persisted to disk |
| $global:episodeNumbers | global | int[] | Collected episode numbers during a run for gap analysis |
| $global:appRoot | global | string | Base directory: scripts own folder or %APPDATA% fallback |
| $global:settingsFile | global | string | {appRoot}/EpisodeRenamer.settings.json |
| $global:undoFile | global | string | {appRoot}/EpisodeRenamer.undo.json |
| $script:lastLive | script | int | Environment.TickCount of last live-preview fire (debounce) |
| $script:suppressLive | script | bool | Suppresses live-preview during programmatic text changes |
| $script:lastRunStats | script | {Mapped, Correct, Ignored, Missing} | Summary from last run for report export |
| $script:lastRunLog | script | ArrayList | Full per-file log entries for report export |
| $script:lastRunMode | script | string | Whether last run was preview or actual rename |
| $script:lastMissingCount | script | int | Count of missing episodes from last analysis |
| $script:lastMissingRuns | script | int[][] | Consecutive-gap ranges [[start,end], ...] |

### 1.2 UI Control State & Style Initialization

#### Form (Main Window)
- Title: "  مغير أسماء الحلقات الاحترافي ULTRA"
- Size: 1000x760, BackColor RGB(245,247,250), Font Segoe UI 10pt
- StartPosition CenterScreen, RightToLeft=Yes, RightToLeftLayout=true
- AllowDrop=true (drag-and-drop), FormClosing calls Save-Settings

#### GroupBox groupPath (" مسار المجلد ")
| Control | Type | Size | Location | Notes |
|---|---|---|---|---|
| pathBox | TextBox | 650x30 | (110,32) | Border=FixedSingle, holds folder path |
| browseBtn | Button | 90x32 | (15,30) | Text "استعراض", blue RGB(52,152,219) |
| recurseCheck | CheckBox | AutoSize | (15,65) | Text "مجلدات فرعية" |

#### GroupBox groupSettings (" خيارات التسمية ")
| Control | Type | Size | Location | Notes |
|---|---|---|---|---|
| showLabel | Label | AutoSize | (740,35) | "اسم المسلسل:" |
| showBox | TextBox | 190x30 | (540,32) | Border=FixedSingle |
| styleLabel | Label | AutoSize | (325,35) | "نمط التسمية:" |
| styleDropdown | ComboBox | 200x30 | (120,32) | DropDownList, 8 items, default index 0 |
| cleanTagsCheck | CheckBox | AutoSize | (120,70) | "تنظيف علامات المصدر" |
| suggestBtn | Button | 110x32 | (540,67) | "اقتراح الاسم", purple RGB(155,89,182) |

#### styleDropdown items (exact order, 8 styles)
| Index | Display Text | Format Pattern | Episode Digits | Season Digits |
|---|---|---|---|---|
| 0 | S01E01 (نمط بليكس القياسي) | S{SS}E{EEE} | 3 | 2 |
| 1 | S01.E01 (نمط التورنت) | S{SS}.E{EEE} | 3 | 2 |
| 2 | Season 01 Episode 01 (وصفي) | Season {SS} Episode {EE} | 2 | 2 |
| 3 | EP 001 (أنمي - 3 أرقام) | EP {EEE} | 3 | n/a |
| 4 | #01 (أنمي مختصر) | #{EE} | 2 | n/a |
| 5 | الموسم 01 - الحلقة 01 (عربي) | الموسم {SS} الحلقة {EE} | 2 | 2 |
| 6 | S1E1 (بدون أصفار) | S{s}E{e} (raw ints) | raw | raw |
| 7 | EP01 (أنمي - رقمين) | EP{EE} | 2 | n/a |

#### Action Buttons (all 130x40, location row y=248)
| Control | Text | Location | Color |
|---|---|---|---|
| previewBtn | "👁 معاينة" | (20,248) | Green RGB(46,204,113) |
| renameBtn | "✍ إعادة تسمية" | (160,248) | Red RGB(231,76,60) |
| undoBtn | "↩ تراجع" | (300,248) | Gray RGB(149,165,166) |
| exportBtn | "💾 حفظ التقرير" | (440,248) | Navy RGB(52,73,94) |

#### Progress Bar & Output Console
| Control | Type | Size | Location | Notes |
|---|---|---|---|---|
| progress | ProgressBar | 960x14 | (20,298) | Style=Continuous |
| output | TextBox | 960x380 | (20,320) | Multiline, ReadOnly, VerticalScroll, Black BG, LimeGreen FG, Segoe UI 10pt, RTL=No (LTR log) |

#### Set-ModernButtonStyle($btn, $color)
- FlatStyle=Flat, BackColor=$color, ForeColor=White, Cursor=Hand, FlatAppearance.BorderSize=0, Font="Segoe UI 11pt Bold"

### 1.3 Get-FormattedName($season, $episode, $style)

Maps the 8 naming-style dropdown switches via switch -Wildcard.
Parameters: $season (int), $episode (int), $style (string -- first item text from $styleDropdown.SelectedItem).

Internal defaults: $s = season.ToString().PadLeft(2,'0') (2-digit), $e = episode.ToString().PadLeft(3,'0') (3-digit).

| Switch Case | Output Format | Notes |
|---|---|---|
| "S01E01*" | S{2dp}E{3dp} | e.g. S01E003 |
| "S01.E01*" | S{2dp}.E{3dp} | e.g. S01.E003 |
| "Season 01 Episode 01*" | Season {2dp} Episode {2dp} | Episode re-padded to 2 digits |
| "EP 001*" | EP {3dp} | e.g. EP 003 |
| "#01*" | #{2dp} | Episode re-padded to 2 digits |
| "الموسم 01 - الحلقة 01*" | الموسم {2dp} الحلقة {2dp} | Both 2-digit |
| "S1E1*" | S{raw}E{raw} | No zero-padding |
| "EP01*" | EP{2dp} | Episode 2-digit, no space |
| default | S{2dp}E{3dp} | Same as case 0 |

### 1.4 Convert-LatinDigits($text)

Sequential character replacement: Eastern Arabic (Hindi) digits to Latin digits via 10 chained -replace.

| Arabic | ٠ | ١ | ٢ | ٣ | ٤ | ٥ | ٦ | ٧ | ٨ | ٩ |
|---|---|---|---|---|---|---|---|---|---|---|
| Latin | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |

### 1.5 Convert-NormalizedText($text) -- Pipeline (5 steps)

1. Guard: if IsNullOrWhiteSpace return as-is
2. Convert-LatinDigits (Hindi to Latin digits)
3. -replace '\u0640','' -- strip Kashida (Tatweel)
4. -replace '[._\-––\[\]()]+',' ' -- normalize all punctuation/separators to single space (handles ., _, -, en-dash –, em-dash —, brackets)
5. -replace '\s{2,}',' ' -- collapse multiple spaces; then .Trim()

### 1.6 Remove-SourceTags($text)

Removes media-source metadata tags. Applied only when cleanTagsCheck is checked (called from Get-NewName show-name processing).

| # | Regex | Description |
|---|---|---|
| 1 | \[[^\]]*\] | Any bracket content [...] |
| 2 | مدبلج | Dubbed |
| 3 | مترجم | Subtitled |
| 4 | 960p | Resolution |
| 5 | 720p | Resolution |
| 6 | 1080p | Resolution |
| 7 | 4K | Resolution |
| 8 | WEB-?DL | Web-DL / WEBDL |
| 9 | BluRay | Blu-ray |
| 10 | \bHD\b | HD word boundary |

Post-cleanup: -replace '[\s_-]+',' ' (spaces/underscores/dashes to space); -replace '^[\s-]+|[\s-]+$','' (trim leading/trailing whitespace and dashes).

### 1.7 Detect-ShowNameFromFolder($path)

Extracts suggested show name from leaf folder name.
1. Guard: return '' if path null/empty
2. $leaf = Split-Path $path -Leaf; guard return '' if empty
3. Convert-NormalizedText (full normalization)
4. -replace '\[[^\]]*\]',' ' -- remove bracket content
5. -replace '[\s_]+',' ' then trim
6. -replace '(?i)\s*(Season\s*\d+|S\d+\s*E\d+|S\d+)[\s.,-]*$' -- remove Season/SxxExx/Sxx suffixes (case-insensitive)
7. -replace '(?:\s+|^)(الموسم\s*(?:الأول|الثاني|...|العاشر|[٠-٩\d]+))[\s.,-]*$' -- remove Arabic season suffixes (text or digit)
8. .Trim(' ','.','_','-') final trim

Arabic season words: الأول..العاشر plus [٠-٩\d]+ digit forms.

### 1.8 Auto-Fill-ShowName($path)

1. Guard: return if path null or not a valid directory
2. $candidate = Detect-ShowNameFromFolder; guard return if empty
3. Set $suppressLive=true (suppress live preview)
4. $showBox.Text = $candidate; restore $suppressLive=false
5. Log "✨ اسم المسلسل المقترح تلقائياً: {candidate}"

### 1.9 Show-NameDialog($initial)

Modal dialog 420x160, FixedDialog, no min/max, CenterParent, RTL=Yes.
- Label "اسم المسلسل:", TextBox 270x26 pre-filled, OK button "موافق", Cancel button "إلغاء"
- Returns trimmed TextBox text if OK, $null if Cancel
- Guard: returns $null if main form not visible

### 1.10 Save-Settings

Serializes to $global:settingsFile as UTF-8 JSON:
{ "Path": pathBox.Text, "Show": showBox.Text, "Style": styleDropdown.SelectedIndex, "Recurse": recurseCheck.Checked, "CleanTags": cleanTagsCheck.Checked }
Error-swallowed (try/catch).

### 1.11 Load-Settings

Deserializes from $global:settingsFile. Guards each field: Path/Show only if truthy; Style only if valid index [0, Items.Count); Recurse/CleanTags if not null. Entire load error-swallowed.

### 1.12 Save-UndoLog / 1.13 Load-UndoLog

Save-UndoLog: ConvertTo-Json -Depth 3 of $global:renameLog to $global:undoFile. Error-swallowed.
Load-UndoLog: Null to empty array; Array to array cast; Non-array wrapped in array; Error to empty array.

### 1.14 Convert-RomanNumeral($s)

Standard subtractive Roman numeral parser.
| Symbol | I | V | X | L | C | D | M |
|---|---|---|---|---|---|---|---|
| Value | 1 | 5 | 10 | 50 | 100 | 500 | 1000 |

Iterates right-to-left: if current < previous, subtract; else add. Returns $null on invalid char.

### 1.15 Resolve-SeasonFolderName($name, [switch]$AllowGeneric)

Multi-layered season detection from folder name. Returns [int?].
Preprocessing: Convert-NormalizedText $name, guard null.

Reference dictionaries:
- $enWords: one=1 .. twenty=20
- $arWords: الأول=1 .. العشرون=20 (includes compound teens like الحادي عشر=11)
- Separator pattern: $sep = '[\s._-]*'

Regex priority order (first match wins):
| # | Pattern | Capture | Return |
|---|---|---|---|
| 1 | Season{sep}(\d+) | digits | int |
| 2 | Season{sep}([IVXLCDM]+)$ (end) | roman | Convert-RomanNumeral() |
| 3 | Season{sep}(EnglishWord) | word | $enWords |
| 4 | (?:الموسم\|موسم){sep}([٠-٩]+) | Hindi digits | converted int |
| 5 | (?:الموسم\|موسم){sep}(\d+) | Latin digits | int |
| 6 | (?:الموسم\|موسم){sep}(ArabicWord) | ordinal | $arWords |
| 7 | (?:الجزء\|جزء){sep}([٠-٩]+) | Hindi digits | converted int |
| 8 | (?:الجزء\|جزء){sep}(\d+) | Latin digits | int |
| 9 | (?:الجزء\|جزء){sep}(ArabicWord) | ordinal | $arWords |
| 10 | (?:^\|[\s._-])(?:م\|موسم)[\s._-]*0*(\d+) | short Arabic | int |
| 11 | (?:^\|[\s._-])(?:ج\|جزء)[\s._-]*0*(\d+) | short Arabic | int |
| 12 | (?:^\|[\s._-])[sS][\s._-]*0*(\d+) | S-prefixed | int |
| 13 | (\d+)(?:st\|nd\|rd\|th){sep}Season | ordinal prefix | int |
| 14 | ^[sS][eE]?[\s._-]*0*(\d+)$ (full) | SE/S prefix | int |
| 15 | \b(?:Part\|Pt\.?\|Cour\|Cours)[\s._-]*0*(\d+) | Part/Pt/Cour | int |
| 16 | ^\d{1,2}$ (full) | bare 1-2 digit num | int |
| 17 | (AllowGeneric only) 0*(\d+) | any digits | int |

Returns $null if no match.

### 1.16 Get-SeasonNumber($file)

Multi-layer ancestral tree search:
1. Try $file.Name (normalized) for SxxExx, extract season
2. Walk directory tree from $file.Directory upward to root path:
   - Immediate parent only: Resolve-SeasonFolderName -AllowGeneric
   - All ancestors above: Resolve-SeasonFolderName WITHOUT -AllowGeneric
   - Stop when reaching $pathBox.Text root
3. Return $null if not found (caller defaults to season 1)

### 1.17 Get-NewName($file)

Returns ($episodeNumber, $newFileName) or (null, null).

Steps:
1. $nameLat = Convert-NormalizedText $file.Name
2. $nameBase = Convert-NormalizedText $file.BaseName
3. $season = Get-SeasonNumber (default 1 if null)

Episode extraction -- strict 7-tier regex sequence:
| Tier | Regex | Capture |
|---|---|---|
| 1 | [sS](\d+)[.\-\s]?[eE](\d+) | Group[2] = episode |
| 2 | (\d+)x(\d+) | Group[2] = episode |
| 3 | (?:Episode\|EP)[\s._-]*(\d+) | Group[1] = episode |
| 4 | (?:الحلقة\|حلقة)[\s._-]*(\d+) | Group[1] = episode |
| 5 | ^(\d{1,3})$ (full match on $nameBase) | Group[1] = episode |
| 6 | ^(\d{1,3})(?!\d) (full match on $nameBase) | Group[1] = episode |
| 7 | (?:[_\\s.\-])(\d{1,3})(?!\d) on $nameLat | Group[1] = episode |

If $ep not null:
- $style = styleDropdown.SelectedItem.ToString()
- $pattern = Get-FormattedName $season $ep $style
- $showName = $showBox.Text.Trim()
- If showName exists:
  - Strip all Path.GetInvalidFileNameChars() from showName
  - .TrimEnd(' ','.'); remove leading ^[-\\.\\s]+
  - If cleanTagsCheck: Remove-SourceTags then normalize separators
  - $newName = "{showName}-{pattern}{extension}"
- If no showName: $newName = "{pattern}{extension}"
- Return ($ep, $newName)
Return (null, null) if no episode found

### 1.18 Analyze-Missing

Gap analysis on $global:episodeNumbers.
1. Sort unique episode numbers
2. Find integers in [min,max] not present, group consecutive runs into $script:lastMissingRuns (array of [start,end])
3. Output: warning header "⚠️ تنبيه الحلقات المفقودة ({count} حلقة مفقودة):"; each run "• الحلقة {n}" or "• من الحلقة {start} إلى {end}"; truncation max 15 runs then "... ({remaining} نطاقات أخرى)"; no gaps: "✅ لا توجد حلقات مفقودة"

### 1.19 Save-MissingEpisodesFile($path)

Creates/updates "الحلقات المفقودة.txt" in episode folder.
- If lastMissingCount <= 0: delete existing file, return null
- Else create with UTF-8 BOM

File format:
=== ملف الحلقات المفقودة ===
التاريخ: {yyyy-MM-dd HH:mm:ss}
مجلد الحلقات: {path}
(blank)
عدد الحلقات المفقودة: {count}
(blank)
الحلقات المفقودة بالتفصيل:
------------------------
• الحلقة {n} مفقودة.
• نقص من الحلقة {start} إلى الحلقة {end}.
(blank)
=== نهاية الملف ===

Returns file path if created, null otherwise.

### 1.20 Process-Files($previewOnly) -- THE CORE ENGINE

Phase 1 -- Setup:
1. $path = pathBox.Text.Trim(); reset $global:episodeNumbers = @()
2. Validate path exists, error if not
3. Mode header: preview or rename
4. File-fetch: LiteralPath, File=$true, optionally Recurse
5. Video extension whitelist: \.(3gp|3g2|asf|avi|divx|f4v|flv|m2ts|m4v|mkv|mov|mp4|mpe|mpeg|mpg|mts|ogv|rm|rmvb|ts|vob|webm|wmv)$
6. $all = Get-ChildItem (all files)

Phase 2 -- Report log construction:
1. Non-video files -> reportLog {Type='متجاهل', Name, Reason='ليس ملف فيديو'}
2. Video files filtered, sorted by season number (null -> Int32.MaxValue)

Phase 3 -- Per-file processing:
1. Reset $targetSeen = @{}, counters, stopwatch
2. $progress.Maximum = files.Count
3. CRITICAL: only clear $global:renameLog if NOT preview mode
4. For each file:
   - Season header: separator + "الموسم {XX}" when season changes
   - Get-NewName
   - If new name exists:
     - Add episode to $episodeNumbers
     - $targetPath = Join-Path $file.DirectoryName $newName
     - Self-collision: if $newName -eq $file.Name -> mark "correct", no touch
     - External collision: if $targetSeen.ContainsKey OR file exists on disk -> skip, reason "يوجد ملف بنفس الاسم مسبقاً"
     - Otherwise: log as modified, add to $targetSeen
   - If no new name: log ignored, reason "لم يُعثر على رقم حلقة"
   - Execute rename (if not preview): Rename-Item, append to renameLog, Save-UndoLog
   - Increment progress, DoEvents()

Phase 4 -- Ignored files display: filter reportLog Type='متجاهل', show with reason
Phase 5 -- Summary: preview mode vs rename mode stats (mapped, correct, ignored, elapsed), past-tense Arabic for rename
Phase 6 -- Missing: Analyze-Missing + Save-MissingEpisodesFile $path
Phase 7 -- Store round data: lastRunMode, lastRunStats, lastRunLog; if rename mode: Save-Settings

### 1.21 Live-Preview (500ms debounce)

Guards: form visible; $suppressLive false; Environment.TickCount - lastLive >= 500. Action: clear output, Process-Files $true.

### 1.22 Set-Busy($busy)

Toggles previewBtn.Enabled, renameBtn.Enabled, undoBtn.Enabled = -not $busy.

### 1.23 Show-FolderPicker

Native Win32 COM folder picker (inline C# P/Invoke). Returns folder path string or null.
COM types:
- IFileOpenDialog GUID 42F85136-DB7E-439C-85F1-E4075D135FC8
- IShellItem GUID 43826D1E-E718-42EE-BC55-A1E261C37BFE
- FileOpenDialogRCW CLSID DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7

Options: FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_NOCHANGEDIR
Initial folder: SHCreateItemFromParsingName with pathBox.Text
Result: IShellItem.GetDisplayName(SIGDN_FILESYSPATH) -> Marshal.PtrToStringUni -> FreeCoTaskMem
Guard: returns null if form not visible.

### 1.24 Event Handlers

| Event | Behavior |
|---|---|
| browseBtn.Click | Show-FolderPicker. If path changed: clear showBox (suppress live), Auto-Fill-ShowName. Always log path. |
| suggestBtn.Click | Detect-ShowNameFromFolder -> Show-NameDialog. If non-null: set showBox (suppress live), log. |
| exportBtn.Click | Build Arabic report from lastRunMode/Stats/Log/MissingRuns. UTF-8 BOM via SaveFileDialog. |
| undoBtn.Click | Iterate renameLog reverse. Verify item.New exists, Rename-Item back to Path.GetFileName(item.Old). Clear log + save. Wrapped in Set-Busy. |
| styleDropdown.SelectedIndexChanged | Live-Preview |
| showBox.TextChanged | Live-Preview |
| previewBtn.Click | Set-Busy true -> clear output -> Process-Files $true -> Set-Busy false |
| renameBtn.Click | MessageBox confirm ("هل أنت متأكد...?") -> Set-Busy true -> clear -> Process-Files $false -> Set-Busy false |
| form.DragEnter | If FileDrop present -> Effect=Copy |
| form.DragDrop | If dropped[0] is container dir: same logic as browseBtn |
| form.FormClosing | Save-Settings |

### 1.25 Initialization Sequence

Load-Settings; Load-UndoLog; $form.ShowDialog() (blocking modal)

### 1.26 Report Export Structure (exportBtn)

UTF-8 BOM file:
=== تقرير إعادة تسمية الحلقات ===  (or === تقرير المعاينة ===)
التاريخ: {yyyy-MM-dd HH:mm:ss}
(blank)
( وضع / تم تنفيذ )
(blank)
إحصائيات سريعة:
------------------------
• إجمالي الملفات المعدلة: {Mapped}
• إجمالي الملفات المتجاهلة: {Ignored}
• الحلقات المفقودة: {Missing}
• أسماء صحيحة بالفعل (لم تُغيّر): {Correct}
(blank)
الحلقات المفقودة بالتفصيل:
------------------------
• الحلقة {n} مفقودة.
• نقص من الحلقة {start} إلى الحلقة {end}.
(blank)
سجل العمليات بالتفصيل:
------------------------
[تم التعديل]  {Old} -> {New}
[الاسم صحيح]  {Name}
[تم التجاهل]  {Name} ({Reason})
(blank)
=== نهاية التقرير ===

---

## 2. Modern WPF UI Binding

### 2.1 XAML Layout Architecture

Replace WinForms absolute positioning with responsive Grid-based layout.

Main Window structure:
- Window FlowDirection="RightToLeft", Title same Arabic title, window size ~1000x760 DIPs
- Grid rows: Auto (GroupPath), Auto (GroupSettings), Auto (ActionButtons row), Auto (ProgressBar), * (LogConsole)
- AllowDrop="True", DragEnter + Drop handlers

### 2.2 Data Binding Map (UI to Code-Behind)

| Prototype | WPF Control | Binding Strategy |
|---|---|---|
| pathBox | TextBox | Two-way binding to FolderPath or code-behind text access |
| browseBtn | Button | Click -> BrowseBtn_Click (NativeFolderPicker) |
| recurseCheck | CheckBox | Two-way binding to RecurseSubfolders |
| showBox | TextBox | Two-way binding to ShowName |
| styleDropdown | ComboBox | SelectedIndex binding to SelectedStyleIndex, ItemsSource=static style list |
| cleanTagsCheck | CheckBox | Two-way binding to CleanTags |
| suggestBtn | Button | Click -> SuggestBtn_Click (NameDialog) |
| previewBtn | Button | Click -> PreviewBtn_Click |
| renameBtn | Button | Click -> RenameBtn_Click (MessageBox confirm) |
| undoBtn | Button | Click -> UndoBtn_Click |
| exportBtn | Button | Click -> ExportBtn_Click (SaveFileDialog) |
| progress | ProgressBar | Value binding to ProgressValue, Maximum to ProgressMax |
| output | RichTextBox or styled TextBox | Black bg, LimeGreen fg, LTR, append-only, auto-scroll |

### 2.3 Dark-Themed LogConsole

Replicate: Background #000000, Foreground #32CD32, Segoe UI 10pt, FlowDirection LeftToRight, vertical auto-scroll, ReadOnly, append via Dispatcher.Invoke from background threads.

### 2.4 High-DPI Scaling

WPF handles DPI natively via DIPs. Use device-independent dimensions, SizeToContent preference, no fixed pixel sizing.

### 2.5 Button Styling (Set-ModernButtonStyle parity)

Style per button: Background hex, Foreground White, Segoe UI 14px Bold, BorderThickness 0, Cursor Hand, flat template.

| Button | Hex Color |
|---|---|
| Browse | #3498DB (blue) |
| Suggest | #9B59B6 (purple) |
| Preview | #2ECC71 (green) |
| Rename | #E74C3C (red) |
| Undo | #95A5A6 (gray) |
| Export | #34495E (navy) |

### 2.6 Drag-and-Drop

AllowDrop="True", DragEnter accepts FileDrop -> Effect Copy, Drop validates directory -> set path + auto-fill show name (same as browse).

---

## 3. Standalone Native File Dialog Design

### 3.1 Architecture

Static C# helper class inside WPF project. COM P/Invoke directly, no third-party DLLs.

### 3.2 COM Interface Definitions (ComImport)

| Type | GUID |
|---|---|
| IFileOpenDialog | 42F85136-DB7E-439C-85F1-E4075D135FC8 |
| IShellItem | 43826D1E-E718-42EE-BC55-A1E261C37BFE |
| FileOpenDialogRCW | DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7 |

### 3.3 P/Invoke Import

[DllImport("shell32.dll", CharSet = CharSet.Unicode)] SHCreateItemFromParsingName(string pszPath, IntPtr pbc, ref Guid riid, out IShellItem ppv)

### 3.4 Dialog Options (FOS Flags)

| Constant | Value |
|---|---|
| FOS_NOCHANGEDIR | 0x00000008 |
| FOS_PICKFOLDERS | 0x00000020 |
| FOS_FORCEFILESYSTEM | 0x00000040 |

### 3.5 Flow

1. new FileOpenDialogRCW() cast to IFileOpenDialog (CoCreateInstance)
2. GetOptions -> SetOptions (OR flags)
3. If initial path: SHCreateItemFromParsingName -> SetDefaultFolder
4. Show(owner) -- WindowInteropHelper.Handle as owner
5. If S_OK (>=0): GetResult -> GetDisplayName(SIGDN_FILESYSPATH) -> Marshal.PtrToStringUni
6. FreeCoTaskMem on returned pointer
7. Return path or null on error/cancel

### 3.6 WPF Integration

var helper = new WindowInteropHelper(this);
string path = NativeFolderPicker.PickFolder(currentPath, helper.Handle);

---

## 4. Automated Test Suite & Win-x86 Compilation Pipeline

### 4.1 Environment Guard for Headless UI Testing

Every dialog/event handler begins with environment check:
private bool IsHeadlessMode() => Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") == "1";

When headless:
- Skip all MessageBox.Show() calls
- Skip ShowDialog() for folder/name dialogs -- return default/mock values
- Skip SaveFileDialog -- return a temp path
- Allow programmatic injection of test paths via env vars

Guard appears at entry of: BrowseBtn_Click, SuggestBtn_Click, RenameBtn_Click, UndoBtn_Click, ExportBtn_Click, ShowFolderPicker, ShowNameDialog.

### 4.2 Unit Testable Core Functions

| Function | Target Class | Testable |
|---|---|---|
| GetFormattedName | NameFormatter | Pure function |
| ConvertLatinDigits | TextNormalizer | Pure function |
| ConvertNormalizedText | TextNormalizer | Pure function |
| RemoveSourceTags | TextNormalizer | Pure function |
| DetectShowNameFromFolder | ShowNameDetector | Pure function |
| ConvertRomanNumeral | RomanNumeralConverter | Pure function |
| ResolveSeasonFolderName | SeasonResolver | Pure function |
| GetSeasonNumber | SeasonResolver | Needs FileInfo + root path param |
| GetNewName | EpisodeNameGenerator | Needs file info + config params |
| AnalyzeMissing | GapAnalyzer | Pure (takes int[]) |
| SaveMissingEpisodesFile | MissingEpisodesWriter | I/O injectable |
| ProcessFiles | FileProcessingEngine | Orchestrator, testable via interfaces |

### 4.3 Test Project Structure

EpisodeRenamer.Tests/
- UnitTests/: NameFormatterTests, TextNormalizerTests, ShowNameDetectorTests, RomanNumeralConverterTests, SeasonResolverTests, EpisodeNameGeneratorTests, GapAnalyzerTests
- IntegrationTests/: FileProcessingEngineTests (temp dirs + real files), MissingEpisodesWriterTests
- UITests/: HeadlessSmokeTests (EPISODE_RENAMER_HEADLESS=1)

### 4.4 Seven-Phase Development Milestones

PHASE 1 -- Project Scaffolding:
- Create EpisodeRenamer-WPF solution + WPF project (net8.0-windows, x86), test project
- dotnet new wpf; dotnet new xunit (if allowed for tests only)
- Verify `dotnet build` succeeds

PHASE 2 -- Pure Logic Core:
- Implement C# static classes: TextNormalizer, NameFormatter, RomanNumeralConverter, ShowNameDetector, SeasonResolver, GapAnalyzer
- Port all regex patterns verbatim from PS1
- Create unit tests with edge cases (Arabic digits, Kashida, em-dash, SxxExx, Roman numerals)

PHASE 3 -- File Engine:
- EpisodeNameGenerator (Get-NewName parity), FileDiscovery (21 video extensions), FileProcessingEngine (Process-Files parity with rename/collision/undo logic)
- UndoLogManager (Save/Load-UndoLog parity)
- SettingsManager (Save/Load-Settings parity)
- MissingEpisodesWriter + report exporter
- Integration tests on temp folders

PHASE 4 -- WPF UI Skeleton:
- MainWindow.xaml: Grid layout, GroupBoxes, all controls per section 2, RTL flow
- Dark LogConsole (RichTextBox), ModernButtonStyle resource
- Wire up events (browse, suggest, preview, rename, undo, export, drag-drop, closing)
- IsHeadlessMode guard stubbed in all handlers

PHASE 5 -- Native Folder Picker + Dialogs:
- Implement NativeFolderPicker (COM P/Invoke per section 3), NameDialog window (WPF Window)
- MessageBox wrappers honoring headless guard
- SaveFileDialog wiring for export

PHASE 6 -- Integration & Polish:
- Wire ViewModel/Engine to UI, live-preview debounce (DispatcherTimer 500ms), Set-Busy equivalent, progress bar updates
- Restore/save settings on startup/closing
- Process-Files runs on background thread with Dispatcher marshaling + busy states
- Debug drag-drop, RTL rendering, Arabic edge cases

PHASE 7 -- Build, Test & Publish:
- dotnet test; fix failures
- dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true
- Verify single EXE in bin/Release/net8.0-windows/win-x86/publish
- Optional: smoke test with EPISODE_RENAMER_HEADLESS=1

### 4.5 Key Design Decisions

1. Pure logic in separate static classes (testable, no UI deps)
2. UI = thin shell; all engine logic in service classes
3. Regex patterns ported 1:1 from PowerShell where behavior-critical
4. All string output Arabic RTL except log console (LTR)
5. All generated files UTF-8 with BOM
6. Background processing with Dispatcher for WPF thread safety
7. Headless mode via env var for CI automation
8. appRoot: AppContext.BaseDirectory (single-file EXE folder); fallback %APPDATA%\EpisodeRenamer

---
