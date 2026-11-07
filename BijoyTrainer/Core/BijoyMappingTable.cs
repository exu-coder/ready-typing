using System;
using System.Collections.Generic;

namespace BijoyTrainer.Core;

/// <summary>
/// The complete Bijoy Classic physical-key to SutonnyMJ (legacy ANSI) mapping,
/// together with the conjunct table used by the ligature state machine.
/// </summary>
/// <remarks>
/// <para>
/// <b>Mapping source.</b> The Bengali glyph slots were verified against the glyphs
/// inside the bundled SutonnyMJ.ttf and cross-checked with the published
/// Unicode-to-Bijoy ANSI chart (BanglaTools, 221 entries) and the standard
/// Bijoy 52 physical layout (J = ক, K = ত, L = দ, H = ব, N = স, M = ম, ...).
/// The font is a legacy ANSI font: the code point that is stored is what the
/// font paints. For example the physical key <c>j</c> is stored as <c>K</c>,
/// which SutonnyMJ paints as ক, and <c>Shift+A</c> is stored as <c>©</c>,
/// which paints the reph mark.
/// </para>
/// <para>
/// This class has no dependency on Windows Forms. It only knows about
/// characters, so it can be unit-tested on its own.
/// </para>
/// </remarks>
public static class BijoyMappingTable
{
    /// <summary>The SutonnyMJ code point that paints the hasanta (্).</summary>
    public const char Halanta = '&';

    /// <summary>The SutonnyMJ code point that paints the Bengali full stop (।).</summary>
    public const char Dari = '|';

    /// <summary>The SutonnyMJ code point that paints the standard e-kar (ে).</summary>
    public const char Ekar = '‡';

    /// <summary>
    /// The compact e-kar variant without the left overhang. It is used when the
    /// sign follows a zero-width mark such as the ra-phala, where the standard
    /// form would drift over the previous cluster.
    /// </summary>
    public const char EkarTight = '†';

    /// <summary>The SutonnyMJ code point that paints a reph (র্).</summary>
    public const char Reph = '©';

    /// <summary>The SutonnyMJ code point that paints the ra-phala (্র).</summary>
    public const char RaPhala = '«';

    /// <summary>The SutonnyMJ code point that paints the ya-phala (্য).</summary>
    public const char YaPhala = '¨';

    /// <summary>
    /// Maps a physical letter plus Shift state to the SutonnyMJ code point.
    /// The key is always a lower-case letter.
    /// </summary>
    private static readonly Dictionary<(char Key, bool Shift), char> LetterMapInternal = new()
    {
        // Home row: vowel signs and the most common consonants.
        [('a', false)] = '…', // ৃ  (ri-kar)
        [('a', true)] = Reph, // ©  (reph)
        [('s', false)] = 'y', // ু
        [('s', true)] = '~',  // ূ
        [('d', false)] = 'w', // ি  (typed before the consonant)
        [('d', true)] = 'x',  // ী
        [('f', false)] = 'v', // া
        [('f', true)] = 'A',  // অ
        [('g', false)] = Halanta, // ্
        [('g', true)] = Dari,     // ।
        [('h', false)] = 'e', // ব
        [('h', true)] = 'f',  // ভ
        [('j', false)] = 'K', // ক
        [('j', true)] = 'L',  // খ
        [('k', false)] = 'Z', // ত
        [('k', true)] = '_',  // থ
        [('l', false)] = '`', // দ
        [('l', true)] = 'a',  // ধ

        // Top row.
        [('q', false)] = 'O', // ঙ
        [('q', true)] = 's',  // ং
        [('w', false)] = 'h', // য
        [('w', true)] = 'q',  // য়
        [('e', false)] = 'W', // ড
        [('e', true)] = 'X',  // ঢ
        [('r', false)] = 'c', // প
        [('r', true)] = 'd',  // ফ
        [('t', false)] = 'U', // ট
        [('t', true)] = 'V',  // ঠ
        [('y', false)] = 'P', // চ
        [('y', true)] = 'Q',  // ছ
        [('u', false)] = 'R', // জ
        [('u', true)] = 'S',  // ঝ
        [('i', false)] = 'n', // হ
        [('i', true)] = 'T',  // ঞ
        [('o', false)] = 'M', // গ
        [('o', true)] = 'N',  // ঘ
        [('p', false)] = 'o', // ড়
        [('p', true)] = 'p',  // ঢ়

        // Bottom row.
        [('z', false)] = RaPhala, // ্র
        [('z', true)] = YaPhala,  // ্য
        [('x', false)] = 'I',     // ও
        [('x', true)] = 'Š',      // ৗ
        [('c', false)] = Ekar,    // ে  (typed before the consonant)
        [('c', true)] = '‰',      // ৈ
        [('v', false)] = 'i',     // র
        [('v', true)] = 'j',      // ল
        [('b', false)] = 'b',     // ন
        [('b', true)] = 'Y',      // ণ
        [('n', false)] = 'm',     // স
        [('n', true)] = 'l',      // ষ
        [('m', false)] = 'g',     // ম
        [('m', true)] = 'k'       // শ
    };

    /// <summary>
    /// Maps non-letter physical characters (digits, punctuation and the extra
    /// marks on the number row) to SutonnyMJ code points.
    /// </summary>
    private static readonly Dictionary<char, char> SymbolMapInternal = new()
    {
        [' '] = ' ',
        ['0'] = '0', ['1'] = '1', ['2'] = '2', ['3'] = '3', ['4'] = '4',
        ['5'] = '5', ['6'] = '6', ['7'] = '7', ['8'] = '8', ['9'] = '9',

        ['!'] = '!', ['@'] = '@', ['#'] = '#', ['$'] = '$', ['%'] = '%',
        ['^'] = '^', ['*'] = '*', ['('] = '(', [')'] = ')',

        // Shift+7 is the chandrabindu key in the Bijoy layout.
        ['&'] = 'u',

        ['-'] = '-', ['_'] = '-', ['='] = '=', ['+'] = '+',
        [','] = ',', ['<'] = '<', ['.'] = '.', ['>'] = '>',
        ['/'] = '/', ['?'] = '?', [';'] = ';', [':'] = ':',
        ['\''] = '\'', ['"'] = '"',
        ['['] = '[', ['{'] = '{', [']'] = ']', ['}'] = '}',

        // The backslash key carries ৎ and ঃ in the Bijoy layout.
        ['\\'] = 'r', // ৎ
        ['|'] = 't'   // ঃ
    };

    /// <summary>
    /// Common two-consonant conjuncts and the single or short legacy code that
    /// SutonnyMJ paints as the joined cluster. Composed codes keep the text
    /// compact and match what the real Bijoy keyboard produces.
    /// </summary>
    private static readonly Dictionary<(char First, char Second), string> ConjunctMapInternal = new()
    {
        // ক
        [('K', 'K')] = "°", [('K', 'U')] = "±", [('K', 'Z')] = "³",
        [('K', 'i')] = "µ", [('K', 'l')] = "¶", [('K', 'm')] = "·",
        [('K', 'e')] = "K¡", [('K', 'j')] = "K¬",

        // গ / ঙ
        [('M', 'a')] = "»", [('M', 'b')] = "Mœ", [('M', 'g')] = "M¥", [('M', 'j')] = "M­",
        [('O', 'K')] = "¼", [('O', 'M')] = "½",
        [('O', 'L')] = "•L", [('O', 'N')] = "•N",

        // জ / ঞ
        [('R', 'R')] = "¾", [('R', 'S')] = "À", [('R', 'T')] = "Á", [('R', 'e')] = "R¡",
        [('T', 'P')] = "Â", [('T', 'Q')] = "Ã", [('T', 'R')] = "Ä", [('T', 'S')] = "Å",

        // ট / ড / ণ
        [('U', 'U')] = "Æ", [('U', 'e')] = "U¡", [('U', 'g')] = "U¥",
        [('W', 'W')] = "Ç",
        [('Y', 'U')] = "È", [('Y', 'V')] = "É", [('Y', 'W')] = "Ê", [('Y', 'e')] = "Y^",

        // ত / থ / দ / ধ
        [('Z', 'Z')] = "Ë", [('Z', '_')] = "Ì", [('Z', 'i')] = "Î",
        [('Z', 'b')] = "Zœ", [('Z', 'g')] = "Z¥", [('Z', 'e')] = "Z¡",
        [('_', 'e')] = "_¡",
        [('`', '`')] = "Ï", [('`', 'a')] = "×", [('`', 'e')] = "Ø", [('`', 'g')] = "Ù",
        [('`', 'M')] = "\u02DCM", [('`', 'N')] = "\u02DCN", [('`', 'f')] = "\u2122¢",
        [('a', 'e')] = "aŸ", [('a', 'g')] = "a¥",

        // ন
        [('b', 'U')] = "›U", [('b', 'V')] = "Ú", [('b', 'W')] = "Û",
        [('b', 'Z')] = "š—", [('b', '_')] = "š’", [('b', '`')] = "›`", [('b', 'a')] = "Ü",
        [('b', 'b')] = "bœ", [('b', 'e')] = "š^", [('b', 'g')] = "b¥",

        // প / ফ
        [('c', 'U')] = "Þ", [('c', 'Z')] = "ß", [('c', 'b')] = "cœ",
        [('c', 'c')] = "à", [('c', 'j')] = "c­", [('c', 'm')] = "á",
        [('d', 'j')] = "d¬",

        // ব / ভ / ম
        [('e', 'R')] = "â", [('e', '`')] = "ã", [('e', 'a')] = "ä",
        [('e', 'e')] = "eŸ", [('e', 'j')] = "e­",
        [('f', 'i')] = "å",
        [('g', 'b')] = "gœ", [('g', 'c')] = "¤ú", [('g', 'd')] = "ç",
        [('g', 'e')] = "¤^", [('g', 'f')] = "¤¢", [('g', 'g')] = "¤§", [('g', 'j')] = "¤­",

        // ল / শ / ষ
        [('j', 'K')] = "é", [('j', 'M')] = "ê", [('j', 'U')] = "ë", [('j', 'W')] = "ì",
        [('j', 'c')] = "í", [('j', 'd')] = "î",
        [('j', 'e')] = "j¦", [('j', 'g')] = "j¥", [('j', 'j')] = "jø",
        [('k', 'P')] = "ð", [('k', 'b')] = "kœ", [('k', 'e')] = "k¦",
        [('k', 'g')] = "k¥", [('k', 'j')] = "kø",
        [('l', 'U')] = "ó", [('l', 'V')] = "ô", [('l', 'Y')] = "ò", [('l', 'd')] = "õ",

        // স / হ
        [('m', 'K')] = "¯‹", [('m', 'U')] = "÷", [('m', 'L')] = "ö",
        [('m', 'Z')] = "¯—", [('m', '_')] = "¯’", [('m', 'b')] = "mœ",
        [('m', 'd')] = "ù", [('m', 'e')] = "¯^", [('m', 'g')] = "¯§",
        [('n', 'Y')] = "nè", [('n', 'b')] = "ý", [('n', 'g')] = "þ",
        [('n', 'j')] = "n¬"
    };

    private static readonly HashSet<char> VowelSigns = new()
    {
        'v', 'w', 'x', 'y', '~', '…', '‡', '‰', 'Š'
    };

    /// <summary>
    /// Independent vowels that are typed as hasanta + a vowel sign. The letters
    /// with a dedicated key (অ and ও) are not in this table.
    /// </summary>
    private static readonly Dictionary<char, char> IndependentVowelSignsInternal = new()
    {
        ['B'] = 'w', // ই
        ['C'] = 'x', // ঈ
        ['D'] = 'y', // উ
        ['E'] = '~', // ঊ
        ['F'] = '…', // ঋ
        ['G'] = '‡', // এ
        ['H'] = '‰', // ঐ
        ['J'] = 'Š'  // ঔ
    };

    private static readonly Dictionary<char, (char Key, bool Shift)> ReverseMapInternal = BuildReverseMap();
    private static readonly Dictionary<string, (char First, char Second)> ConjunctReverseInternal = BuildConjunctReverseMap();
    private static readonly Dictionary<char, (char First, char Second)> ConjunctPrefixInternal = BuildConjunctPrefixMap();

    /// <summary>
    /// Gets the read-only physical-letter mapping. Included for tools, tests and
    /// the on-screen keyboard.
    /// </summary>
    public static IReadOnlyDictionary<(char Key, bool Shift), char> LetterMap => LetterMapInternal;

    /// <summary>
    /// Gets the read-only physical-symbol mapping.
    /// </summary>
    public static IReadOnlyDictionary<char, char> SymbolMap => SymbolMapInternal;

    /// <summary>
    /// Resolves a physical key character to its SutonnyMJ output code point.
    /// </summary>
    /// <param name="physical">
    /// The character supplied by <see cref="System.Windows.Forms.KeyPressEventArgs.KeyChar"/>
    /// (for example <c>j</c>, <c>J</c>, <c>1</c> or <c>&amp;</c>).
    /// </param>
    /// <param name="shift">Whether Shift (not Caps Lock) is held.</param>
    /// <param name="sutonny">The legacy code point to store.</param>
    /// <returns><c>true</c> when the key has a mapping.</returns>
    public static bool TryMap(char physical, bool shift, out char sutonny)
    {
        if (char.IsLetter(physical))
        {
            return LetterMapInternal.TryGetValue((char.ToLowerInvariant(physical), shift), out sutonny);
        }

        return SymbolMapInternal.TryGetValue(physical, out sutonny);
    }

    /// <summary>
    /// Determines whether a SutonnyMJ code point is a consonant glyph.
    /// </summary>
    /// <param name="code">The legacy code point.</param>
    /// <returns><c>true</c> for consonants (including ড়, ঢ় and য়).</returns>
    public static bool IsConsonant(char code)
    {
        return (code is (>= 'K' and <= 'Z') or (>= 'a' and <= 'q')) || code == '`';
    }

    /// <summary>
    /// Determines whether a SutonnyMJ code point is a dependent vowel sign.
    /// </summary>
    /// <param name="code">The legacy code point.</param>
    /// <returns><c>true</c> for া, ি, ী, ু, ূ, ৃ, ে, ৈ and ৗ.</returns>
    public static bool IsVowelSign(char code) => VowelSigns.Contains(code);

    /// <summary>
    /// Gets the independent vowel produced by hasanta + a vowel-sign key.
    /// </summary>
    /// <param name="vowelSign">The vowel sign that follows the hasanta key.</param>
    /// <param name="vowel">The independent vowel's SutonnyMJ code(s).</param>
    /// <returns><c>true</c> when the combination is defined.</returns>
    public static bool TryGetIndependentVowel(char vowelSign, out string vowel)
    {
        vowel = vowelSign switch
        {
            'v' => "Av", // আ = অ + া
            'w' => "B",  // ই
            'x' => "C",  // ঈ
            'y' => "D",  // উ
            '~' => "E",  // ঊ
            '…' => "F",  // ঋ
            '‡' => "G",  // এ
            '‰' => "H",  // ঐ
            'Š' => "J",  // ঔ
            _ => string.Empty
        };

        return vowel.Length > 0;
    }

    /// <summary>
    /// Gets the vowel sign that, typed after the hasanta key, produces an
    /// independent vowel.
    /// </summary>
    /// <param name="vowel">The independent vowel's legacy code point.</param>
    /// <param name="sign">The matching dependent vowel sign.</param>
    /// <returns><c>true</c> when the vowel is formed with hasanta + sign.</returns>
    public static bool TryGetIndependentVowelSign(char vowel, out char sign)
    {
        return IndependentVowelSignsInternal.TryGetValue(vowel, out sign);
    }

    /// <summary>
    /// Looks up a composed conjunct for two consonants.
    /// </summary>
    /// <param name="first">Legacy code of the first consonant.</param>
    /// <param name="second">Legacy code of the second consonant.</param>
    /// <param name="conjunct">The combined legacy code(s).</param>
    /// <returns><c>true</c> when a composed form is available.</returns>
    public static bool TryGetConjunct(char first, char second, out string conjunct)
    {
        return ConjunctMapInternal.TryGetValue((first, second), out conjunct!);
    }

    /// <summary>
    /// Finds the two consonants that make up a composed conjunct code.
    /// </summary>
    /// <param name="conjunct">A single composed conjunct code.</param>
    /// <param name="first">The first consonant code.</param>
    /// <param name="second">The second consonant code.</param>
    /// <returns><c>true</c> when the code is a known composed conjunct.</returns>
    public static bool TryGetConjunctParts(string conjunct, out char first, out char second)
    {
        if (ConjunctReverseInternal.TryGetValue(conjunct, out var parts))
        {
            first = parts.First;
            second = parts.Second;
            return true;
        }

        first = default;
        second = default;
        return false;
    }

    /// <summary>
    /// Gets the read-only composed-conjunct lookup keyed by the legacy text.
    /// Multi-character values are the reason the trainer tokenises the target
    /// instead of assuming one glyph per physical key.
    /// </summary>
    public static IReadOnlyDictionary<string, (char First, char Second)> ComposedConjuncts =>
        ConjunctReverseInternal;

    /// <summary>
    /// Resolves the next key of a multi-character composed conjunct while its
    /// first consonant is already pending. This lets the on-screen hint point
    /// at the joining consonant instead of repeating the base key.
    /// </summary>
    /// <param name="target">The full target text.</param>
    /// <param name="index">Index of the first character of the cluster.</param>
    /// <param name="pendingBase">The consonant that has just been typed.</param>
    /// <param name="second">The second consonant of the matched conjunct.</param>
    /// <returns><c>true</c> when a composed conjunct starts at the index.</returns>
    public static bool TryGetConjunctContinuation(
        string target,
        int index,
        char pendingBase,
        out char second)
    {
        second = default;

        if (index < 0 || index >= target.Length)
        {
            return false;
        }

        foreach (var pair in ConjunctReverseInternal)
        {
            string value = pair.Key;
            if (value.Length <= 1 ||
                value[0] != target[index] ||
                pair.Value.First != pendingBase ||
                index + value.Length > target.Length)
            {
                continue;
            }

            bool matches = true;
            for (int i = 1; i < value.Length; i++)
            {
                if (target[index + i] != value[i])
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                second = pair.Value.Second;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds the physical key that produces a SutonnyMJ code point.
    /// </summary>
    /// <param name="sutonny">The legacy code point.</param>
    /// <param name="key">The physical key (lower-case for letters).</param>
    /// <param name="shift">Whether Shift is required.</param>
    /// <returns><c>true</c> when a direct key exists.</returns>
    public static bool TryGetPhysicalKey(char sutonny, out char key, out bool shift)
    {
        if (ReverseMapInternal.TryGetValue(sutonny, out var mapping))
        {
            key = mapping.Key;
            shift = mapping.Shift;
            return true;
        }

        key = default;
        shift = false;
        return false;
    }

    /// <summary>
    /// Finds the first physical key required to produce a target character.
    /// Conjuncts report the key of their first consonant.
    /// </summary>
    /// <param name="sutonny">A target legacy code point.</param>
    /// <param name="key">The physical key.</param>
    /// <param name="shift">Whether Shift is required.</param>
    /// <returns><c>true</c> when a key could be determined.</returns>
    public static bool TryGetNextKey(char sutonny, out char key, out bool shift)
    {
        if (sutonny == ' ')
        {
            key = ' ';
            shift = false;
            return true;
        }

        if (TryGetPhysicalKey(sutonny, out key, out shift))
        {
            return true;
        }

        if (TryGetConjunctParts(sutonny.ToString(), out char first, out _) &&
            TryGetPhysicalKey(first, out key, out shift))
        {
            return true;
        }

        if (ConjunctPrefixInternal.TryGetValue(sutonny, out var parts) &&
            TryGetPhysicalKey(parts.First, out key, out shift))
        {
            return true;
        }

        // Independent vowels without their own key are typed as hasanta + the
        // matching vowel sign (for example G then D produces ই).
        if (IndependentVowelSignsInternal.ContainsKey(sutonny))
        {
            key = 'g';
            shift = false;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Formats a human-readable key name, for example <c>Shift + J</c>.
    /// </summary>
    /// <param name="key">The physical key.</param>
    /// <param name="shift">Whether Shift is required.</param>
    /// <returns>A short label suitable for the UI.</returns>
    public static string FormatKey(char key, bool shift)
    {
        if (key == ' ')
        {
            return "Space";
        }

        string label = key switch
        {
            '\\' => "\\",
            '\'' => "'",
            _ => char.ToUpperInvariant(key).ToString()
        };

        return shift ? "Shift + " + label : label;
    }

    /// <summary>
    /// Produces a hint such as <c>J  →  G  →  K</c> for the next target glyph.
    /// </summary>
    /// <param name="sutonny">The next target legacy code point.</param>
    /// <returns>A human-readable key sequence, or <c>?</c>.</returns>
    public static string GetKeyHint(char sutonny)
    {
        if (sutonny == ' ')
        {
            return "Space";
        }

        if (TryGetConjunctParts(sutonny.ToString(), out char first, out char second))
        {
            string a = DescribeConsonant(first);
            string b = DescribeConsonant(second);
            return $"{a}  →  G  →  {b}";
        }

        if (TryGetPhysicalKey(sutonny, out char key, out bool shift))
        {
            return FormatKey(key, shift);
        }

        if (ConjunctPrefixInternal.TryGetValue(sutonny, out var parts))
        {
            return $"{DescribeConsonant(parts.First)}  →  G  →  {DescribeConsonant(parts.Second)}";
        }

        if (IndependentVowelSignsInternal.TryGetValue(sutonny, out char vowelSign) &&
            TryGetPhysicalKey(vowelSign, out char signKey, out bool signShift))
        {
            return $"G  →  {FormatKey(signKey, signShift)}";
        }

        return "?";
    }

    private static string DescribeConsonant(char code)
    {
        return TryGetPhysicalKey(code, out char key, out bool shift)
            ? FormatKey(key, shift)
            : "?";
    }

    private static Dictionary<char, (char Key, bool Shift)> BuildReverseMap()
    {
        var reverse = new Dictionary<char, (char Key, bool Shift)>();

        foreach (var pair in LetterMapInternal)
        {
            reverse.TryAdd(pair.Value, (pair.Key.Key, pair.Key.Shift));
        }

        // Hasanta is produced by G but is not a "symbol" key output.
        reverse.TryAdd(Halanta, ('g', false));

        // The compact e-kar shares the ে key with the standard form.
        reverse.TryAdd(EkarTight, ('c', false));

        foreach (var pair in SymbolMapInternal)
        {
            reverse.TryAdd(pair.Value, (pair.Key, false));
        }

        // Space is produced by the space bar.
        reverse[' '] = (' ', false);

        return reverse;
    }

    private static Dictionary<string, (char First, char Second)> BuildConjunctReverseMap()
    {
        var reverse = new Dictionary<string, (char First, char Second)>(StringComparer.Ordinal);

        foreach (var pair in ConjunctMapInternal)
        {
            reverse.TryAdd(pair.Value, (pair.Key.First, pair.Key.Second));
        }

        return reverse;
    }

    private static Dictionary<char, (char First, char Second)> BuildConjunctPrefixMap()
    {
        var prefixes = new Dictionary<char, (char First, char Second)>();

        foreach (var pair in ConjunctMapInternal)
        {
            if (pair.Value.Length > 1)
            {
                prefixes.TryAdd(pair.Value[0], (pair.Key.First, pair.Key.Second));
            }
        }

        return prefixes;
    }
}
