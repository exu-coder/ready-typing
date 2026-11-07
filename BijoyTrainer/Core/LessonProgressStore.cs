using System;
using System.Globalization;
using System.IO;

namespace BijoyTrainer.Core;

/// <summary>
/// A tiny, UI-free store for the learner's last completed lesson. The file is
/// kept beside the per-user app data, so no administrator rights are needed.
/// </summary>
public static class LessonProgressStore
{
    /// <summary>Gets the progress file name.</summary>
    public const string FileName = "progress.txt";

    /// <summary>Gets the full path to the progress file.</summary>
    /// <returns>The progress file path, or <c>null</c> if LocalAppData is unavailable.</returns>
    public static string? GetPath()
    {
        // Tests can redirect persistence without touching a real install.
        string? overridePath = Environment.GetEnvironmentVariable("READY_TYPING_PROGRESS_FILE");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(folder))
        {
            return null;
        }

        return Path.Combine(folder, "Ready Typing", FileName);
    }

    /// <summary>Gets the last completed one-based lesson, or 0 if none.</summary>
    public static int LoadLastCompleted()
    {
        string? path = GetPath();
        if (path is null || !File.Exists(path))
        {
            return 0;
        }

        try
        {
            return ParseCompleted(File.ReadAllText(path));
        }
        catch
        {
            // Progress is a convenience; never block the lesson bank.
            return 0;
        }
    }

    /// <summary>Gets the lesson index to load on startup.</summary>
    /// <param name="lessonCount">The total number of lessons in the bank.</param>
    /// <returns>A zero-based lesson index.</returns>
    public static int LoadResumeIndex(int lessonCount)
    {
        return ResolveResumeIndex(LoadLastCompleted(), lessonCount);
    }

    /// <summary>
    /// Converts a saved one-based completion count into a zero-based lesson
    /// index to open. A saved completed lesson is the one just finished, so the
    /// next lesson is opened.
    /// </summary>
    /// <param name="completed">The last one-based completed lesson.</param>
    /// <param name="lessonCount">The total number of lessons.</param>
    /// <returns>A zero-based lesson index.</returns>
    public static int ResolveResumeIndex(int completed, int lessonCount)
    {
        if (lessonCount <= 0)
        {
            return 0;
        }

        if (completed <= 0)
        {
            return 0;
        }

        return Math.Min(completed, lessonCount - 1);
    }

    /// <summary>Saves the last completed one-based lesson.</summary>
    /// <param name="lessonNumber">The one-based lesson number.</param>
    public static void SaveLastCompleted(int lessonNumber)
    {
        if (lessonNumber <= 0)
        {
            return;
        }

        string? path = GetPath();
        if (path is null)
        {
            return;
        }

        try
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, lessonNumber.ToString(CultureInfo.InvariantCulture));
        }
        catch
        {
            // Progress is a convenience; never let a save failure block a lesson.
        }
    }

    /// <summary>Parses a saved progress value.</summary>
    /// <param name="text">The raw text stored in the progress file.</param>
    /// <returns>A non-negative lesson number, or 0 when missing or invalid.</returns>
    public static int ParseCompleted(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) ||
            !int.TryParse(text.Trim(), out int value))
        {
            return 0;
        }

        return Math.Max(0, value);
    }
}
