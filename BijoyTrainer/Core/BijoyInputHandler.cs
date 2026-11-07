namespace BijoyTrainer.Core;

/// <summary>
/// Translates physical key presses into SutonnyMJ text edits, including
/// conjunct composition. This type is intentionally free of any Windows Forms
/// reference so the Core folder remains testable and UI-independent.
/// </summary>
/// <remarks>
/// <para>
/// The thin WinForms event wiring lives in <c>MainForm</c>: it forwards
/// <c>KeyPress</c> characters to <see cref="TryProcessKey"/> and applies the
/// returned <see cref="LigatureAction"/> to the input <c>TextBox</c>. Backspace
/// is forwarded to <see cref="ProcessBackspace"/> so a pending hasanta can be
/// cancelled before the text box deletes a character.
/// </para>
/// <para>
/// Shift state must come from <c>Control.ModifierKeys</c> rather than
/// <c>char.IsUpper</c>, because Caps Lock also upper-cases <c>KeyChar</c> while
/// the Bijoy layout expects the unshifted character in that case.
/// </para>
/// </remarks>
public sealed class BijoyInputHandler
{
    private readonly LigatureBuffer _buffer = new();

    /// <summary>Gets the ligature state machine used by this handler.</summary>
    public LigatureBuffer Buffer => _buffer;

    /// <summary>Gets a value indicating whether a hasanta is pending.</summary>
    public bool IsPending => _buffer.IsPending;

    /// <summary>Clears all buffered state, for example when a new lesson starts.</summary>
    public void Reset() => _buffer.Reset();

    /// <summary>Forgets the tracked base consonant after the caret moves.</summary>
    public void ForgetBase() => _buffer.ForgetBase();

    /// <summary>
    /// Translates one printable key press.
    /// </summary>
    /// <param name="keyChar">
    /// The character from <c>KeyPressEventArgs.KeyChar</c>. For letters this is
    /// the shifted or unshifted Latin character (for example <c>j</c> or
    /// <c>J</c>).
    /// </param>
    /// <param name="shift">Whether a physical Shift key is held.</param>
    /// <param name="action">The edit the caller must apply to the text box.</param>
    /// <returns>
    /// <c>true</c> when the key is part of the Bijoy layout (the caller should
    /// mark the event handled even if the action is a no-op); <c>false</c> for
    /// keys the trainer does not consume.
    /// </returns>
    public bool TryProcessKey(char keyChar, bool shift, out LigatureAction action)
    {
        return TryProcessKey(keyChar, shift, null, out action);
    }

    /// <summary>
    /// Translates one printable key press with awareness of the character just
    /// before the caret. The context lets the e-kar pick its compact variant
    /// after a ra-phala, which is the only way the legacy font paints words
    /// such as প্রপোজ without a detached vowel sign.
    /// </summary>
    /// <param name="keyChar">The character from <c>KeyPressEventArgs.KeyChar</c>.</param>
    /// <param name="shift">Whether a physical Shift key is held.</param>
    /// <param name="previous">The character immediately before the caret, if any.</param>
    /// <param name="action">The edit the caller must apply to the text box.</param>
    /// <returns><c>true</c> when the key is part of the Bijoy layout.</returns>
    public bool TryProcessKey(char keyChar, bool shift, char? previous, out LigatureAction action)
    {
        action = LigatureAction.None();

        if (keyChar is '\r' or '\n' or '\t')
        {
            return false;
        }

        if (!BijoyMappingTable.TryMap(keyChar, shift, out char code))
        {
            // Unknown printable key. The caller may swallow it so that the
            // legacy font never paints a stray Latin-looking glyph.
            return false;
        }

        if (code == BijoyMappingTable.Halanta)
        {
            action = _buffer.PressHalanta();
            return true;
        }

        if (BijoyMappingTable.IsConsonant(code))
        {
            action = _buffer.PressConsonant(code);
            return true;
        }

        if (BijoyMappingTable.IsVowelSign(code))
        {
            if (code == BijoyMappingTable.Ekar && previous == BijoyMappingTable.RaPhala)
            {
                code = BijoyMappingTable.EkarTight;
            }

            action = _buffer.PressVowelSign(code);
            return true;
        }

        action = _buffer.PressOther(code.ToString());
        return true;
    }

    /// <summary>
    /// Handles Backspace. When a hasanta is pending it is cancelled and the
    /// caller must suppress the deletion; otherwise the text box should delete
    /// normally.
    /// </summary>
    /// <returns><c>true</c> when the key press was consumed by the buffer.</returns>
    public bool ProcessBackspace()
    {
        if (_buffer.CancelPending())
        {
            return true;
        }

        _buffer.ForgetBase();
        return false;
    }
}
