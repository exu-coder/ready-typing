# Ready Typing

A polished Bengali typing tutor for the **Bijoy / SutonnyMJ legacy** keyboard.
Ready Typing runs completely offline, embeds its own font, and needs no
administrator rights.

## What is in this folder

| File | Purpose |
| --- | --- |
| `dist/ReadyTypingSetup.exe` | Recommended installer. Installs the trainer per user, adds Desktop and Start-menu shortcuts, and can launch it when finished. |
| `dist/ReadyTyping.exe` | Portable single executable (about 61 MB). Double-click to run; nothing to install, no .NET runtime needed. |
| `build-setup.ps1` | Rebuilds the trainer, the compressed installer payload and both files in `dist/`. |
| `test-lessons.ps1` | Runs the repository test project against all 118 lessons. |

The trainer includes **118 lessons**: warm-up drills, everyday words, sentences,
**100 Bangladesh captions** in ten themed groups, and a closing proposal line
written for the developer of the app.

## Timed lessons

Each lesson has its own countdown. The clock starts on the first keystroke (or
when you press **Start**), and **Next lesson** stays locked until the line is
typed correctly before the timer reaches zero. If time runs out the input
locks; press **Reset** to try the same lesson again. Every lesson's time
budget is the previous budget multiplied by **2.9×**, so short drills still
move quickly and the long captions have room to breathe.

## Progress

The app remembers the last lesson the learner **passes** in
`%LocalAppData%\Ready Typing\progress.txt`. The next launch opens the following
lesson, or the finale after all 118 are complete. Press **Lesson 1** at any
time to return to the first lesson. No administrator rights are needed and the
progress file is never needed to install or run the trainer.

## Lesson results

When a lesson ends — completed or timed out — the results screen opens inside
the app window, modelled on RapidTyping:

- **Next step** shows the overall rating, speed in WPM, slowdown by words and
  keystrokes, and a five-star rating.
- **Detailed statistics** shows per-key speed in KPM and per-key accuracy with
  `correct:total` counts, with keys coloured by finger.
- A clean run unlocks **Go to the next lesson**; mistakes or a timeout preselect
  **Try again**. **Enter** (or **Go on**) confirms; other keys do nothing.

## Install

1. Run `dist/ReadyTypingSetup.exe`.
2. Choose the install folder (default `%LocalAppData%\Ready Typing`) and the
   shortcuts you want.
3. Press **Install now**. When it finishes, press **Open Ready Typing**.

The installer copies a single self-contained executable; the Bengali font is
inside it, so nothing is added to Windows Fonts.

## Test

```powershell
.\test-lessons.ps1 -Dotnet "path\to\dotnet.exe"
```

The test project verifies the core mappings, progress resume rules, the result
report model, and physical-key playback of all **118 lessons**.

## Build

Requirements: a .NET SDK (6.0 or newer) and the .NET Framework 4.8 runtime for
the installer.

```powershell
.\build-setup.ps1
```

The script publishes the app, gzips the payload, builds the setup window, and
writes both deliverables into `dist/`.

Full documentation: [`BijoyTrainer/README.md`](BijoyTrainer/README.md).

## A note on the text

The lesson bank stores **SutonnyMJ legacy ANSI codes**, not Unicode Bengali.
For example `Avwg evsjvq Mvb MvB|` renders as *আমি বাংলায় গান গাই।* with the
embedded font. Keep that rule when adding new lessons.

One exception is needed for the e-kar right after a ra-phala: the lesson bank
uses the compact `†` (U+2020) instead of the standard `‡` (U+2021) there, so the
word **প্রপোজ** keeps its vowel sign attached (`c«†cvR`). The app applies the same
rule automatically while you type.

All English text in the app and the installer is set in **Times New Roman**.

## License

The source code is released under the **MIT License** — see [LICENSE](LICENSE).

The SutonnyMJ typeface embedded in the app is distributed with the SutonnyMJ
keyboard and is **not** covered by the MIT license; it remains the property of
its authors.

