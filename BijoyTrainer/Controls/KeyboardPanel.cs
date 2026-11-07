using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using BijoyTrainer.Core;

namespace BijoyTrainer.Controls;

/// <summary>
/// Draws a compact Bijoy keyboard with finger-zone colours, Bengali legends and
/// a pulsing highlight on the key the learner should press next.
/// </summary>
public class KeyboardPanel : Control
{
    private static readonly Color Accent = Color.FromArgb(79, 70, 229);
    private static readonly Color Ink = Color.FromArgb(30, 27, 75);
    private static readonly Color MutedInk = Color.FromArgb(70, 68, 96);

    private static readonly Color Pinky = Color.FromArgb(249, 168, 212);
    private static readonly Color Ring = Color.FromArgb(252, 211, 77);
    private static readonly Color Middle = Color.FromArgb(134, 239, 172);
    private static readonly Color Index = Color.FromArgb(147, 197, 253);
    private static readonly Color Thumb = Color.FromArgb(228, 230, 243);

    private static readonly string[][] Rows =
    {
        new[] { "`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" },
        new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\" },
        new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'" },
        new[] { "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/" },
        new[] { "Shift", " ", "Shift" }
    };

    private readonly Font _latinFont = new("Times New Roman", 7f, FontStyle.Bold);
    private readonly Font _spaceFont = new("Times New Roman", 8f, FontStyle.Bold);

    private Font? _glyphFont;
    private Font? _shiftFont;
    private float _glyphFontSize;

    private Font? _bengaliFont;
    private char? _nextKey;
    private bool _nextShift;
    private bool _shiftHeld;
    private bool _pointToShift;
    private double _pulse;

    /// <summary>Initializes a new instance of the <see cref="KeyboardPanel"/> class.</summary>
    public KeyboardPanel()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.Transparent;
        MinimumSize = new Size(400, 140);
    }

    /// <summary>Gets or sets the legacy Bengali font used for the key legends.</summary>
    public Font? BengaliFont
    {
        get => _bengaliFont;
        set
        {
            _bengaliFont = value;
            ResetGlyphFonts();
            Invalidate();
        }
    }

    /// <summary>Gets or sets the physical key to highlight.</summary>
    public char? NextKey
    {
        get => _nextKey;
        set { _nextKey = value; Invalidate(); }
    }

    /// <summary>Gets or sets whether the highlighted key requires Shift.</summary>
    public bool NextShift
    {
        get => _nextShift;
        set { _nextShift = value; Invalidate(); }
    }

    /// <summary>Gets or sets whether a physical Shift key is currently held.</summary>
    public bool ShiftHeld
    {
        get => _shiftHeld;
        set
        {
            if (_shiftHeld == value)
            {
                return;
            }

            _shiftHeld = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the pointing hand should guide
    /// the learner to the Shift key first (used while Shift is not held).
    /// </summary>
    public bool PointToShift
    {
        get => _pointToShift;
        set
        {
            if (_pointToShift == value)
            {
                return;
            }

            _pointToShift = value;
            Invalidate();
        }
    }

    /// <summary>Gets or sets a 0..1 pulse value used for the highlight glow.</summary>
    public double Pulse
    {
        get => _pulse;
        set
        {
            _pulse = Math.Clamp(value, 0d, 1d);
            if (_nextKey is not null)
            {
                Invalidate();
            }
        }
    }

    /// <summary>Clears the next-key highlight.</summary>
    public void ClearHighlight()
    {
        _nextKey = null;
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.Clear(Parent?.BackColor ?? Color.White);

        if (Width < 100 || Height < 80)
        {
            return;
        }

        const float sidePad = 2f;
        const float gap = 5f;
        // Headroom reserved for the pointing hand so it is never clipped by the
        // top edge of the control (previously the hand was cut off on the top rows).
        const float handRoom = 50f;
        float topPad = handRoom;

        float unitWidth = (Width - sidePad * 2 - gap * 12f) / 13f;
        float keyHeight = (Height - topPad - 4f - gap * 4f) / 5f;
        if (unitWidth <= 2f || keyHeight <= 2f)
        {
            return;
        }

        UpdateGlyphFonts(keyHeight);

        Rectangle? highlightRect = null;
        char? highlightKey = null;
        bool highlightIsShift = false;

        // Hold Shift with the hand that is not typing the next key.
        bool rightShift = _pointToShift &&
                          _nextKey is char targetKey &&
                          DescribeFinger(targetKey).StartsWith("left", StringComparison.Ordinal);

        for (int rowIndex = 0; rowIndex < Rows.Length; rowIndex++)
        {
            string[] row = Rows[rowIndex];
            float rowUnits = 0f;
            foreach (string label in row)
            {
                rowUnits += UnitsFor(label);
            }

            float rowWidth = rowUnits * unitWidth + (row.Length - 1) * gap;
            float x = (Width - rowWidth) / 2f;
            float y = topPad + rowIndex * (keyHeight + gap);

            foreach (string label in row)
            {
                float units = UnitsFor(label);
                float keyWidth = units * unitWidth + (units - 1f) * gap;
                RectangleF rectF = new(x, y, keyWidth, keyHeight);
                Rectangle rect = Rectangle.Round(rectF);

                char physical = label == "Shift"
                    ? '\0'
                    : label.Length > 0
                        ? label[0]
                        : ' ';
                bool isShiftKey = label == "Shift";
                bool isShiftTarget = _pointToShift &&
                                     isShiftKey &&
                                     rightShift == (x >= Width / 2f);
                bool isNext = !_pointToShift &&
                              _nextKey is char next &&
                              CharEquals(next, physical);

                if (isShiftTarget || isNext)
                {
                    highlightRect = rect;
                    highlightKey = physical;
                    highlightIsShift = isShiftKey;
                }
                else
                {
                    DrawKey(graphics, rect, label, physical, false);
                }

                x += keyWidth + gap;
            }
        }

        if (highlightRect is Rectangle highlight && highlightKey is char key)
        {
            DrawHighlightGlow(graphics, highlight);
            DrawKey(
                graphics,
                highlight,
                highlightIsShift ? "Shift" : key == ' ' ? " " : key.ToString(),
                key,
                true);
            DrawHand(graphics, highlight);
        }
    }

    /// <summary>
    /// Draws a small pointing hand above the highlighted key, like the finger
    /// guide in RapidTyping. The hand gently bobs so it is easy to spot.
    /// </summary>
    /// <param name="graphics">Target surface.</param>
    /// <param name="keyRect">The key that should be pressed next.</param>
    private void DrawHand(Graphics graphics, Rectangle keyRect)
    {
        float scale = Math.Clamp(keyRect.Height / 34f, 0.55f, 1.25f);
        // Never let the hand extend past the top of the control.
        scale = Math.Min(scale, Math.Max(0.4f, (keyRect.Top - 2f) / 56f));
        float tipX = keyRect.Left + keyRect.Width / 2f;
        float tipY = keyRect.Top - 3f + (float)(Math.Sin(_pulse * 2d) * 1.6d);

        GraphicsState state = graphics.Save();
        try
        {
            graphics.TranslateTransform(tipX, tipY);
            graphics.ScaleTransform(scale, scale);

            using GraphicsPath hand = CreateHandPath();

            using (var shadow = new SolidBrush(Color.FromArgb(38, 30, 27, 75)))
            {
                graphics.TranslateTransform(1.8f, 2.4f);
                graphics.FillPath(shadow, hand);
                graphics.TranslateTransform(-1.8f, -2.4f);
            }

            using (var cuffBrush = new SolidBrush(Color.FromArgb(67, 56, 202)))
            using (GraphicsPath cuff = CreateRoundedPath(-10f, -56f, 20f, 16f, 5f))
            {
                graphics.FillPath(cuffBrush, cuff);
            }

            using (var skin = new LinearGradientBrush(
                       new RectangleF(-20f, -50f, 40f, 56f),
                       Color.FromArgb(238, 255, 224, 189),
                       Color.FromArgb(238, 244, 155, 96),
                       LinearGradientMode.Vertical))
            {
                graphics.FillPath(skin, hand);
            }

            using (var outline = new Pen(Color.FromArgb(180, 83, 9), 1.5f))
            {
                graphics.DrawPath(outline, hand);
            }

            // Nail and knuckle creases give the mitten shape a readable hand.
            using (var nail = new SolidBrush(Color.FromArgb(255, 244, 233)))
            {
                graphics.FillEllipse(nail, -2.4f, -7.2f, 4.8f, 5.6f);
            }

            using (var crease = new Pen(Color.FromArgb(90, 146, 64, 14), 1.1f))
            {
                graphics.DrawArc(crease, -7f, -34f, 14f, 10f, 200f, 140f);
                graphics.DrawArc(crease, -7f, -41f, 14f, 9f, 200f, 140f);
                graphics.DrawLine(crease, -11.5f, -27f, -5f, -30f);
            }
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private static GraphicsPath CreateHandPath()
    {
        var path = new GraphicsPath();
        path.StartFigure();
        path.AddLine(-4.6f, 0f, -4.6f, -18f);
        path.AddBezier(-4.6f, -18f, -8f, -19.5f, -12f, -19f, -13.8f, -24f);
        path.AddBezier(-13.8f, -24f, -18.5f, -25f, -18.5f, -31.5f, -13.6f, -31.5f);
        path.AddBezier(-13.6f, -31.5f, -13.2f, -38f, -11.5f, -44.5f, -4.5f, -44.8f);
        path.AddLine(-4.5f, -44.8f, 8.5f, -44.8f);
        path.AddBezier(8.5f, -44.8f, 15.5f, -44.5f, 15f, -35f, 14.2f, -27f);
        path.AddBezier(14.2f, -27f, 13.5f, -20.5f, 9f, -17.8f, 4.6f, -17.8f);
        path.AddLine(4.6f, -17.8f, 4.6f, 0f);
        path.AddArc(-4.6f, -4.6f, 9.2f, 9.2f, 0f, 180f);
        path.CloseFigure();
        return path;
    }

    private static GraphicsPath CreateRoundedPath(float x, float y, float width, float height, float radius)
    {
        var path = new GraphicsPath();
        float d = Math.Min(radius * 2f, Math.Min(width, height));
        var arc = new RectangleF(x, y, d, d);

        path.AddArc(arc, 180f, 90f);
        arc.X = x + width - d;
        path.AddArc(arc, 270f, 90f);
        arc.Y = y + height - d;
        path.AddArc(arc, 0f, 90f);
        arc.X = x;
        path.AddArc(arc, 90f, 90f);
        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// Returns a friendly touch-typing finger name for a physical key.
    /// </summary>
    /// <param name="physical">The physical key character.</param>
    /// <returns>For example <c>left index finger</c>.</returns>
    public static string DescribeFinger(char physical)
    {
        char key = char.ToLowerInvariant(physical);

        return key switch
        {
            ' ' => "thumb",
            '`' or '1' or 'q' or 'a' or 'z' => "left pinky finger",
            '2' or 'w' or 's' or 'x' => "left ring finger",
            '3' or 'e' or 'd' or 'c' => "left middle finger",
            '4' or '5' or 'r' or 't' or 'f' or 'g' or 'v' or 'b' => "left index finger",
            '6' or '7' or 'y' or 'u' or 'h' or 'j' or 'n' or 'm' => "right index finger",
            '8' or 'i' or 'k' or ',' => "right middle finger",
            '9' or 'o' or 'l' or '.' => "right ring finger",
            _ => "right pinky finger"
        };
    }

    private void DrawKey(Graphics graphics, Rectangle rect, string label, char physical, bool highlighted)
    {
        int radius = Math.Max(3, Math.Min(8, rect.Height / 5));
        using GraphicsPath path = RoundedPanel.CreateRoundedPath(rect, radius);

        if (label == "Shift")
        {
            DrawShiftKey(graphics, rect, path, highlighted);
            return;
        }

        Color baseColor = physical == ' ' ? Thumb : FingerColor(physical);
        using (var fill = new LinearGradientBrush(
                   rect,
                   Lighten(baseColor, 0.34f),
                   baseColor,
                   LinearGradientMode.Vertical))
        {
            graphics.FillPath(fill, path);
        }

        if (highlighted)
        {
            using var tint = new SolidBrush(Color.FromArgb(36 + (int)(_pulse * 34), Accent.R, Accent.G, Accent.B));
            graphics.FillPath(tint, path);
        }

        using (var border = new Pen(
                   highlighted ? Accent : Color.FromArgb(78, 70, 96, 130),
                   highlighted ? 2.4f : 1f))
        {
            graphics.DrawPath(border, path);
        }

        // Subtle top highlight for a tactile key-cap look.
        using (var gloss = new Pen(Color.FromArgb(highlighted ? 150 : 110, 255, 255, 255), 1f))
        {
            graphics.DrawLine(
                gloss,
                rect.Left + radius,
                rect.Top + 1,
                rect.Right - radius,
                rect.Top + 1);
        }

        if (physical == ' ')
        {
            TextRenderer.DrawText(
                graphics,
                "SPACE",
                _spaceFont,
                rect,
                MutedInk,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            return;
        }

        char? primary = GetBengali(physical, false);
        char? shifted = GetBengali(physical, true);
        if (shifted == primary)
        {
            shifted = null;
        }

        // Reserve space in the top-right corner for the shifted-glyph badge so the
        // latin label never runs underneath it.
        int shiftBadge = shifted is char ? Math.Max(12, (int)(rect.Height * 0.38f)) : 0;
        int latinWidth = Math.Max(8, rect.Width - 10 - (shiftBadge > 0 ? shiftBadge + 4 : 0));

        // Latin key label, top-left.
        TextRenderer.DrawText(
            graphics,
            label.ToUpperInvariant(),
            _latinFont,
            new Rectangle(rect.Left + 5, rect.Top + 2, latinWidth, 12),
            highlighted ? Color.FromArgb(67, 56, 202) : MutedInk,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

        if (primary is char primaryGlyph && _glyphFont is not null && rect.Height >= 20)
        {
            // Keep the Bengali legend clear of the latin label (top 13px) and inside
            // the bottom edge of the key.
            int glyphTop = rect.Top + 13;
            Rectangle glyphRect = new(
                rect.Left + 2,
                glyphTop,
                rect.Width - 4,
                Math.Max(1, rect.Height - 13 - 2));

            using var brush = new SolidBrush(Ink);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoClip
            };

            graphics.DrawString(primaryGlyph.ToString(), _glyphFont, brush, glyphRect, format);
        }

        if (shifted is char shiftGlyph && _shiftFont is not null)
        {
            int badgeHeight = Math.Max(12, (int)(rect.Height * 0.38f));
            int badgeWidth = Math.Max(12, badgeHeight);
            Rectangle shiftRect = new(
                rect.Right - badgeWidth - 2,
                rect.Top + 2,
                badgeWidth,
                badgeHeight);

            bool shiftPill = _shiftHeld || (highlighted && _nextShift);
            using (GraphicsPath badge = RoundedPanel.CreateRoundedPath(shiftRect, 4))
            using (var badgeBrush = new SolidBrush(
                       shiftPill ? Accent : Color.FromArgb(228, 230, 243)))
            {
                graphics.FillPath(badgeBrush, badge);
            }

            using var brush = new SolidBrush(
                shiftPill ? Color.White : Color.FromArgb(48, 46, 84));
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            graphics.DrawString(shiftGlyph.ToString(), _shiftFont, brush, shiftRect, format);
        }
    }

    private void DrawShiftKey(Graphics graphics, Rectangle rect, GraphicsPath path, bool highlighted)
    {
        bool wanted = _nextShift && !_shiftHeld;
        Color fill = _shiftHeld || highlighted
            ? Accent
            : wanted
                ? Color.FromArgb(254, 215, 170)
                : Color.FromArgb(226, 228, 240);

        using (var brush = new LinearGradientBrush(
                   rect,
                   Lighten(fill, 0.30f),
                   fill,
                   LinearGradientMode.Vertical))
        {
            graphics.FillPath(brush, path);
        }

        float borderWidth = _shiftHeld || wanted || highlighted ? 2.2f : 1f;
        Color borderColor = _shiftHeld || highlighted
            ? Color.FromArgb(67, 56, 202)
            : wanted
                ? Color.FromArgb(245, 158, 11)
                : Color.FromArgb(148, 153, 184);

        using (var border = new Pen(borderColor, borderWidth))
        {
            graphics.DrawPath(border, path);
        }

        if (wanted && !highlighted)
        {
            using var tint = new SolidBrush(Color.FromArgb(30 + (int)(_pulse * 40), 245, 158, 11));
            graphics.FillPath(tint, path);
        }

        int radius = Math.Max(3, Math.Min(8, rect.Height / 5));
        using (var gloss = new Pen(Color.FromArgb(120, 255, 255, 255), 1f))
        {
            graphics.DrawLine(
                gloss,
                rect.Left + radius,
                rect.Top + 1,
                rect.Right - radius,
                rect.Top + 1);
        }

        Color textColor = _shiftHeld || highlighted
            ? Color.White
            : wanted
                ? Color.FromArgb(146, 64, 14)
                : MutedInk;

        TextRenderer.DrawText(
            graphics,
            _shiftHeld ? "SHIFT ON" : "SHIFT",
            _spaceFont,
            rect,
            textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    private void DrawHighlightGlow(Graphics graphics, Rectangle rect)
    {
        int alpha = 42 + (int)(_pulse * 40);
        for (int i = 4; i >= 1; i--)
        {
            Rectangle glowRect = Rectangle.Inflate(rect, i * 2, i * 2);
            using GraphicsPath glowPath = RoundedPanel.CreateRoundedPath(
                glowRect,
                Math.Max(4, Math.Min(10, rect.Height / 5)) + i);
            using var pen = new Pen(Color.FromArgb(Math.Max(12, alpha / i), Accent.R, Accent.G, Accent.B), 3f);
            graphics.DrawPath(pen, glowPath);
        }
    }

    private void UpdateGlyphFonts(float keyHeight)
    {
        float size = Math.Max(8f, keyHeight * 0.36f);
        if (_glyphFont is not null && Math.Abs(size - _glyphFontSize) < 0.75f)
        {
            return;
        }

        ResetGlyphFonts();
        _glyphFontSize = size;

        if (_bengaliFont is null)
        {
            return;
        }

        FontFamily family = _bengaliFont.FontFamily;
        _glyphFont = new Font(family, size, FontStyle.Regular, GraphicsUnit.Pixel);
        _shiftFont = new Font(family, Math.Max(7f, size * 0.55f), FontStyle.Regular, GraphicsUnit.Pixel);
    }

    private void ResetGlyphFonts()
    {
        _glyphFont?.Dispose();
        _shiftFont?.Dispose();
        _glyphFont = null;
        _shiftFont = null;
        _glyphFontSize = 0f;
    }

    private static char? GetBengali(char physical, bool shift)
    {
        if (!BijoyMappingTable.TryMap(physical, shift, out char code))
        {
            return null;
        }

        bool display = char.IsLetter(code) ||
                       char.IsDigit(code) ||
                       code >= 0x80 ||
                       code is '&' or '|';

        return display ? code : null;
    }

    private static float UnitsFor(string label)
    {
        return label switch
        {
            " " => 6f,
            "Shift" => 2.4f,
            _ => 1f
        };
    }

    private static bool CharEquals(char a, char b)
    {
        return char.ToLowerInvariant(a) == char.ToLowerInvariant(b);
    }

    /// <summary>
    /// Returns the finger-zone colour used for a physical key. Space is drawn
    /// in the neutral thumb colour.
    /// </summary>
    /// <param name="key">The physical key character.</param>
    /// <returns>The fill colour for the key.</returns>
    public static Color KeyColor(char key)
    {
        return key == ' ' ? Thumb : FingerColor(key);
    }

    /// <summary>
    /// Returns the finger-zone colour for a physical key, ignoring the special
    /// neutral colour used for Space by <see cref="KeyColor"/>.
    /// </summary>
    /// <param name="key">The physical key character.</param>
    /// <returns>The finger-zone fill colour.</returns>
    public static Color FingerColor(char key)
    {
        char k = char.ToLowerInvariant(key);
        return k switch
        {
            'q' or 'a' or 'z' or '`' or '1' => Pinky,
            'w' or 's' or 'x' or '2' => Ring,
            'e' or 'd' or 'c' or '3' => Middle,
            'r' or 'f' or 'v' or 't' or 'g' or 'b' or '4' or '5' => Index,
            'y' or 'h' or 'n' or 'u' or 'j' or 'm' or '6' or '7' => Index,
            'i' or 'k' or ',' or '8' => Middle,
            'o' or 'l' or '.' or '9' => Ring,
            _ => Pinky
        };
    }

    private static Color Lighten(Color color, float amount)
    {
        return Color.FromArgb(
            color.A,
            (int)Math.Min(255, color.R + (255 - color.R) * amount),
            (int)Math.Min(255, color.G + (255 - color.G) * amount),
            (int)Math.Min(255, color.B + (255 - color.B) * amount));
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _latinFont.Dispose();
            _spaceFont.Dispose();
            ResetGlyphFonts();
        }

        base.Dispose(disposing);
    }
}
