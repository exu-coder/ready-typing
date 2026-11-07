using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace BijoyTrainer.Core;

/// <summary>
/// A single practice line. <see cref="Text"/> is stored in SutonnyMJ legacy
/// codes; the UI paints it with the embedded font.
/// </summary>
/// <param name="Index">Zero-based index in the lesson list.</param>
/// <param name="Title">Short user-facing title.</param>
/// <param name="Group">Group heading from the lesson file.</param>
/// <param name="Text">The target text in SutonnyMJ codes.</param>
public sealed record Lesson(int Index, string Title, string Group, string Text);

/// <summary>
/// Loads lessons from the embedded <c>Data/lessons.txt</c> resource and parses
/// the simple line-based lesson format.
/// </summary>
/// <remarks>
/// <para>Format:</para>
/// <code>
/// # Group heading
/// Optional title => target text in SutonnyMJ codes
/// Another lesson line
/// </code>
/// <para>
/// Blank lines are ignored. Lines beginning with <c>#</c> start a group. Lines
/// beginning with <c>//</c> are comments. The <c>=&gt;</c> title separator is
/// optional; without it the manager generates a numbered title.
/// </para>
/// </remarks>
public sealed class LessonManager
{
    /// <summary>The manifest resource name of the embedded lesson file.</summary>
    public const string LessonResourceName = "BijoyTrainer.Data.lessons.txt";

    private const string TitleSeparator = "=>";

    private static readonly Lesson[] EmergencyLessons =
    {
        new(0, "Warm up", "Built-in", "K Z ` K Z ` K Z `"),
        new(1, "Home row words", "Built-in", "Kg Kj ej Ze `g"),
        new(2, "Greeting", "Built-in", "Avwg evsjvq Mvb MvB|")
    };

    private LessonManager(IReadOnlyList<Lesson> lessons)
    {
        Lessons = lessons;
    }

    /// <summary>Gets all loaded lessons in display order.</summary>
    public IReadOnlyList<Lesson> Lessons { get; }

    /// <summary>
    /// Loads the lesson file. The embedded resource is preferred; a file under
    /// <c>Data/lessons.txt</c> next to the executable is used as a fallback so
    /// lessons can be edited without rebuilding.
    /// </summary>
    /// <param name="warning">
    /// A non-fatal message describing a fallback, or <c>null</c>.
    /// </param>
    /// <returns>A manager that always contains at least one lesson.</returns>
    public static LessonManager Load(out string? warning)
    {
        var warnings = new List<string>();

        try
        {
            using Stream? stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(LessonResourceName);

            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                string content = reader.ReadToEnd();
                var parsed = Parse(content);
                if (parsed.Count > 0)
                {
                    warning = null;
                    return new LessonManager(parsed);
                }

                warnings.Add("The embedded lesson file did not contain any usable lessons.");
            }
            else
            {
                warnings.Add("The embedded lesson file was not found.");
            }
        }
        catch (Exception ex)
        {
            warnings.Add("The embedded lesson file could not be read: " + ex.Message);
        }

        try
        {
            string externalPath = Path.Combine(AppContext.BaseDirectory, "Data", "lessons.txt");
            if (File.Exists(externalPath))
            {
                var parsed = Parse(File.ReadAllText(externalPath));
                if (parsed.Count > 0)
                {
                    warning = string.Join(" ", warnings) + " Loaded Data/lessons.txt from disk instead.";
                    return new LessonManager(parsed);
                }

                warnings.Add("Data/lessons.txt contained no usable lessons.");
            }
        }
        catch (Exception ex)
        {
            warnings.Add("Data/lessons.txt could not be read: " + ex.Message);
        }

        warnings.Add("Using the built-in emergency lessons.");
        warning = string.Join(" ", warnings);
        return new LessonManager(EmergencyLessons);
    }

    private static List<Lesson> Parse(string content)
    {
        var lessons = new List<Lesson>();
        string group = "Lessons";
        int lessonNumber = 0;

        foreach (string rawLine in content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
        {
            string line = rawLine.TrimEnd();
            string trimmed = line.Trim();

            if (trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                group = trimmed.TrimStart('#').Trim();
                if (group.Length == 0)
                {
                    group = "Lessons";
                }

                continue;
            }

            string title;
            string text;

            int separatorIndex = line.IndexOf(TitleSeparator, StringComparison.Ordinal);
            if (separatorIndex > 0)
            {
                title = line[..separatorIndex].Trim();
                text = line[(separatorIndex + TitleSeparator.Length)..].Trim();
            }
            else
            {
                title = $"Lesson {lessonNumber + 1}";
                text = trimmed;
            }

            if (text.Length == 0)
            {
                continue;
            }

            if (title.Length == 0)
            {
                title = $"Lesson {lessonNumber + 1}";
            }

            lessons.Add(new Lesson(lessonNumber, title, group, text));
            lessonNumber++;
        }

        return lessons;
    }
}
