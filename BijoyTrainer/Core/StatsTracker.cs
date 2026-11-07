using System;
using System.Diagnostics;

namespace BijoyTrainer.Core;

/// <summary>
/// Tracks elapsed typing time and derives CPM, WPM and accuracy.
/// </summary>
/// <remarks>
/// CPM uses correctly typed characters, which is the usual "net" typing speed
/// shown by Bengali typing tutors. WPM uses the standard five-character word.
/// Accuracy is correct characters divided by characters typed, so it drops as
/// soon as a mistake is made and recovers when the learner backspaces over it.
/// </remarks>
public sealed class StatsTracker
{
    private static readonly TimeSpan MinimumMeasuredTime = TimeSpan.FromSeconds(1);

    private readonly Stopwatch _stopwatch = new();

    private int _correctChars;
    private int _typedChars;

    /// <summary>Gets a value indicating whether the clock is running.</summary>
    public bool IsRunning => _stopwatch.IsRunning;

    /// <summary>Gets the elapsed typing time.</summary>
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <summary>Gets the number of correct characters in the last update.</summary>
    public int CorrectChars => _correctChars;

    /// <summary>Gets the number of typed characters in the last update.</summary>
    public int TypedChars => _typedChars;

    /// <summary>Gets the current characters-per-minute value.</summary>
    public double CharactersPerMinute
    {
        get
        {
            if (Elapsed < MinimumMeasuredTime)
            {
                return 0d;
            }

            return _correctChars / Elapsed.TotalMinutes;
        }
    }

    /// <summary>Gets the current words-per-minute value (5 characters = 1 word).</summary>
    public double WordsPerMinute => CharactersPerMinute / 5d;

    /// <summary>
    /// Gets the accuracy percentage. A learner who has not typed anything yet
    /// is shown as 100%.
    /// </summary>
    public double Accuracy => _typedChars == 0 ? 100d : (_correctChars * 100d) / _typedChars;

    /// <summary>Starts the clock if it is not already running.</summary>
    public void StartTimer()
    {
        if (!_stopwatch.IsRunning)
        {
            _stopwatch.Start();
        }
    }

    /// <summary>Stops the clock, preserving the elapsed time.</summary>
    public void StopTimer() => _stopwatch.Stop();

    /// <summary>Stops the clock and clears every statistic.</summary>
    public void Reset()
    {
        _stopwatch.Reset();
        _correctChars = 0;
        _typedChars = 0;
    }

    /// <summary>
    /// Updates the counters that drive CPM and accuracy. Called after every
    /// change to the input box and on each UI timer tick.
    /// </summary>
    /// <param name="correctChars">Correct characters reported by the comparison engine.</param>
    /// <param name="typedChars">Total characters typed.</param>
    public void Update(int correctChars, int typedChars)
    {
        _correctChars = Math.Max(0, correctChars);
        _typedChars = Math.Max(0, typedChars);
    }
}
