using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using BijoyTrainer.Core;

internal static class Program
{
    private sealed class Editor
    {
        public readonly StringBuilder Text = new();
        public int Caret;

        public void Apply(LigatureAction action)
        {
            switch (action.Kind)
            {
                case LigatureActionKind.None:
                    break;

                case LigatureActionKind.Insert:
                    Text.Insert(Caret, action.Text);
                    Caret += action.Text.Length;
                    break;

                case LigatureActionKind.ReplacePrevious:
                    if (Caret > 0 &&
                        action.ExpectedPrevious is char expected &&
                        Text[Caret - 1] == expected)
                    {
                        Text.Remove(Caret - 1, 1);
                        Text.Insert(Caret - 1, action.Text);
                        Caret += action.Text.Length - 1;
                    }
                    else
                    {
                        Text.Insert(Caret, action.Fallback);
                        Caret += action.Fallback.Length;
                    }

                    break;
            }
        }

        public override string ToString() => Text.ToString();
    }

    private static int _failures;

    private static void Check(bool condition, string name, string? detail = null)
    {
        if (condition)
        {
            Console.WriteLine("PASS  " + name);
        }
        else
        {
            _failures++;
            Console.WriteLine("FAIL  " + name + (detail is null ? string.Empty : "  ->  " + detail));
        }
    }

    private static string TypeKeys(params (char Key, bool Shift)[] keys)
    {
        var handler = new BijoyInputHandler();
        var editor = new Editor();

        foreach ((char key, bool shift) in keys)
        {
            char? previous = editor.Text.Length > 0 ? editor.Text[editor.Text.Length - 1] : null;
            if (handler.TryProcessKey(key, shift, previous, out LigatureAction action))
            {
                editor.Apply(action);
            }
            else
            {
                throw new InvalidOperationException($"Unmapped key {key} (shift={shift}).");
            }
        }

        return editor.Text.ToString();
    }

    private static IEnumerable<(char, bool)> KeysFor(string legacy)
    {
        for (int pos = 0; pos < legacy.Length; pos++)
        {
            char ch = legacy[pos];

            if (ch == ' ')
            {
                yield return (' ', false);
                continue;
            }

            string? matched = null;
            foreach (var pair in BijoyMappingTable.ComposedConjuncts)
            {
                string value = pair.Key;
                if (value.Length <= 1 || value.Length <= (matched?.Length ?? 0))
                {
                    continue;
                }

                if (pos + value.Length <= legacy.Length &&
                    string.CompareOrdinal(legacy, pos, value, 0, value.Length) == 0)
                {
                    matched = value;
                }
            }

            if (matched is not null)
            {
                var parts = BijoyMappingTable.ComposedConjuncts[matched];
                if (BijoyMappingTable.TryGetPhysicalKey(parts.First, out char a, out bool aShift) &&
                    BijoyMappingTable.TryGetPhysicalKey(parts.Second, out char b, out bool bShift))
                {
                    yield return (a, aShift);
                    yield return ('g', false);
                    yield return (b, bShift);
                    pos += matched.Length - 1;
                    continue;
                }
            }

            // Composed conjuncts are typed as first consonant, hasanta, second.
            if (BijoyMappingTable.TryGetConjunctParts(ch.ToString(), out char first, out char second) &&
                BijoyMappingTable.TryGetPhysicalKey(first, out char firstKey, out bool firstShift) &&
                BijoyMappingTable.TryGetPhysicalKey(second, out char secondKey, out bool secondShift))
            {
                yield return (firstKey, firstShift);
                yield return ('g', false);
                yield return (secondKey, secondShift);
                continue;
            }

            // Independent vowels without a key are hasanta + vowel sign.
            if (BijoyMappingTable.TryGetIndependentVowelSign(ch, out char sign) &&
                BijoyMappingTable.TryGetPhysicalKey(sign, out char signKey, out bool signShift))
            {
                yield return ('g', false);
                yield return (signKey, signShift);
                continue;
            }

            if (!BijoyMappingTable.TryGetPhysicalKey(ch, out char key, out bool shift))
            {
                throw new InvalidOperationException($"No physical key for U+{(int)ch:X4} '{ch}'.");
            }

            yield return (key, shift);
        }
    }

    private static string Escape(string text)
    {
        var sb = new StringBuilder();
        foreach (char ch in text)
        {
            sb.Append(ch < 128 ? ch.ToString() : $"\\u{(int)ch:X4}");
        }

        return sb.ToString();
    }

    private static string FindLessonFile()
    {
        string[] candidates =
        {
            Path.GetFullPath(Path.Combine("BijoyTrainer", "Data", "lessons.txt")),
            Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "..", "BijoyTrainer", "Data", "lessons.txt")),
            Path.Combine(AppContext.BaseDirectory, "lessons.txt")
        };

        string? found = candidates.FirstOrDefault(File.Exists);
        if (found is null)
        {
            throw new FileNotFoundException(
                "Could not find BijoyTrainer/Data/lessons.txt for the lesson tests.");
        }

        return found;
    }

    private static void Main(string[] args)
    {
        // 1. Core mapping spot checks.
        Check(TypeKeys(('j', false), ('k', false), ('l', false)) == "KZ`", "home-row mapping");
        Check(TypeKeys(('f', true), ('f', false), ('d', false), ('m', false)) == "Avwg", "Avwg sequence");
        Check(TypeKeys(('m', false)) == "g", "m -> ম");
        Check(TypeKeys(('h', false)) == "e", "h -> ব");
        Check(TypeKeys(('n', true)) == "l", "Shift+N -> ষ");
        Check(TypeKeys(('g', true)) == "|", "Shift+G -> ।");

        // 2. Conjunct state machine.
        Check(TypeKeys(('j', false), ('g', false), ('n', true)) == "¶", "K + halant + ষ -> ক্ষ");
        Check(TypeKeys(('j', false), ('g', false), ('k', false)) == "³", "K + halant + ত -> ক্ত");
        Check(TypeKeys(('d', false), ('m', true), ('j', false), ('g', false), ('n', true)) == "wk¶",
            "wk + halant + ষ -> wk¶");
        Check(TypeKeys(('b', false), ('g', false), ('l', false)) == "›`",
            "N + halant + দ -> ন্দ (multi-character conjunct)");
        Check(TypeKeys(('j', false), ('a', true), ('m', false)) == "K©g",
            "reph after a consonant");
        string independentAfterConsonant = TypeKeys(
            ('h', false), ('c', false), ('v', true), ('g', false), ('d', false));
        Check(independentAfterConsonant == "e‡jB",
            "independent ই after a consonant (বলেই)", Escape(independentAfterConsonant));

        var pending = new BijoyInputHandler();
        pending.TryProcessKey('g', false, out _);
        Check(pending.IsPending, "halant enters pending state");
        Check(pending.ProcessBackspace() && !pending.IsPending, "backspace cancels pending halant");
        Check(!pending.ProcessBackspace(), "second backspace passes through");

        var flush = new BijoyInputHandler();
        flush.TryProcessKey('j', false, out LigatureAction first);
        flush.TryProcessKey('g', false, out _);
        bool handled = flush.TryProcessKey(' ', false, out LigatureAction spaceAction);
        var flushEditor = new Editor();
        flushEditor.Apply(first);
        flushEditor.Apply(spaceAction);
        Check(handled && flushEditor.Text.ToString() == "K& ", "pending halant flushes before space");

        // 3. Comparison engine.
        var comparison = new ComparisonEngine();
        comparison.SetTarget("K Z `");
        comparison.Update("K Z m");
        Check(comparison.CorrectCount == 4 && comparison.ErrorCount == 1, "comparison counts",
            $"{comparison.CorrectCount}/{comparison.ErrorCount}");
        Check(comparison.PerCharacterResult.Count == 5 &&
              comparison.PerCharacterResult[0] &&
              comparison.PerCharacterResult[1] &&
              comparison.PerCharacterResult[2] &&
              comparison.PerCharacterResult[3] &&
              !comparison.PerCharacterResult[4], "comparison per-character results");
        comparison.Update("K Z ` ");
        Check(comparison.ErrorCount == 1, "extra typed character counts as an error");
        Check(comparison.IsComplete, "completion flag");

        // 4. Stats.
        var stats = new StatsTracker();
        Check(Math.Abs(stats.Accuracy - 100d) < 0.001, "accuracy defaults to 100");
        stats.Update(9, 10);
        Check(Math.Abs(stats.Accuracy - 90d) < 0.001, "accuracy 90%");
        stats.Update(0, 0);
        Check(Math.Abs(stats.Accuracy - 100d) < 0.001, "accuracy resets to 100");

        // 4b. Lesson report used by the results screen.
        var strokes = new List<Keystroke>
        {
            new('a', true, TimeSpan.FromSeconds(0.0), 0),
            new('b', true, TimeSpan.FromSeconds(0.2), 1),
            new('c', true, TimeSpan.FromSeconds(0.4), 2),
            new('d', false, TimeSpan.FromSeconds(0.6), 3),
            new(' ', true, TimeSpan.FromSeconds(0.8), 4),
            new('e', true, TimeSpan.FromSeconds(3.0), 5),
            new('e', true, TimeSpan.FromSeconds(3.2), 6)
        };

        LessonReport report = LessonReport.Build(
            "Demo line",
            3,
            118,
            "abcd e",
            passed: true,
            elapsed: TimeSpan.FromSeconds(3.4),
            correctChars: 7,
            typedChars: 7,
            strokes);

        Check(report.Keys.Count == 6, "report tracks every key", report.Keys.Count.ToString());
        Check(Math.Abs(report.Accuracy - 100d) < 0.001, "report accuracy");
        Check(Math.Abs(report.Rating - 100d) < 0.001 && report.Stars == 5,
            "perfect rating earns five stars");
        Check(Math.Abs(report.WordsWithDelay - 50d) < 0.001,
            "word delay percentage", report.WordsWithDelay.ToString("0.0"));
        Check(Math.Abs(report.SlowKeystrokes - (100d / 6d)) < 0.001,
            "slow keystroke percentage", report.SlowKeystrokes.ToString("0.0"));

        KeyReport dKey = report.Keys.First(key => key.Key == 'd');
        KeyReport eKey = report.Keys.First(key => key.Key == 'e');
        Check(dKey.Presses == 1 && dKey.Correct == 0 && Math.Abs(dKey.Accuracy) < 0.001,
            "per-key accuracy records mistakes");
        Check(eKey.Presses == 2 && eKey.Correct == 2 && Math.Abs(eKey.Accuracy - 100d) < 0.001,
            "per-key accuracy counts repeats");
        Check(eKey.SpeedKpm > 0d, "per-key speed is sampled", eKey.SpeedKpm.ToString("0.0"));

        LessonReport failed = LessonReport.Build(
            "Demo line",
            3,
            118,
            "abcd",
            passed: false,
            elapsed: TimeSpan.FromSeconds(1),
            correctChars: 2,
            typedChars: 4,
            strokes);
        Check(!failed.Passed && failed.Stars == 2 && Math.Abs(failed.Rating - 50d) < 0.001,
            "half accuracy earns two stars");

        // 5. Progress resume rules.
        Check(LessonProgressStore.ParseCompleted("7") == 7, "progress file parses a number");
        Check(LessonProgressStore.ParseCompleted("junk") == 0, "progress file rejects invalid input");
        Check(LessonProgressStore.ResolveResumeIndex(0, 118) == 0, "no progress starts at lesson 1");
        Check(LessonProgressStore.ResolveResumeIndex(1, 118) == 1, "passing lesson 1 resumes at lesson 2");
        Check(LessonProgressStore.ResolveResumeIndex(117, 118) == 117, "passing lesson 117 resumes at lesson 118");
        Check(LessonProgressStore.ResolveResumeIndex(118, 118) == 117, "finishing the bank keeps the finale open");

        // 6. Lesson round trip: every lesson can be produced by physical keys.
        string lessonsPath = args.Length > 0
            ? args[0]
            : FindLessonFile();

        string[] lines = File.ReadAllLines(lessonsPath);
        int lessons = 0;
        int skipped = 0;

        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//") || line.StartsWith("#"))
            {
                continue;
            }

            int sep = line.IndexOf("=>", StringComparison.Ordinal);
            string text = sep >= 0 ? line[(sep + 2)..].Trim() : line;
            if (text.Length == 0)
            {
                continue;
            }

            lessons++;

            string produced;
            try
            {
                produced = TypeKeys(KeysFor(text).ToArray());
            }
            catch (Exception ex)
            {
                _failures++;
                Console.WriteLine($"FAIL  lesson {lessons} threw: {ex.Message}  [{Escape(text)}]");
                continue;
            }

            if (produced != text)
            {
                _failures++;
                Console.WriteLine($"FAIL  lesson {lessons}: {Escape(text)} != {Escape(produced)}");
            }
            else
            {
                skipped++;
            }

            for (int i = 0; i < text.Length; )
            {
                string? multi = null;
                foreach (var pair in BijoyMappingTable.ComposedConjuncts)
                {
                    string value = pair.Key;
                    if (value.Length <= 1 || value.Length <= (multi?.Length ?? 0))
                    {
                        continue;
                    }

                    if (i + value.Length <= text.Length &&
                        string.CompareOrdinal(text, i, value, 0, value.Length) == 0)
                    {
                        multi = value;
                    }
                }

                if (multi is not null)
                {
                    if (BijoyMappingTable.GetKeyHint(multi[0]) == "?")
                    {
                        _failures++;
                        Console.WriteLine(
                            $"FAIL  lesson {lessons} has no key hint for cluster at index {i}");
                        break;
                    }

                    i += multi.Length;
                    continue;
                }

                if (BijoyMappingTable.GetKeyHint(text[i]) == "?")
                {
                    _failures++;
                    Console.WriteLine(
                        $"FAIL  lesson {lessons} has no key hint for U+{(int)text[i]:X4} at index {i}");
                    break;
                }

                i++;
            }
        }

        Console.WriteLine($"Lessons verified: {skipped}/{lessons}");
        Console.WriteLine(_failures == 0 ? "ALL TESTS PASSED" : $"{_failures} FAILURE(S)");
        Environment.ExitCode = _failures == 0 ? 0 : 1;
    }
}
