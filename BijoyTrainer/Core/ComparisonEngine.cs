using System;
using System.Collections.Generic;

namespace BijoyTrainer.Core;

/// <summary>
/// Compares the text typed by the learner with the current lesson, character by
/// character. The comparison operates on SutonnyMJ code points, so one stored
/// character always corresponds to one painted glyph position.
/// </summary>
public sealed class ComparisonEngine
{
    private readonly List<bool> _results = new();

    /// <summary>Gets the current target text.</summary>
    public string Target { get; private set; } = string.Empty;

    /// <summary>Gets the text most recently passed to <see cref="Update"/>.</summary>
    public string Typed { get; private set; } = string.Empty;

    /// <summary>Gets the number of correctly typed characters.</summary>
    public int CorrectCount { get; private set; }

    /// <summary>Gets the number of incorrectly typed or extra characters.</summary>
    public int ErrorCount { get; private set; }

    /// <summary>Gets the total number of characters typed.</summary>
    public int TotalTyped => Typed.Length;

    /// <summary>
    /// Gets a per-character result for every typed character. Index <c>i</c> is
    /// <c>true</c> when <c>Typed[i]</c> equals <c>Target[i]</c>.
    /// </summary>
    public IReadOnlyList<bool> PerCharacterResult => _results;

    /// <summary>
    /// Gets a value indicating whether the learner has typed at least as many
    /// characters as the target contains.
    /// </summary>
    public bool IsComplete => Target.Length > 0 && Typed.Length >= Target.Length;

    /// <summary>Gets the index of the next character to type.</summary>
    public int CurrentIndex => Math.Min(Typed.Length, Target.Length);

    /// <summary>Gets the index of the first mistake, or <c>-1</c>.</summary>
    public int FirstErrorIndex
    {
        get
        {
            for (int i = 0; i < _results.Count; i++)
            {
                if (!_results[i])
                {
                    return i;
                }
            }

            return -1;
        }
    }

    /// <summary>Sets a new lesson target and clears the comparison.</summary>
    /// <param name="target">The target SutonnyMJ text.</param>
    public void SetTarget(string target)
    {
        Target = target ?? string.Empty;
        Typed = string.Empty;
        CorrectCount = 0;
        ErrorCount = 0;
        _results.Clear();
    }

    /// <summary>Recomputes the comparison for the supplied typed text.</summary>
    /// <param name="typed">The text currently in the input box.</param>
    public void Update(string typed)
    {
        Typed = typed ?? string.Empty;
        _results.Clear();

        int correct = 0;
        int errors = 0;

        for (int i = 0; i < Typed.Length; i++)
        {
            bool isCorrect = i < Target.Length && Typed[i] == Target[i];
            _results.Add(isCorrect);

            if (isCorrect)
            {
                correct++;
            }
            else
            {
                errors++;
            }
        }

        CorrectCount = correct;
        ErrorCount = errors;
    }
}
