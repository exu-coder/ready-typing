# Ready Typing — Bijoy Typing Trainer

A single-window Bengali typing tutor for the **Bijoy (SutonnyMJ legacy)** keyboard.
Bengali is painted with the bundled `SutonnyMJ.ttf`, which is embedded in the
assembly — nothing is installed into Windows and no administrator rights are
required.

![Ready Typing](Resources/app.ico)

---

## 1. Features

- **Embedded font.** `Resources/SutonnyMJ.ttf` is stored as an assembly resource
  and loaded at startup with `PrivateFontCollection.AddMemoryFont`. The bytes are
  copied to unmanaged memory with `Marshal.AllocCoTaskMem` / `Marshal.Copy`, and
  the same buffer is also registered process-privately with GDI
  (`AddFontMemResourceEx`) so the Win32 `TextBox`/`RichTextBox` can display the
  legacy glyphs without a system install.
- **Real Bijoy 52 layout.** Physical keys are translated to SutonnyMJ legacy
  code points (never Unicode Bengali). Shift legends are handled, and Caps Lock
  does not break the layout.
- **Conjunct (যুক্তবর্ণ) state machine.** `G` (hasanta) puts the buffer in a
  *pending* state. The next consonant either completes a composed conjunct
  (e.g. `J G L` → `¶` = ক্ষ) or the hasanta is kept and the consonant appended.
  `Backspace` cancels the pending hasanta instead of deleting a character.
- **Live feedback.** The target line is coloured per character: green = correct,
  red = wrong, grey = not typed yet, amber = the current character.
- **Mistake detection.** The input underline turns red as soon as the last typed
  character is wrong, and the status line reports how many characters are wrong.
- **Stats.** CPM, WPM (5 characters = 1 word), accuracy and lesson progress,
  updated in real time.
- **Virtual Bijoy keyboard** with finger-zone colours, a pulsing highlight on
  the next key to press, and a pointing-hand hint that names the finger to use.
  All shifted legends are shown in teal pills. When a capital/shifted key is
  needed, the hand points at a physical **Shift** key first, then moves to the
  target key once Shift is held.
- **Timed lessons, RapidTyping style.** Every lesson has its own countdown that
  starts on the first keystroke or **Start**. Every time budget is **2.9x** the
  original estimate. The learner must finish the line before it reaches zero;
  **Next lesson** stays disabled until the line is passed. A timeout locks the
  input until **Reset** is pressed.
- **Progress between sessions.** The last lesson the learner passes is saved in
  `%LocalAppData%\Ready Typing\progress.txt`; the next launch opens the following
  lesson. **Lesson 1** returns to the start at any time.
- **Results screen after every lesson.** Finishing a line, or letting the clock
  expire, opens the results screen inside the main window with two tabs modelled
  on RapidTyping:
  **Next step** shows the overall rating, speed, two slowdown bars and a star
  rating; **Detailed statistics** shows per-key speed (KPM) and per-key accuracy
  bars coloured by finger. **Enter** confirms the selected option; a clean run
  unlocks **Go to the next lesson**, while any remaining mistakes preselect
  **Try again**.
- **118 lessons.** Warm-up drills, everyday words, sentences, **100 Bangladesh
  captions** across ten themed groups, and a closing proposal line written for
  the developer of the app.
- **Branded UI.** The neon **BijoyTrainer** logo is embedded in the executable
  and painted in the header; the app icon is a multi-size `.ico` generated from
  the same artwork.
- **RapidTyping-inspired UI** with a gradient header, rounded cards, and a
  resizable single-window layout.
- **Consistent typography.** Every English label uses **Times New Roman** (bold
  for headings) in the app and the installer; Bengali always uses the embedded
  SutonnyMJ font.
- **One-click installer.** `ReadyTypingSetup.exe` installs the app per user,
  creates Desktop and Start-menu shortcuts, and needs no administrator rights.

---

## 2. Requirements

- Windows 10/11
- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) (or newer SDK —
  the project targets `net6.0-windows`)
- To *run* a framework-dependent build you need the **.NET 6 Desktop Runtime**.
  The published `dist/ReadyTyping.exe` and the installer are self-contained and
  need no runtime; see [Publishing](#6-publishing-a-single-exe).

No NuGet packages are used.

---

## 3. Adding the font

The repository already ships `Resources/SutonnyMJ.ttf`. If you use your own copy
of the font:

1. Copy it to `BijoyTrainer/Resources/SutonnyMJ.ttf` (keep that exact file name).
2. In the `.csproj` the file is already declared:

   ```xml
   <EmbeddedResource Include="Resources\SutonnyMJ.ttf"
                     LogicalName="BijoyTrainer.Resources.SutonnyMJ.ttf" />
   ```

3. If you rename the file, also update `FontLoader.FontResourceName` and the
   `LogicalName` together.

The font is **not** installed into `C:\Windows\Fonts`. If the resource is
missing, the application shows a friendly message box and falls back to a system
font instead of crashing.

---

## 4. Build and run

From the repository root:

```powershell
cd BijoyTrainer
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

The executable is produced in:

```
BijoyTrainer/bin/Release/net6.0-windows/BijoyTrainer.exe
```

## Running the lesson tests

From the repository root, with a .NET SDK available:

```powershell
.\test-lessons.ps1 -Dotnet "path\to\dotnet.exe"
```

The `LessonTests` project verifies the mappings, the conjunct state machine,
progress resume rules, the result model, and physical-key playback of all
118 lessons. If `dotnet` is on PATH, omit `-Dotnet`.

> The product name, window title and assembly title are **Ready Typing**. The
> assembly file name is `BijoyTrainer.exe`.

---

## 5. Using the trainer

| Action | Result |
| --- | --- |
| `Start` | Starts the lesson countdown (typing also starts it automatically). |
| `Reset` | Clears the current lesson, stats and timer so you can retry. |
| `Next lesson` | Unlocks only after the line is completed in time, then loads the next lesson. |
| `Lesson 1` | Returns to lesson 1 at any time. |
| `Shift + key` | Produces the upper (shifted) Bengali legend; the keyboard points at Shift first. |
| `G` | Hasanta — enters the pending state for a conjunct. |
| `Backspace` | Cancels a pending hasanta first, then deletes normally. |
| `Escape` | Cancels any pending hasanta. |

There is no lesson picker: lessons still run in a straight line from 1 to 118;
**Lesson 1** is a return shortcut, not a lesson picker. The time budget is
`round((8 + characters) × 2.9)`, clamped to 58–696 seconds.

**Progress**

The app writes the last **passed** lesson number to
`%LocalAppData%\Ready Typing\progress.txt`. The next launch opens the following
lesson (or the finale after all 118 are passed). A progress-file failure never
blocks the trainer; tests can redirect it with `READY_TYPING_PROGRESS_FILE`.

**Results screen**

The results screen opens by itself inside the main window when the line is
completed or the countdown reaches zero. The typing layout is replaced by the
report, so the app never opens a second window. It has two tabs:

| Tab | Contents |
| --- | --- |
| **Next step** | Overall rating bar (Could be better → Excellent), speed in WPM, slowdown by words and by keystrokes, a 1–5 star rating, and the two options. |
| **Detailed statistics** | A per-key speed chart in KPM and a per-key accuracy chart showing `percentage` and `correct:total`, with finger-coloured key pills. |

- **Go to the next lesson** is available only when the line was finished with
  no wrong characters. **Try again** is always available and is preselected
  after a mistake or a timeout.
- **Enter** (or the **Go on** button) confirms the selected option. Escape and
  every other key are ignored, so the results screen cannot close by accident.
- The **overall rating** is the final accuracy percentage; stars are awarded at
  50 / 70 / 85 / 95 percent. Per-key speed samples the typing pace at each press,
  and a slowdown is counted when more than 1.5 seconds pass before a word starts
  or between two keystrokes.
- The report stays on screen until a choice is confirmed; there is no close
  button, so a stray click cannot skip it.

**Reading the colours**

| Colour | Meaning |
| --- | --- |
| Green | Correctly typed character |
| Red | Wrong character |
| Grey | Not typed yet |
| Amber background | Current character |

**Next-key hint** — `Next key: J` means press the `J` key. While a hasanta is
pending the hint changes to the joining consonant and the keyboard highlights
it, so a conjunct such as `J → G → K` is shown one key at a time.

---

## 6. Publishing a single exe

Framework-dependent (small, needs the .NET 6 Desktop Runtime):

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

Self-contained single file (largest, runs on a clean Windows machine):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true
```

This produces a single executable of about 61 MB. The font, lessons, logo and
icon stay inside it as embedded resources.

---

## 7. Building the installer

`ReadyTypingSetup/` is a .NET Framework 4.8 WinForms installer. It embeds the
published executable as a gzip payload and unpacks it to
`%LocalAppData%\Ready Typing`, so it works without administrator rights.

```powershell
# 1. publish the app (section 6) into a temporary folder
dotnet publish ..\BijoyTrainer -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -o .\publish

# 2. gzip the payload that the setup embeds
$in = [IO.File]::OpenRead('.\publish\BijoyTrainer.exe')
$out = [IO.File]::Create('.\ReadyTypingSetup\Payload\ReadyTyping.exe.gz')
$gzip = New-Object IO.Compression.GZipStream($out, [IO.Compression.CompressionLevel]::Optimal)
$in.CopyTo($gzip)
$gzip.Dispose(); $out.Dispose(); $in.Dispose()

# 3. build the setup window
dotnet build .\ReadyTypingSetup -c Release
```

The setup shows three pages: a welcome page with the install path and shortcut
options, a live progress page, and a success page that can launch the trainer.
The default target is `%LocalAppData%\Ready Typing`; the Desktop and Start-menu
shortcuts are optional.

The installer also writes `ReadyTyping-<version>.ico` beside the executable and
points both shortcuts at that file instead of at the exe. Windows caches shortcut
icons by path, so a replaced executable at the same path can keep showing the
previous artwork; the versioned icon name guarantees the neon **BijoyTrainer**
logo appears after an upgrade. The setup then calls `SHChangeNotify` so the shell
refreshes its icon cache.

---

## 8. Project structure

```
BijoyTrainer/
├── BijoyTrainer.csproj
├── Program.cs
├── MainForm.cs               UI behaviour: keys, colours, stats, timer
├── MainForm.Designer.cs      Layout and control creation
├── ResultsView.cs            In-window lesson results: tabs, charts, Enter
├── Core/
│   ├── FontLoader.cs         Embedded font loading/disposal
│   ├── BijoyMappingTable.cs  Physical → legacy mapping + conjunct table
│   ├── BijoyInputHandler.cs  KeyPress → text-box action (UI-free)
│   ├── LigatureBuffer.cs     Hasanta / conjunct state machine
│   ├── ComparisonEngine.cs   Character-by-character comparison
│   ├── LessonManager.cs      Embedded lesson parsing + fallbacks
│   ├── LessonProgressStore.cs Last-passed-lesson persistence
│   ├── LessonReport.cs       Results model: keys, slowdowns, stars
│   └── StatsTracker.cs       CPM / WPM / accuracy
├── Controls/
│   ├── AccentButton.cs       Rounded owner-drawn button
│   ├── KeyboardPanel.cs      Virtual Bijoy keyboard
│   ├── KeyChart.cs           Per-key speed / accuracy bars
│   ├── ProgressStrip.cs      Gradient progress bar
│   ├── RoundedPanel.cs       Rounded card + shared path helper
│   ├── ScoreBar.cs           Gradient score bar with value marker
│   ├── StarRating.cs         Five-star rating
│   └── StatCard.cs           Statistic tile
├── Data/
│   └── lessons.txt           Lesson bank (legacy codes)
├── Resources/
│   ├── SutonnyMJ.ttf         Embedded legacy font
│   ├── logo.png              Embedded header logo
│   └── app.ico               Multi-size application icon
└── README.md

LessonTests/
├── LessonTests.csproj         net8.0 lesson regression test project
└── Program.cs                 Verifies mappings, resume rules and all 118 lessons

ReadyTypingSetup/
├── ReadyTypingSetup.csproj   net48 installer project
├── Program.cs                Entry point
├── InstallerForm.cs          Welcome / progress / success pages
├── InstallEngine.cs          Payload unpack, icon + shortcut creation
├── Ui.cs                     Rounded panel, button, progress, tick
├── app.manifest              Per-monitor DPI + asInvoker
├── Payload/
│   └── ReadyTyping.exe.gz    Embedded gzip payload
└── Resources/
    ├── app.ico               Setup icon + shortcut icon written at install
    └── logo.png
```

`Core/` has **no reference to `System.Windows.Forms`**; it can be unit-tested on
its own. The WinForms event wiring lives in `MainForm`.

---

## 9. Lesson file format

`Data/lessons.txt` is embedded as `BijoyTrainer.Data.lessons.txt` (a copy next
to the executable under `Data/lessons.txt` is used as a fallback).

```text
// comment
# Group heading
Optional title => target text in SutonnyMJ legacy codes
Another lesson line
```

- One lesson per line.
- `#` starts a group heading, `//` starts a comment, blank lines are ignored.
- `=>` is optional; without it a numbered title is generated.
- **The text must be SutonnyMJ legacy ANSI codes, not Unicode Bengali.**
  For example `Avwg evsjvq Mvb MvB|` renders as *আমি বাংলায় গান গাই।*

To add a lesson, append a line and rebuild (or drop an edited
`bin\...\Data\lessons.txt` next to the executable).

The 100 Bangladesh captions sit between the original drills and the closing
finale, so the proposal line is always the last lesson (118/118):

```text
The Proposal => Avgw ‡Zvgv‡K fv‡jvevmw, n¨vu ZygwB Avgvi, Avgw GB A¨v‡ci ‡W‡fjcvi Ges ‡Zvgv‡K c«†cvR KiQw, Zygw Kw Avgvi n‡e|
```

(The line contains a danda `।`; in the lesson bank it is stored as the legacy
pipe code `|`, typed with **Shift + G**.)

### The e-kar after a ra-phala

SutonnyMJ ships two e-kar glyphs: the standard `‡` (U+2021) and a compact `†`
(U+2020). The standard glyph has a wide left overhang, so when it directly
follows the zero-width ra-phala `«` the legacy font paints it detached and a
word such as **প্রপোজ** reads as **প্রোপোজ**. The lesson bank therefore stores the
compact variant in that position (`c«†cvR`), and the input handler does the same
automatically: whatever the learner types after `«`, the app substitutes `†`.
Everywhere else `‡` is kept (`‡c«g` = প্রেম).

> If you type a lesson by hand, remember this exception, or paste Unicode through
> the converter used to build the bank.

The Bangladesh pack covers ten groups: Bangladesh Pride, Nature & Beauty,
Cox's Bazar & Sea, Sundarban, Dhaka, Hills/Sajek/Bandarban, Sylhet & Tea Garden,
River & Village Life, Language & Culture, and Short Bangladesh Captions.

---

## 10. Extending the mapping table

`Core/BijoyMappingTable.cs` holds three tables:

1. **`LetterMapInternal`** — `(physical letter, shift) → legacy code`.
   Add an entry to support a new key:

   ```csharp
   [('q', true)] = 's',   // ং
   ```

2. **`SymbolMapInternal`** — `physical symbol → legacy code` (digits,
   punctuation, chandrabindu, etc.).

3. **`ConjunctMapInternal`** — `(first consonant, second consonant) → composed
   legacy code`. A composed code keeps the output compact and matches what the
   real Bijoy keyboard produces:

   ```csharp
   [('K', 'Z')] = "³",   // ক্ত
   ```

   If a pair is not listed, the state machine still produces the cluster as
   `first + hasanta + second` (e.g. `K` `&` `` ` ``), which SutonnyMJ can render
   through its own shaping. Add composed codes for the most common conjuncts to
   keep the typed text comparable to professionally typeset Bijoy text.

After editing the table, rebuild. `TryGetPhysicalKey` / `GetKeyHint` derive the
reverse map and the on-screen hints automatically, so the virtual keyboard and
the "next key" label stay in sync.

> Keep everything in legacy ANSI codes. Do not insert Unicode Bengali
> (`অ আ ক খ …`) — the bundled font would show missing glyphs.

---

## 11. Design notes and assumptions

- **Bijoy version.** The mapping follows the classic **Bijoy 52** physical
  layout (`J` = ক, `K` = ত, `L` = দ, `H` = ব, `N` = স, `M` = ম, `G` = hasanta …)
  while the stored text uses the classic **SutonnyMJ ANSI** slots (`K` = ক,
  `Z` = ত, `` ` `` = দ, `e` = ব, `m` = স, `g` = ম). The table is the translation
  layer between the two.
- **Shift detection** uses `Control.ModifierKeys` rather than
  `char.IsUpper(KeyChar)`, so Caps Lock cannot silently switch the layout.
- **Input handling** uses `KeyPress` (as required). `KeyDown` is only used for
  `Backspace`, arrows and `Escape`, which never produce a `KeyPress` character.
- **Unknown printable keys** are swallowed so the legacy font never paints a
  stray Latin-looking glyph; `Ctrl`/`Alt` shortcuts pass through.
- **IME is disabled** on the input box (`ImeMode.Disable`) because a composing
  IME would intercept the physical keys before the Bijoy mapping sees them.
- **CPM** counts *correct* characters over elapsed time; **WPM** = CPM / 5;
  **Accuracy** = correct ÷ typed (100% before anything is typed).
- **No composed conjunct exists** for some rare pairs; those are emitted as
  `consonant + & + consonant`, which the font shapes correctly.
- **DPI.** `PerMonitorV2` DPI awareness is enabled; the layout is positioned
  explicitly on resize so it stays consistent across DPI settings.
- **Typography.** All English UI text — header, body, keyboard legends, stat
  tiles, buttons and the installer — is drawn in **Times New Roman**, with
  `FontStyle.Bold` wherever the previous design used a semibold or black face.
  Bengali text never uses a system font: it is always the embedded SutonnyMJ.
- **Transparent labels.** The header caption and timer use `Color.Transparent`
  labels over a custom-painted gradient, so the lesson caption is kept clear of
  the subtitle; overlapping transparent siblings would repaint the gradient and
  clip the text beneath them.

---

## 12. Troubleshooting

| Symptom | Fix |
| --- | --- |
| Message box about the font on startup | Ensure `Resources/SutonnyMJ.ttf` exists and is an `EmbeddedResource` with `LogicalName` `BijoyTrainer.Resources.SutonnyMJ.ttf`. |
| `You must install .NET Desktop Runtime` | Install the .NET 6 Desktop Runtime, or publish self-contained (see §6). |
| Bengali shows as Latin letters | A Unicode font was used, or the embedded font failed to load; check the startup message. |
| Keys do nothing | Click the **Your Input** box first; an IME or another window may have focus. |
| Lessons look different than expected | The technique is legacy ANSI: `Avwg` = আমি. Edit `Data/lessons.txt` using ANSI codes only. |
| Setup says Ready Typing is running | Close the trainer, then run the setup again; Windows cannot replace the executable while it is open. |
