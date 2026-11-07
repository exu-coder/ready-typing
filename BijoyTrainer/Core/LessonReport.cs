using System;
using System.Collections.Generic;
using System.Linq;

namespace BijoyTrainer.Core;

/// <summary>One processed key press during a lesson.</summary>
/// <param name="Key">The physical key, lower-case for letters.</param>
/// <param name="Correct">Whether the inserted text matched the lesson.</param>
/// <param name="At">Elapsed time since the lesson clock started.</param>
/// <param name="Position">The target index where the key inserted text.</param>
public readonly record struct Keystroke(char Key, bool Correct, TimeSpan At, int Position);

/// <summary>Per-key totals used by the detailed statistics view.</summary>
public sealed class KeyReport
{
    /// <summary>Gets the physical key, lower-case for letters.</summary>
    public char Key { get; init; }

    /// <summary>Gets the number of presses of this key.</summary>
    public int Presses { get; init; }

    /// <summary>Gets the number of correct presses of this key.</summary>
    public int Correct { get; init; }

    /// <summary>Gets the average speed, in key presses per minute, for this key.</summary>
    public double SpeedKpm { get; init; }

    /// <summary>Gets the accuracy percentage for this key.</summary>
    public double Accuracy => Presses == 0 ? 100d : Correct * 100d / Presses;

    /// <summary>Gets a display label such as <c>f</c> or <c>Space</c>.</summary>
    public string Label => Key == ' ' ? "Space" : Key.ToString();
}

/// <summary>
/// A finished lesson's numbers, ready for the results window. Built from the
/// lesson target, the typed counters and every recorded keystroke.
/// </summary>
public sealed class LessonReport
{
    private const double PauseThresholdSeconds = 1.5d;

    /// <summary>Gets the lesson title.</summary>
    public string Title { get; private init; } = string.Empty;

    /// <summary>Gets the one-based lesson number.</summary>
    public int LessonNumber { get; private init; }

    /// <summary>Gets the total number of lessons.</summary>
    public int LessonCount { get; private init; }

    /// <summary>Gets a value indicating whether the line was typed without mistakes.</summary>
    public bool Passed { get; private init; }

    /// <summary>Gets the elapsed lesson time.</summary>
    public TimeSpan Elapsed { get; private init; }

    /// <summary>Gets the number of correct characters.</summary>
    public int CorrectChars { get; private init; }

    /// <summary>Gets the number of typed characters.</summary>
    public int TypedChars { get; private init; }

    /// <summary>Gets the number of wrong or extra characters in the final text.</summary>
    public int Mistakes => Math.Max(0, TypedChars - CorrectChars);

    /// <summary>Gets the target length in characters.</summary>
    public int TargetLength { get; private init; }

    /// <summary>Gets the overall accuracy percentage.</summary>
    public double Accuracy { get; private init; }

    /// <summary>Gets the net characters per minute.</summary>
    public double CharactersPerMinute { get; private init; }

    /// <summary>Gets the net words per minute (five characters = one word).</summary>
    public double WordsPerMinute { get; private init; }

    /// <summary>Gets the overall rating shown on the first bar (0-100).</summary>
    public double Rating { get; private init; }

    /// <summary>Gets the number of filled stars (1-5).</summary>
    public int Stars { get; private init; }

    /// <summary>Gets the percentage of words typed after a long pause.</summary>
    public double WordsWithDelay { get; private init; }

    /// <summary>Gets the percentage of keystrokes that followed a long pause.</summary>
    public double SlowKeystrokes { get; private init; }

    /// <summary>Gets the per-key statistics, most used key first.</summary>
    public IReadOnlyList<KeyReport> Keys { get; private init; } = Array.Empty<KeyReport>();

    /// <summary>
    /// Builds a report from a finished lesson.
    /// </summary>
    /// <param name="title">Lesson title shown in the header.</param>
    /// <param name="lessonNumber">One-based lesson number.</param>
    /// <param name="lessonCount">Total lesson count.</param>
    /// <param name="target">The lesson's target text.</param>
    /// <param name="passed">Whether the line was completed without mistakes.</param>
    /// <param name="elapsed">The lesson clock at completion.</param>
    /// <param name="correctChars">Correct characters reported by the comparison.</param>
    /// <param name="typedChars">Characters typed.</param>
    /// <param name="keystrokes">Every typed key press, in order.</param>
    /// <returns>The completed report.</returns>
    public static LessonReport Build(
        string title,
        int lessonNumber,
        int lessonCount,
        string target,
        bool passed,
        TimeSpan elapsed,
        int correctChars,
        int typedChars,
        IReadOnlyList<Keystroke> keystrokes)
    {
        target ??= string.Empty;
        keystrokes ??= Array.Empty<Keystroke>();

        double minutes = Math.Max(elapsed.TotalMinutes, 1d / 60d);
        double accuracy = typedChars == 0 ? 0d : correctChars * 100d / typedChars;
        double cpm = correctChars / minutes;

        var byKey = new Dictionary<char, KeyTotals>();
        double previousSeconds = -1d;

        foreach (Keystroke stroke in keystrokes)
        {
            char key = stroke.Key == ' ' ? ' ' : char.ToLowerInvariant(stroke.Key);

            if (!byKey.TryGetValue(key, out KeyTotals? totals))
            {
                totals = new KeyTotals();
                byKey[key] = totals;
            }

            totals.Presses++;
            if (stroke.Correct)
            {
                totals.Correct++;
            }

            // Sample the typing pace at the moment this key was pressed. The
            // gap to the previous press is a better per-key speed than the
            // running average, which would make every late key look fast.
            if (previousSeconds >= 0d)
            {
                double gap = Math.Clamp(stroke.At.TotalSeconds - previousSeconds, 0.12d, 6d);
                totals.SpeedSum += 60d / gap;
                totals.SpeedSamples++;
            }

            previousSeconds = stroke.At.TotalSeconds;
        }

        List<KeyReport> keys = byKey
            .Select(pair => new KeyReport
            {
                Key = pair.Key,
                Presses = pair.Value.Presses,
                Correct = pair.Value.Correct,
                SpeedKpm = pair.Value.SpeedSamples == 0
                    ? cpm
                    : pair.Value.SpeedSum / pair.Value.SpeedSamples
            })
            .OrderByDescending(key => key.Presses)
            .ThenBy(key => key.Key)
            .Take(10)
            .ToList();

        int slowStrokes = 0;
        for (int i = 1; i < keystrokes.Count; i++)
        {
            if ((keystrokes[i].At - keystrokes[i - 1].At).TotalSeconds > PauseThresholdSeconds)
            {
                slowStrokes++;
            }
        }

        double slowKeystrokes = keystrokes.Count > 1
            ? slowStrokes * 100d / (keystrokes.Count - 1)
            : 0d;

        int words = 0;
        int delayedWords = 0;
        int wordStart = 0;

        for (int i = 0; i <= target.Length; i++)
        {
            bool end = i == target.Length || target[i] == ' ';
            if (!end)
            {
                continue;
            }

            if (i > wordStart)
            {
                words++;
                int first = -1;
                for (int k = 0; k < keystrokes.Count; k++)
                {
                    if (keystrokes[k].Position >= wordStart)
                    {
                        first = k;
                        break;
                    }
                }

                if (first > 0 &&
                    (keystrokes[first].At - keystrokes[first - 1].At).TotalSeconds > PauseThresholdSeconds)
                {
                    delayedWords++;
                }
            }

            wordStart = i + 1;
        }

        double rating = Math.Clamp(accuracy, 0d, 100d);

        return new LessonReport
        {
            Title = title,
            LessonNumber = lessonNumber,
            LessonCount = lessonCount,
            Passed = passed,
            Elapsed = elapsed,
            CorrectChars = correctChars,
            TypedChars = typedChars,
            TargetLength = target.Length,
            Accuracy = accuracy,
            CharactersPerMinute = cpm,
            WordsPerMinute = cpm / 5d,
            Rating = rating,
            Stars = StarsFor(rating),
            WordsWithDelay = words == 0 ? 0d : delayedWords * 100d / words,
            SlowKeystrokes = slowKeystrokes,
            Keys = keys
        };
    }

    private static int StarsFor(double rating)
    {
        return rating >= 95d ? 5
            : rating >= 85d ? 4
            : rating >= 70d ? 3
            : rating >= 50d ? 2
            : 1;
    }

    private sealed class KeyTotals
    {
        public int Presses;
        public int Correct;
        public double SpeedSum;
        public int SpeedSamples;
    }
}
