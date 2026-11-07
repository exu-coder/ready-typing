namespace BijoyTrainer.Core;

/// <summary>
/// Describes what the input handler should do with the text box after a key
/// passes through the <see cref="LigatureBuffer"/>.
/// </summary>
public enum LigatureActionKind
{
    /// <summary>Nothing needs to be inserted; the key is now buffered.</summary>
    None,

    /// <summary>Insert <see cref="LigatureAction.Text"/> at the caret.</summary>
    Insert,

    /// <summary>
    /// Replace the character immediately before the caret with
    /// <see cref="LigatureAction.Text"/>.
    /// </summary>
    ReplacePrevious
}

/// <summary>
/// The result of a ligature-buffer transition.
/// </summary>
public readonly struct LigatureAction
{
    private LigatureAction(
        LigatureActionKind kind,
        string text,
        char? expectedPrevious,
        string fallback)
    {
        Kind = kind;
        Text = text;
        ExpectedPrevious = expectedPrevious;
        Fallback = fallback;
    }

    /// <summary>Gets the action to perform.</summary>
    public LigatureActionKind Kind { get; }

    /// <summary>Gets the text to insert or use as a replacement.</summary>
    public string Text { get; }

    /// <summary>
    /// Gets the character that must currently precede the caret for a
    /// <see cref="LigatureActionKind.ReplacePrevious"/> action to be valid.
    /// </summary>
    public char? ExpectedPrevious { get; }

    /// <summary>
    /// Gets the text to insert when a replacement cannot be performed because
    /// the caret moved or the previous character changed.
    /// </summary>
    public string Fallback { get; }

    /// <summary>Creates a no-op action.</summary>
    public static LigatureAction None() => new(LigatureActionKind.None, string.Empty, null, string.Empty);

    /// <summary>Creates an insertion action.</summary>
    /// <param name="text">Text to insert at the caret.</param>
    public static LigatureAction Insert(string text) =>
        new(LigatureActionKind.Insert, text, null, string.Empty);

    /// <summary>Creates a replace-previous action.</summary>
    /// <param name="text">Text that replaces the previous character.</param>
    /// <param name="expectedPrevious">The character that must precede the caret.</param>
    /// <param name="fallback">Text to insert if the replacement is not possible.</param>
    public static LigatureAction Replace(string text, char expectedPrevious, string fallback) =>
        new(LigatureActionKind.ReplacePrevious, text, expectedPrevious, fallback);
}

/// <summary>
/// A small state machine that turns "consonant + hasanta + consonant" into a
/// single composed conjunct code, while still allowing a lone hasanta.
/// </summary>
/// <remarks>
/// <para>State transitions:</para>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Normal</b> — the last typed character is tracked so a following
///       hasanta can find its base consonant.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>PendingHasanta</b> — entered when the halant key (G) is pressed.
///       Nothing is inserted yet. The caret stays after the previous consonant.
///     </description>
///   </item>
///   <item>
///     <description>
///       From <b>PendingHasanta</b>, a consonant either replaces the previous
///       consonant with a composed conjunct, or appends "hasanta + consonant"
///       when no composed form exists. A vowel sign after a stand-alone hasanta
///       produces an independent vowel (G then F = আ, G then D = ই, ...).
///       Any other key flushes the hasanta first. Backspace cancels the
///       pending state without deleting the consonant.
///     </description>
///   </item>
/// </list>
/// <para>
/// This class is deliberately UI-free: it returns a <see cref="LigatureAction"/>
/// and the input handler applies it to the text box.
/// </para>
/// </remarks>
public sealed class LigatureBuffer
{
    /// <summary>The number of pending-hasanta transitions documented above.</summary>
    private bool _pending;
    private char? _lastConsonant;

    /// <summary>Gets a value indicating whether a hasanta is waiting for the next key.</summary>
    public bool IsPending => _pending;

    /// <summary>
    /// Gets a value indicating whether the last key was a consonant that can
    /// still be used as the first half of a conjunct or independent vowel.
    /// </summary>
    public bool HasOpenConsonant => _lastConsonant.HasValue;

    /// <summary>
    /// Gets the consonant that will be joined when the pending hasanta is
    /// resolved, or <c>null</c> when the hasanta started with no base.
    /// </summary>
    public char? PendingBase => _pending ? _lastConsonant : null;

    /// <summary>Clears all state without inserting anything.</summary>
    public void Reset()
    {
        _pending = false;
        _lastConsonant = null;
    }

    /// <summary>
    /// Forgets the last consonant (for example after the caret moved), while
    /// keeping any pending state untouched.
    /// </summary>
    public void ForgetBase()
    {
        if (!_pending)
        {
            _lastConsonant = null;
        }
    }

    /// <summary>
    /// Handles the hasanta key.
    /// </summary>
    /// <returns>
    /// A no-op when the hasanta can be buffered; an insertion of a second
    /// hasanta when one is already pending.
    /// </returns>
    public LigatureAction PressHalanta()
    {
        if (_pending)
        {
            // "G G" is not a valid cluster; flush one visible hasanta.
            return LigatureAction.Insert(BijoyMappingTable.Halanta.ToString());
        }

        _pending = true;
        return LigatureAction.None();
    }

    /// <summary>
    /// Handles a consonant, either starting a new character or completing a
    /// conjunct.
    /// </summary>
    /// <param name="second">The legacy code of the consonant that was pressed.</param>
    /// <returns>The text-box action to perform.</returns>
    public LigatureAction PressConsonant(char second)
    {
        if (!_pending)
        {
            _lastConsonant = second;
            return LigatureAction.Insert(second.ToString());
        }

        _pending = false;

        if (_lastConsonant is char first &&
            BijoyMappingTable.TryGetConjunct(first, second, out string conjunct))
        {
            // The first consonant is already in the text box; replace it with
            // the composed conjunct code.
            _lastConsonant = null;
            string fallback = BijoyMappingTable.Halanta.ToString() + second;
            return LigatureAction.Replace(conjunct, first, fallback);
        }

        // No composed form: keep the first consonant and append hasanta + second.
        // The cluster is now "closed"; a further hasanta starts a fresh one.
        _lastConsonant = null;
        return LigatureAction.Insert(BijoyMappingTable.Halanta.ToString() + second);
    }

    /// <summary>
    /// Handles a dependent vowel sign. When it follows a stand-alone hasanta,
    /// the sign is turned into the matching independent vowel.
    /// </summary>
    /// <param name="sign">The legacy vowel-sign code.</param>
    /// <returns>The text-box action to perform.</returns>
    public LigatureAction PressVowelSign(char sign)
    {
        if (!_pending)
        {
            _lastConsonant = null;
            return LigatureAction.Insert(sign.ToString());
        }

        _pending = false;

        // A vowel sign after a stand-alone hasanta becomes the matching
        // independent vowel. This is also the only way to type an independent
        // vowel that follows a consonant (for example the ই in বলেই), so the
        // previous consonant must not swallow the sign into a fake cluster.
        if (BijoyMappingTable.TryGetIndependentVowel(sign, out string independent))
        {
            _lastConsonant = null;
            return LigatureAction.Insert(independent);
        }

        _lastConsonant = null;
        return LigatureAction.Insert(BijoyMappingTable.Halanta.ToString() + sign);
    }

    /// <summary>
    /// Handles any other key (space, digit, punctuation, independent vowel...).
    /// A pending hasanta is flushed first.
    /// </summary>
    /// <param name="text">The text the key produces.</param>
    /// <returns>The text-box action to perform.</returns>
    public LigatureAction PressOther(string text)
    {
        _lastConsonant = null;

        if (!_pending)
        {
            return LigatureAction.Insert(text);
        }

        _pending = false;
        return LigatureAction.Insert(BijoyMappingTable.Halanta.ToString() + text);
    }

    /// <summary>
    /// Cancels a pending hasanta, typically from Backspace. The already-typed
    /// consonant is left untouched.
    /// </summary>
    /// <returns><c>true</c> when a pending hasanta was cancelled.</returns>
    public bool CancelPending()
    {
        if (!_pending)
        {
            return false;
        }

        _pending = false;
        return true;
    }
}
