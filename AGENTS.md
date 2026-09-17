# AGENTS.md

## Build & Test

```bash
dotnet build                              # builds solution
dotnet test                               # runs all tests (232 cases, 19 classes)
dotnet test --filter "FullyQualifiedName~TextNormalizer"   # single class
dotnet test --filter "DisplayName~ConvertLatinDigits"       # single test
dotnet publish src/EpisodeRenamer.App/EpisodeRenamer.App.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o publish-x86
```

CI order (ci.yml): restore → build → test → publish. No lint/typecheck commands exist.

## Architecture

- **Single WPF project** (`src/EpisodeRenamer.App/`, net8.0-windows, C# 12) + test project (`tests/EpisodeRenamer.Tests/`, xunit 2.5.3)
- **Zero NuGet packages** in the main project. All logic is hand-rolled. The test project adds only xunit + coverlet.
- **Version**: 1.2.0 (set in `EpisodeRenamer.App.csproj`)

### Core/ (17 classes — all non-UI logic)

| Class | Type | Purpose |
|---|---|---|
| `TextNormalizer` | static | Hindi→Latin digits, Kashida strip, separator normalization, source-tag removal |
| `NameFormatter` | static | 8 style patterns + custom template (`{show}/{season}/{ep}/{ep2}/{ep3}/{epEnd}/{epEnd2}/{epEnd3}`) |
| `EpisodeNameGenerator` | static | 7-tier regex episode extraction + season resolution + show-name cleanup |
| `SeasonResolver` | static | 17-regex season detection from folder names (EN/AR digits, Roman, words) |
| `ShowNameDetector` | static | Extract show name from leaf folder name |
| `RomanNumeralConverter` | static | Subtractive Roman numeral parser |
| `FileDiscovery` | static | 23 video extensions (HashSet), recurse, `IgnoreInaccessible` |
| `FileProcessingEngine` | sealed | Main engine: preview/rename/collision/undo/ignore/subtitles/cancellation |
| `GapAnalyzer` | static | Find missing episodes, group consecutive runs |
| `MissingEpisodesWriter` | static | Write `الحلقات المفقودة.txt` (UTF-8 BOM) |
| `ReportExporter` | static | Build + write report files (UTF-8 BOM) |
| `SettingsManager` | sealed | Load/save `AppSettings` JSON (BOM), export/import |
| `UndoLogManager` | sealed | Load/save/clear/undo `List<RenameEntry>` JSON (BOM) |
| `AppSettings` | sealed DTO | Path, Show, Style, Recurse, CleanTags, CustomPattern, IgnorePatterns, RenameSubtitles, Window geometry, DarkTheme |
| `RenameEntry` | record | `Old`, `New` strings |
| `FileProcessingTypes` | records | `RunStats`, `ReportItem`, `RunResult` |
| `ErrorLogger` | static | Append to `%APPDATA%\EpisodeRenamer\error.log` |

### UI/ (7 classes)

| Class | Purpose |
|---|---|
| `NativeFolderPicker` | COM P/Invoke folder picker (IFileOpenDialog, hardcoded GUIDs) |
| `DialogService` | MessageBox/SaveFile/OpenFile with headless guards |
| `ShowNameDialog` | Modal 420x160 WPF window for show name input |
| `ThemeManager` | Light/dark palettes (15 resource keys), console is always black+green |
| `ToastWindow` | Borderless topmost 5s auto-close toast notification |
| `PreviewRow` | DataGrid row model (Status, OldName, Editable NewName) |
| `WindowBoundsHelper` | Validate + persist window geometry |

### MainWindow

- Thin shell: all engine logic lives in `Core/`
- `ProcessFiles()` runs `_engine.Run(...)` on `Task.Run`, marshals progress via `Dispatcher.BeginInvoke`
- Builds `nameOverrides` from editable DataGrid rows for per-file name customization
- `ResolveAppRoot()` = `AppContext.BaseDirectory` fallback `%APPDATA%\EpisodeRenamer`
- Keyboard shortcuts: Ctrl+B (browse), Ctrl+E (preview), Ctrl+R (rename), Ctrl+Z (undo)

## Features Beyond the Original PowerShell Prototype

These are implemented in C# but were NOT in the original PS1 design:

- **9th naming style**: قالب مخصص (Custom template) with `{show}/{season}/{ep}/{ep2}/{ep3}/{epEnd}/{epEnd2}/{epEnd3}` tokens
- **Dark mode toggle** (`ThemeManager`, persisted in settings)
- **Stop/cancel button** (`CancellationToken` wired to engine)
- **Subtitle renaming** (`.srt/.ass/.ssa/.sub/.vtt` matched to episode files)
- **Ignore patterns** (substring matching, reason: `يطابق نمط الاستبعاد`)
- **Editable preview grid** (double-click to override individual names)
- **Settings export/import** (JSON files via `SaveTo`/`LoadFrom`)
- **Window geometry persistence** (position, size, maximized state)
- **Pending-undo restore prompt** on startup (`PromptPendingUndo`)
- **Toast notification** after successful rename
- **Global exception logging** (`ErrorLogger` + `App.xaml.cs` handlers)
- **`InternalsVisibleTo("EpisodeRenamer.Tests")`** for UI test access

## Testing

- **232 test cases** across 19 classes
- **Tests are NOT parallelized** (`xUnitConfig.cs` sets `DisableTestParallelization = true`) — integration tests use temp directories and must not collide
- Tests create real temp dirs via `TestHelpers.NewTempDir()` (`ER` + 12 random chars). They clean up, but a crash can leave orphans in `%TEMP%\ER*`
- **Headless mode**: set env var `EPISODE_RENAMER_HEADLESS=1` to skip MessageBoxes and dialogs
- **Test layout**: flat + `UI/` subfolder (NOT the `UnitTests/IntegrationTests/UITests` folders from the original plan)

### Running focused tests

```bash
dotnet test --filter "FullyQualifiedName~SeasonResolver"       # all season tests
dotnet test --filter "FullyQualifiedName~FileProcessingEngine" # engine integration
dotnet test --filter "FullyQualifiedName~MainWindow"           # UI headless tests
dotnet test --filter "DisplayName~ConvertLatinDigits"           # single theory case
```

## Conventions

- All user-facing strings are **Arabic RTL**. The log console is LTR (black bg, green text `#32CD32`)
- Generated text files use **UTF-8 with BOM** (`new UTF8Encoding(true)`)
- Output is `win-x86` only. The `.csproj` does not specify `RuntimeIdentifier`; publish commands must include `-r win-x86`
- App icon lives at `assets/app.ico`
- Settings and undo files (`EpisodeRenamer.settings.json`, `EpisodeRenamer.undo.json`) are gitignored; they live alongside the EXE at runtime
- Regex patterns in `Core/` are ported from a PowerShell prototype. Some patterns have minor deviations (see below)

## Gotchas

- `FileProcessingEngine.Run()` accepts many parameters (style, showName, cleanTags, ignorePatterns, renameSubtitles, customPattern, nameOverrides, cancellationToken). If adding new options, pass them through the engine — don't bypass it from the UI.
- `MainWindow.xaml.cs` contains `DoEvents()`-style processing via `Task.Run` + `Dispatcher.BeginInvoke`. Long operations run inline with progress marshaling; adding blocking calls on the UI thread will freeze the app.
- `NativeFolderPicker` uses COM P/Invoke with hardcoded GUIDs. Do not refactor to use `Microsoft.Win32.OpenFolderDialog` — the COM approach is intentional for Windows 7+ compat.
- **SeasonResolver regex #13** (`Nth Season`): the C# version `([0-9]+)(?:st|nd|rd|th)Season` does NOT allow a separator between ordinal and "Season" (the original PS1 spec did). This path is also untested.
- **ShowNameDetector** has a dead `RemovePatterns` field (10-tag alternation) that is never called. Active logic uses only bracket/season stripping. Do not rely on `RemovePatterns` for tag removal — use `TextNormalizer.RemoveSourceTags` instead.
- **Custom template names** bypass the `{show}-{pattern}` convention entirely — the template text is used as-is. If you add `{show}` in the template, it gets replaced; otherwise no show-name prefix is added.

## Agent Principles

### Think First, Code Second

- Before writing code, understand the problem: read the relevant Core/ class, its tests, and how `FileProcessingEngine.Run()` is invoked from `MainWindow.xaml.cs`
- Question the requirements: if a requested behavior contradicts the engine's contract or a regex pattern, raise it to the user with a concrete alternative — do not silently "fix" it
- Prefer the simplest solution that satisfies the requirement. Complexity is only justified when proven necessary
- When a bug appears, don't patch symptoms: trace the root cause, then fix it where the state is introduced

### Software Engineering Principles

- **Single Responsibility**: a Core/ class does one thing well. Don't grow `FileProcessingEngine` into a god-object — extract cohesive helpers like `GapAnalyzer` or `ReportExporter` already are
- **Layered Architecture**: `Core/` never imports `System.Windows`; `UI/` never contains engine logic — if you feel the urge to cross the boundary, the design is wrong, restructure instead
- **Extend, Don't Modify**: new naming styles, file types, or ignore-methods should be additive (a new branch, a new regex tier, a new parameter) — preserve existing behavior unless the user explicitly approves a behavior change
- **Fail Predictably**: public APIs return nullable/typed results (`int?`, records), and never throw for expected conditions (missing files, missing episodes). Keep that contract
- **Defensive I/O**: all file/settings/undo operations are error-swallowed by design (SettingsManager, UndoLogManager). Follow that pattern for new I/O — don't let a corrupt JSON crash the app

### Creativity Within the Architecture

- The structure exists so you don't reinvent it. Adding a feature = identify the right class, add pure logic, wire it through the engine, expose it in `MainWindow`, test it
- The 9 naming styles are a switch/`StartsWith` mapping in `NameFormatter` — a new style is a new branch there
- Episode extraction is a tiered regex pipeline in `EpisodeNameGenerator` — a new pattern slots in as a new tier; keep precedence explicit
- Season detection is an ordered 17-pattern list in `SeasonResolver` — a new pattern appends with a clear priority
- If the architecture constrains a good idea, propose the refactor to the user with the trade-offs rather than contorting the feature into the wrong layer

### Code Quality (Professional Standard)

- `dotnet build` and `dotnet test` must pass before you consider work done
- New feature → new tests. Bug fix → a regression test that would have caught it. This project has 232 tests across 19 classes and the count is expected to grow
- Keep parallelism: bug fixes and new features stay as additive and collision-free as possible — never rely on shared temp paths, shared state, or file-name assumptions
- Arabic user-facing strings use Unicode escapes in C# source (`\u0627\u0644\u062d\u0644\u0642\u0629`), not raw Arabic text — avoids encoding corruption across toolchains
- Follow the code's existing style: naming, indentation, param ordering, XML doc comments on public methods
- No dead code: if `ShowNameDetector.RemovePatterns` is unused, leave it (documented quirk) — but never introduce new dead code

### Guardrails (Listen Before You Leap)

These are project decisions with deep history. Propose a change, then let the user approve:

- Regex patterns in `SeasonResolver`/`EpisodeNameGenerator` are ported from a PowerShell prototype — changing them can silently break Arabic/Roman numeral edge cases
- `NativeFolderPicker` COM interop is intentional (Windows 7+ compat) — a change requires user approval
- `MainWindow.xaml` layout is deliberately compact — new controls need a place that earns its space
- The main project has ZERO NuGet packages by design — adding one is a decision for the user
- `xUnitConfig.cs` disables test parallelization deliberately — don't revert it
- The console is a plain `TextBox` (black/`#32CD32`) by design — performance and terminal-style

### Never (Absolute)

- Commit secrets, API keys, or credentials
- Delete or rename existing test classes or test methods without user approval
- Block the UI thread in `MainWindow.xaml.cs` — always `Task.Run` + `Dispatcher.BeginInvoke`

### Before Committing

1. `dotnet build` — zero errors
2. `dotnet test` — all 232 tests pass
3. Review the diff for dead code, debug leftovers, or unrelated changes
4. New files follow naming conventions (PascalCase, no abbreviations)
5. Public methods have XML doc comments if they're new
=====================================================


 # AGENTS.md

 # EpisodeRenamer — Agent Constitution & Project Guide

 This document is the authoritative operating guide for AI coding agents working in this repository.

 The agent must read and follow this file before making changes.

---

 # 1\. Project Overview

 EpisodeRenamer is a Windows WPF application for detecting, normalizing, previewing, renaming, and undoing episode filenames.

 The application is implemented in:

 - C#
- .NET 8
- WPF
- C# 12
- xUnit 2.5.3 for tests

 Architecture:

```
 EpisodeRenamer-WPF/
│
├── EpisodeRenamer.sln                               
├── .gitignore                                       
├── Directory.Build.props                          
├── README.md                                      
│
├── src/
│   └── EpisodeRenamer.App/
│       │
│       ├── Core/                                    
│       │   ├── AppSettings.cs
│       │   ├── EpisodeNameGenerator.cs
│       │   ├── ErrorLogger.cs
│       │   ├── FileDiscovery.cs
│       │   ├── FileProcessingEngine.cs
│       │   ├── FileProcessingTypes.cs
│       │   ├── GapAnalyzer.cs
│       │   ├── MissingEpisodesWriter.cs
│       │   ├── NameFormatter.cs
│       │   ├── RenameEntry.cs
│       │   ├── ReportExporter.cs
│       │   ├── RomanNumeralConverter.cs
│       │   ├── SeasonResolver.cs
│       │   ├── SettingsManager.cs
│       │   ├── ShowNameDetector.cs
│       │   ├── TextNormalizer.cs
│       │   └── UndoLogManager.cs
│       │
│       ├── UI/                                      
│       │   ├── DialogService.cs                     
│       │   ├── NativeFolderPicker.cs                
│       │   ├── PreviewRow.cs                        
│       │   ├── ShowNameDialog.cs                    
│       │   ├── ThemeManager.cs                      
│       │   ├── ToastWindow.cs                       
│       │   ├── WindowBoundsHelper.cs                

│       │
│       ├── App.xaml                                 
│       ├── App.xaml.cs        
│       ├── MainWindow.xaml                     
│       ├── MainWindow.xaml.cs                                      
│       ├── AssemblyInfo.cs                          
│       └── EpisodeRenamer.App.csproj                
│
└── tests/
    └── EpisodeRenamer.Tests/
        ├── EpisodeRenamer.Tests.csproj              
        ├── Core/
        │   ├── EpisodeNameGeneratorTests.cs         
        │   ├── GapAnalyzerTests.cs                 
        │   ├── NameFormatterTests.cs                
        │   ├── SeasonResolverTests.cs              
        │   └── TextNormalizerTests.cs             
        └── TestData/                                
```

 The main application intentionally has **zero NuGet packages**.

 The test project uses only:

 - xunit
- coverlet

 Current application version:

```
1.2.0
```

---

 # 2\. Source of Truth

 When determining intended behavior, use this priority:

 1. Explicit user request
2. Existing repository behavior
3. Existing tests
4. This `AGENTS.md`
5. Existing architecture and conventions
6. General coding preferences

 If two sources conflict, do not silently choose one.

 Prefer repository evidence over assumptions.

 Never invent behavior merely because it seems convenient or theoretically better.

---

 # 3\. Core Agent Principles

 The agent must:

 - Treat this as an existing production codebase.
- Preserve existing behavior unless explicitly asked to change it.
- Make the smallest safe change that solves the requested problem.
- Read relevant code before modifying it.
- Search for callers before changing APIs.
- Read relevant tests before changing behavior.
- Add regression tests for bug fixes.
- Add tests for new functionality.
- Preserve compatibility-sensitive code.
- Avoid unrelated refactoring.
- Avoid speculative features.
- Verify changes with real build/test commands.
- Report verification honestly.

 The agent must never claim that something was tested or verified unless the corresponding command was actually executed.

---

 # 4\. Repository Exploration Rules

 Before modifying code, the agent should:

 1. Inspect the relevant files.
2. Search all references to affected classes/methods/properties.
3. Read existing tests.
4. Inspect serialization when changing persisted models.
5. Inspect UI bindings when changing UI properties.
6. Inspect callers before changing method signatures.
7. Inspect undo behavior when changing rename logic.
8. Inspect collision handling when changing generated names.
9. Check related documentation and `AGENTS.md` rules.

 Do not modify code based solely on a filename or a small snippet.

---

 # 5\. Architecture

 ## Core/

 `Core/` contains all non-UI application logic.

 It must remain independent of WPF.

 Never add:

```
using System.Windows;
```

 or other UI dependencies to Core unless explicitly required and approved.

 Core code must remain testable without displaying dialogs or requiring the WPF UI.

 ## UI/

 `UI/` contains presentation-related code.

 UI code may call Core.

 UI code must not duplicate business logic that belongs in Core.

 Do not move engine behavior into `MainWindow.xaml.cs`.

 ## MainWindow

 `MainWindow` is a thin application shell.

 Its responsibilities include:

 - Collecting user input
- Managing UI state
- Building preview overrides
- Calling Core services
- Displaying progress/results
- Dispatching UI updates

 Business logic belongs in `Core/`.

---

 # 6\. Core Classes

 | Class | Type | Purpose |
| --- | --- | --- |
| `TextNormalizer` | static | Hindi→Latin digits, Kashida stripping, separator normalization, source-tag removal |
| `NameFormatter` | static | 8 naming styles + custom template |
| `EpisodeNameGenerator` | static | 7-tier regex episode extraction + season resolution + show cleanup |
| `SeasonResolver` | static | 17-regex season detection |
| `ShowNameDetector` | static | Show name extraction |
| `RomanNumeralConverter` | static | Roman numeral parser |
| `FileDiscovery` | static | Video discovery and recursive traversal |
| `FileProcessingEngine` | sealed | Main preview/rename/collision/undo/ignore/subtitle/cancellation engine |
| `GapAnalyzer` | static | Missing episode detection |
| `MissingEpisodesWriter` | static | Writes `الحلقات المفقودة.txt` |
| `ReportExporter` | static | Builds and writes report files |
| `SettingsManager` | sealed | Settings load/save/import/export |
| `UndoLogManager` | sealed | Undo persistence and undo operations |
| `AppSettings` | sealed DTO | Application settings |
| `RenameEntry` | record | Old/new rename paths |
| `FileProcessingTypes` | records | `RunStats`, `ReportItem`, `RunResult` |
| `ErrorLogger` | static | Application error logging |

---

 # 7\. UI Classes

 | Class | Purpose |
| --- | --- |
| `NativeFolderPicker` | COM/PInvoke Windows folder picker |
| `DialogService` | MessageBox/file dialogs with headless guards |
| `ShowNameDialog` | Show-name modal dialog |
| `ThemeManager` | Light/dark themes |
| `ToastWindow` | Temporary toast notification |
| `PreviewRow` | Editable preview DataGrid row |
| `WindowBoundsHelper` | Window geometry validation/persistence |

---

 # 8\. Existing Features

 The current implementation includes:

 - 9 naming styles
- Custom naming templates
- `{show}`
- `{season}`
- `{ep}`
- `{ep2}`
- `{ep3}`
- `{epEnd}`
- `{epEnd2}`
- `{epEnd3}`
- Multi-episode filenames (`55+56`, `55-56`, `55E56`, ...) rendered with the same style, joined by `-`
- Season grouping in the preview grid (collapsible headers) with numeric ordering within a season
- Per-season missing-episode detection
- Dark mode
- Cancellation
- Subtitle renaming
- Ignore patterns
- Editable preview grid
- Settings export/import
- Window geometry persistence
- Pending undo restoration
- Toast notifications
- Global exception logging
- Headless UI testing
- Per-file name overrides

 Do not accidentally remove any of these when modifying unrelated code.

---

 # 9\. Naming System

 The custom template is special.

 Custom templates bypass the normal:

```
{show}-{pattern}
```

 convention.

 The template is used as provided.

 If `{show}` exists, it is replaced.

 If `{show}` does not exist, a show-name prefix must not be automatically added.

 Do not change this behavior without an explicit requirement.

---

 # 10\. File Processing Engine

 `FileProcessingEngine` is the central rename pipeline.

 Any new rename-related option must flow through the engine.

 Do not implement special rename behavior directly in the UI.

 The normal conceptual pipeline is:

```
Discover
   ↓
Normalize
   ↓
Detect show/season/episode
   ↓
Apply ignore rules
   ↓
Generate name
   ↓
Apply overrides
   ↓
Preview/validate
   ↓
Detect collisions
   ↓
Rename
   ↓
Record undo
   ↓
Process subtitles
   ↓
Report
```

 Actual implementation details may differ, but the agent must preserve these responsibilities.

---

 # 11\. Rename Safety

 This application modifies user files.

 File operations are therefore safety-critical.

 Never:

 - Delete a file as part of normal renaming.
- Overwrite an existing file unintentionally.
- Rename files outside the selected scope.
- Ignore collision detection.
- Bypass the engine.
- Lose undo information.
- Rename unrelated subtitle files.
- Continue performing unnecessary work after cancellation.

 A rename must be recoverable whenever the existing architecture supports recovery.

 Preview and actual rename must use the same naming logic.

---

 # 12\. Undo Safety

 Undo is a safety mechanism.

 When changing rename behavior:

 - Record successful renames.
- Do not record operations that did not happen.
- Preserve original paths.
- Preserve resulting paths.
- Preserve pending undo information.
- Do not silently clear undo history.

 Any change affecting undo behavior requires tests.

---

 # 13\. Collision Safety

 Never overwrite an existing file unintentionally.

 Before renaming:

 - Validate the target path.
- Detect collisions.
- Preserve the existing collision behavior.
- Ensure preview reflects the same collision rules used by actual rename.

 If collision behavior changes, add regression tests.

---

 # 14\. Cancellation

 Cancellation is a first-class feature.

 Long-running operations must:

 - Observe `CancellationToken`.
- Stop promptly.
- Avoid unnecessary work after cancellation.
- Leave the application in a recoverable state.
- Avoid reporting cancelled work as successful.

 Do not remove or weaken cancellation support.

 Cancellation should not be treated as an ordinary application error.

---

 # 15\. UI Thread Rules

 Never block the WPF UI thread with expensive operations.

 Long-running operations should use the established:

```
Task.Run(...)
```

 pattern and marshal UI updates through:

```
Dispatcher.BeginInvoke
```

 Never introduce unnecessary:

```
.Wait()
.Result
Thread.Sleep(...)
```

 or synchronous large filesystem operations in UI event handlers.

 A frozen UI is considered a regression.

---

 # 16\. MainWindow Processing

 `ProcessFiles()` runs:

```
_engine.Run(...)
```

 through background processing.

 Progress is marshalled through the Dispatcher.

 `nameOverrides` are constructed from editable DataGrid rows.

 Do not bypass this architecture.

 Keyboard shortcuts currently include:

```
Ctrl+B = Browse
Ctrl+E = Preview
Ctrl+R = Rename
Ctrl+Z = Undo
```

---

 # 17\. Compatibility Rules

 ## NativeFolderPicker

 `NativeFolderPicker` intentionally uses COM/PInvoke with hardcoded GUIDs.

 Do not replace it with:

```
Microsoft.Win32.OpenFolderDialog
```

 The COM implementation exists for Windows 7+ compatibility.

 Do not refactor this component casually.

 Changes require explicit user approval.

---

 # 18\. Regex Protection

 The regex logic is compatibility-sensitive.

 This applies especially to:

 - `SeasonResolver`
- `EpisodeNameGenerator`

 These regexes originate from a PowerShell prototype.

 Before modifying a regex:

 1. Understand its original purpose.
2. Search all tests.
3. Inspect neighboring patterns.
4. Check Arabic digits.
5. Check Latin digits.
6. Check Roman numerals.
7. Check season words.
8. Check ordinal forms.
9. Add regression tests.
10. Run focused tests.
11. Run the full test suite.

 Never simplify a regex simply because it looks complicated.

---

 # 19\. Known Regex Gotcha

 `SeasonResolver` regex #13:

```
Nth Season
```

 currently uses:

```
([0-9]+)(?:st|nd|rd|th)Season
```

 The current C# implementation does **not** allow a separator between the ordinal and `Season`.

 The original PowerShell specification did.

 This path is also currently untested.

 Do not silently "fix" this difference.

 If changing it, obtain user approval and add appropriate tests.

---

 # 20\. ShowNameDetector Gotcha

 `ShowNameDetector` contains a dead:

```
RemovePatterns
```

 field.

 It is not currently used.

 Active logic uses bracket/season stripping.

 Do not rely on `RemovePatterns` for source-tag removal.

 Do not call:

```
TextNormalizer.RemoveSourceTags
```

 from `ShowNameDetector`.

 The dead field is intentionally preserved.

---

 # 21\. Text Normalization

 `TextNormalizer` handles:

 - Hindi → Latin digit conversion
- Kashida stripping
- Separator normalization
- Source-tag removal

 Do not move its responsibilities into unrelated classes.

 Be careful when changing normalization because it can affect:

 - show detection
- episode detection
- season detection
- generated names
- collision behavior

 Any behavior change requires regression coverage.

---

 # 22\. Arabic Localization

 User-facing strings are Arabic RTL.

 C# source must use Unicode escapes for Arabic strings.

 Example:

```
"\u0627\u0644\u062d\u0644\u0642\u0629"
```

 Do not introduce raw Arabic text literals into C# source.

 The console is intentionally:

```
Background: black
Foreground: #32CD32
```

 Do not change these colors.

 The console is LTR even though the rest of the user-facing application is RTL.

---

 # 23\. Generated Text Files

 Generated text files must use UTF-8 with BOM:

```
new UTF8Encoding(true)
```

 This applies to reports and:

```
الحلقات المفقودة.txt
```

 Do not change encoding casually.

---

 # 24\. Subtitle Renaming

 Supported subtitle extensions:

```
.srt
.ass
.ssa
.sub
.vtt
```

 Subtitle renaming is coupled to episode file renaming.

 When modifying episode naming or matching logic, verify subtitle behavior.

 Never rename unrelated subtitle files.

---

 # 25\. Ignore Patterns

 Ignore patterns currently use substring matching.

 The ignore reason is:

```
يطابق نمط الاستبعاد
```

 Do not silently change substring matching to another matching strategy.

 If changing matching behavior, add tests.

---

 # 26\. Persistent Settings

 Settings are stored in:

```
EpisodeRenamer.settings.json
```

 Undo data is stored in:

```
EpisodeRenamer.undo.json
```

 These files live alongside the EXE at runtime and are gitignored.

 When modifying `AppSettings`:

 - Preserve backward compatibility.
- Provide safe defaults for new properties.
- Preserve existing property names.
- Test serialization/deserialization.
- Verify import/export behavior.

 Do not casually rename persisted properties.

---

 # 27\. Application Root

 `ResolveAppRoot()` uses:

```
AppContext.BaseDirectory
```

 with:

```
%APPDATA%\EpisodeRenamer
```

 as fallback.

 Preserve this behavior unless explicitly asked to change it.

---

 # 28\. Error Logging

 `ErrorLogger` writes to:

```
%APPDATA%\EpisodeRenamer\error.log
```

 Do not silently swallow unexpected exceptions.

 Avoid empty:

```
catch
{
}
```

 blocks.

 Preserve existing global exception logging.

 Do not expose sensitive internal exception details unnecessarily to users.

---

 # 29\. Testing

 Current test suite:

```
232 test cases
19 classes
```

 Tests are intentionally not parallelized.

 `xUnitConfig.cs` disables test parallelization because integration tests use temporary directories.

 Do not modify this configuration to make tests pass.

---

 # 30\. Test Environment

 Tests use:

```
TestHelpers.NewTempDir()
```

 Temporary directories use:

```
ER + 12 random characters
```

 A crashed test process may leave orphan directories in:

```
%TEMP%\ER*
```

 This is expected.

---

 # 31\. Headless Mode

 For UI tests, use:

```
EPISODE_RENAMER_HEADLESS=1
```

 This prevents MessageBoxes and dialogs from blocking tests.

 Preserve headless guards when modifying UI code.

---

 # 32\. Test Commands

 Full build:

```
dotnet build
```

 Full tests:

```
dotnet test
```

 Season tests:

```
dotnet test --filter "FullyQualifiedName~SeasonResolver"
```

 Episode extraction tests:

```
dotnet test --filter "FullyQualifiedName~EpisodeNameGenerator"
```

 Engine integration tests:

```
dotnet test --filter "FullyQualifiedName~FileProcessingEngine"
```

 MainWindow tests:

```
dotnet test --filter "FullyQualifiedName~MainWindow"
```

 Single test example:

```
dotnet test --filter "DisplayName~ConvertLatinDigits"
```

---

 # 33\. Build and Test Gate

 Every completed code change must pass:

```
dotnet build
dotnet test
```

 before it is considered complete.

 For relevant changes, run focused tests as well.

 If a command fails:

 - Investigate the actual failure.
- Fix the root cause.
- Do not hide the failure.
- Do not delete/weaken tests.
- Do not claim success.

 If the environment prevents execution, state that clearly.

---

 # 34\. No False Verification

 The agent must never say:

```
Build passed
All tests passed
Everything is verified
No warnings exist
```

 unless the corresponding command was actually executed and its output/result observed.

 Never fabricate command output.

 Never fabricate test counts.

---

 # 35\. Test Rules

 For bug fixes:

```
Bug
 ↓
Regression test
 ↓
Implementation fix
 ↓
Focused test
 ↓
Full test suite
```

 For features:

```
Requirement
 ↓
Test
 ↓
Implementation
 ↓
Focused test
 ↓
Full test suite
```

 Never delete an existing test to make the suite pass.

 Never weaken an assertion without a documented reason.

 Never rename tests merely to hide their original intent.

---

 # 36\. Dependencies

 The main project has:

```
ZERO NuGet packages
```

 Do not add a NuGet package without explicit user approval.

 Prefer existing .NET/WPF functionality.

 Do not introduce third-party libraries for convenience.

---

 # 37\. Public API Protection

 Before changing any public API:

 - Search all callers.
- Search tests.
- Check reflection/serialization usage.
- Check UI dependencies.
- Check documentation.

 Do not change:

```
FileProcessingEngine.Run(...)
```

 public signature without explicit approval.

 When possible, implement new behavior internally without breaking the public API.

---

 # 38\. Code Quality

 Follow existing:

 - naming conventions
- indentation
- file organization
- nullability conventions
- access modifiers
- exception handling patterns
- testing style

 Use PascalCase for types and public members.

 Avoid unexplained abbreviations.

 New public methods should have XML documentation.

 Do not introduce architecture solely to satisfy stylistic preferences.

---

 # 39\. Minimal Diff Rule

 Prefer:

```
One problem → One focused change
```

 Do not combine an unrelated refactor with a bug fix.

 Avoid:

 - mass formatting
- unnecessary file moves
- unrelated renames
- changing line endings unnecessarily
- replacing working APIs
- rewriting entire classes for small changes

 If refactoring is genuinely necessary:

 - Explain why.
- Keep the scope narrow.
- Preserve behavior.
- Add tests.
- Run the full suite.

---

 # 40\. No Speculative Features

 Do not implement unrequested features.

 Do not add:

 - telemetry
- analytics
- cloud synchronization
- auto-update
- external APIs
- background services
- databases
- plugin systems
- new package dependencies

 unless explicitly requested.

---

 # 41\. Git Safety

 The agent must not perform destructive Git operations without explicit approval.

 Never run commands such as:

```
git reset --hard
git clean -fd
git checkout -- .
```

 unless explicitly instructed.

 Do not:

 - rewrite Git history
- amend commits without permission
- force-push
- delete branches
- create commits unless requested

---

 # 42\. Secrets and Privacy

 Never commit:

 - API keys
- passwords
- tokens
- credentials
- private certificates
- personal information
- machine-specific secrets

 If a secret is discovered:

 - Do not reproduce it in the response.
- Do not copy it elsewhere.
- Do not commit it.
- Warn the user that sensitive data appears to be exposed.

---

 # 43\. Runtime and Generated Files

 Do not commit:

```
EpisodeRenamer.settings.json
EpisodeRenamer.undo.json
error.log
```

 Do not commit build artifacts unless the repository explicitly requires them:

```
bin/
obj/
publish-x86/
```

---

 # 44\. Publish

 The application publishes as:

```
win-x86
```

 The `.csproj` intentionally does not specify a `RuntimeIdentifier`.

 Publish using:

```
dotnet publish src/EpisodeRenamer.App/EpisodeRenamer.App.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o publish-x86
```

 Do not change the runtime architecture without explicit instruction.

---

 # 45\. CI

 Current CI order:

```
restore
  ↓
build
  ↓
test
  ↓
publish
```

 No lint/typecheck commands currently exist.

 Do not invent additional required CI stages without a specific reason.

---

 # 46\. App Icon

 Application icon:

```
assets/app.ico
```

 Do not remove or replace it as part of unrelated work.

---

 # 47\. Existing Gotchas

 ## FileProcessingEngine

 `Run()` accepts many parameters, including:

```
style
showName
cleanTags
ignorePatterns
renameSubtitles
customPattern
nameOverrides
cancellationToken
```

 New options must flow through the engine.

 Do not bypass it from the UI.

 ## MainWindow

 Long-running processing uses:

```
Task.Run + Dispatcher.BeginInvoke
```

 Do not add blocking work to the UI thread.

 ## NativeFolderPicker

 COM/PInvoke is intentional.

 Do not replace it with `OpenFolderDialog`.

 ## SeasonResolver

 Regex #13 has the known ordinal separator limitation.

 Do not silently change it.

 ## ShowNameDetector

 `RemovePatterns` is dead code.

 Do not activate it accidentally.

 Do not use `TextNormalizer.RemoveSourceTags` from `ShowNameDetector`.

 ## Custom Template

 Custom templates are used as-is.

 Do not automatically prepend show names.

---

 # 48\. Permission Matrix

 ## Always Allowed

 The agent may perform these without asking:

 - Read files
- Search files
- List directories
- Inspect tests
- Run `dotnet build`
- Run `dotnet test`
- Run focused tests
- Add/update tests
- Fix implementation bugs
- Update relevant documentation
- Make small compatible code changes

 ## Ask First

 The agent must ask before:

 - Adding NuGet packages
- Changing public API signatures
- Changing `FileProcessingEngine.Run()` signature
- Modifying `SeasonResolver` regexes
- Modifying `EpisodeNameGenerator` regexes
- Changing `NativeFolderPicker`
- Adding major UI controls to `MainWindow.xaml`
- Changing test infrastructure
- Changing test parallelization
- Changing runtime architecture
- Removing existing functionality
- Performing destructive filesystem operations
- Performing destructive Git operations

---

 # 49\. Protected Invariants

 The following are hard invariants unless the user explicitly requests otherwise:

```
Core must remain UI-independent.

MainWindow must remain a thin shell.

FileProcessingEngine remains the central processing pipeline.

Preview and actual rename use the same naming logic.

Rename collisions must be detected.

Undo information must remain recoverable.

Cancellation must remain supported.

Tests remain non-parallelized.

Main project remains dependency-free.

NativeFolderPicker remains COM/PInvoke based.

Arabic user-facing C# strings use Unicode escapes.

Generated text files remain UTF-8 BOM.

Console remains black + #32CD32.

win-x86 remains the publish target.
```

 Breaking one of these requires explicit user intent and appropriate tests/documentation.

---

 # 50\. Change Impact Analysis

 Before changing a shared Core component, consider its impact on:

```
TextNormalizer
    ↓
ShowNameDetector
    ↓
SeasonResolver
    ↓
EpisodeNameGenerator
    ↓
NameFormatter
    ↓
FileProcessingEngine
    ↓
Preview
    ↓
Rename
    ↓
Undo
    ↓
Reports
```

 A change near the beginning of this chain can affect many downstream behaviors.

 Run broader tests accordingly.

---

 # 51\. Regression Prevention

 When fixing a bug, reproduce the old failure in a test whenever practical.

 The regression test should fail under the old implementation and pass under the new implementation.

 Do not write a test that merely exercises the new code without proving the original bug is prevented.

---

 # 52\. Error Recovery

 When an operation partially fails:

 - Preserve successful rename information.
- Preserve undo data.
- Report failed files.
- Do not pretend the entire operation succeeded.
- Do not discard recovery information.
- Respect cancellation state.

 A partial failure must remain understandable and recoverable.

---

 # 53\. User Data Protection

 Assume all files being processed belong to the user and may be valuable.

 Therefore:

 - Never delete files unnecessarily.
- Never overwrite files silently.
- Never modify files outside the requested scope.
- Never transmit file names or contents externally unless explicitly requested.
- Never add telemetry or external reporting implicitly.

---

 # 54\. Agent Communication

 When asking the user a question, ask only when the missing information genuinely affects implementation.

 Do not ask for information that can be determined from the repository.

 When there are multiple safe interpretations, prefer the one consistent with existing code/tests.

 When an operation is potentially destructive or architectural, ask before proceeding.

---

 # 55\. Completion Report

 After completing a task, report:

 1. What changed.
2. Which files changed.
3. Tests added or updated.
4. Verification commands executed.
5. Build result.
6. Test result.
7. Any warnings.
8. Any remaining limitations.

 Example:

```
Changed:
- Core/Example.cs
- tests/ExampleTests.cs

Tests:
- Added regression coverage for X.

Verification:
- dotnet build: PASS
- dotnet test: PASS

Warnings:
- None observed.

Remaining limitations:
- None.
```

 Never claim more than was actually verified.

---

 # 56\. Final Verification Checklist

 Before declaring a task complete:

```
[ ] Relevant implementation was read
[ ] Relevant tests were read
[ ] Important callers were searched
[ ] Change is minimal and targeted
[ ] New behavior has tests
[ ] Bug fixes have regression tests
[ ] dotnet build was executed
[ ] dotnet build succeeded
[ ] dotnet test was executed
[ ] dotnet test succeeded
[ ] Relevant focused tests were executed
[ ] No known warnings were introduced
[ ] No architecture invariant was violated
[ ] No secrets were introduced
[ ] No destructive operation was performed
[ ] No unrelated files were changed
[ ] Documentation was updated if necessary
[ ] Final response accurately reports verification
```

---

 # 57\. Golden Rules

 These rules have the highest practical importance:

 > **Preserve existing behavior unless the user explicitly requests a change.**

 > **Make the smallest safe change that solves the problem.**

 > **Never bypass the established architecture.**

 > **Never sacrifice data safety for convenience.**

 > **Never weaken tests to hide a problem.**

 > **Never claim verification that did not happen.**

 > **When changing behavior, add a test that protects it.**

 > **When changing sensitive code, inspect its callers and dependencies first.**

 > **When repository evidence contradicts an assumption, trust the repository.**

 > **When an operation is destructive, irreversible, or architectural, ask first.**

 > **Correctness and user-data safety are more important than speed.**

---

 # 58\. Absolute Rule

 If an instruction would cause the agent to:

 - destroy user data,
- bypass collision protection,
- bypass undo,
- block the UI,
- hide a test failure,
- introduce an unapproved dependency,
- break a protected compatibility requirement,
- expose a secret,
- or silently change established behavior,
Do not test using dummy folders and files; test using this real folder: `\\PC57\00 وادي الذئاب` or `D:\00 وادي الذئاب`.
 the agent must stop and choose the safer implementation or ask the user when approval is required.

 The agent's goal is not merely to make the requested code compile.

 The goal is:

```
Understand
→ Change safely
→ Test
→ Verify
→ Preserve
→ Report honestly
```

 