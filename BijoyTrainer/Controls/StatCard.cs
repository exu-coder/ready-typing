using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BijoyTrainer.Controls;

/// <summary>
/// A compact owner-drawn statistic card showing a caption, a large value and an
/// optional suffix.
/// </summary>
public class StatCard : Control
{
    private string _caption = "STAT";
    private string _value = "0";
    private string _suffix = string.Empty;
    private Color _accentColor = Color.FromArgb(79, 70, 229);

    private readonly Font _captionFont = new("Times New Roman", 8f, FontStyle.Bold);
    private readonly Font _valueFont = new("Times New Roman", 19f, FontStyle.Bold);
    private readonly Font _suffixFont = new("Times New Roman", 9f);

    /// <summary>Initializes a new instance of the <see cref="StatCard"/> class.</summary>
    public StatCard()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.Transparent;
        MinimumSize = new Size(120, 64);
    }

    /// <summary>Gets or sets the caption shown above the value.</summary>
    public string Caption
    {
        get => _caption;
        set { _caption = value ?? string.Empty; Invalidate(); }
    }

    /// <summary>Gets or sets the main value text.</summary>
    public string Value
    {
        get => _value;
        set { _value = value ?? string.Empty; Invalidate(); }
    }

    /// <summary>Gets or sets a small suffix such as <c>%</c>.</summary>
    public string Suffix
    {
        get => _suffix;
        set { _suffix = value ?? string.Empty; Invalidate(); }
    }

    /// <summary>Gets or sets the accent colour used for the dot and value.</summary>
    public Color AccentColor
    {
        get => _accentColor;
        set { _accentColor = value; Invalidate(); }
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent?.BackColor ?? Color.White);

        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        using (GraphicsPath path = RoundedPanel.CreateRoundedPath(bounds, 14))
        using (var fill = new SolidBrush(Color.White))
        {
            graphics.FillPath(fill, path);
            using var border = new Pen(Color.FromArgb(226, 228, 240), 1f);
            graphics.DrawPath(border, path);
        }

        // Accent dot + caption
        using (var dot = new SolidBrush(AccentColor))
        {
            graphics.FillEllipse(dot, 14, 13, 6, 6);
        }

        TextRenderer.DrawText(
            graphics,
            Caption,
            _captionFont,
            new Rectangle(26, 7, Width - 36, 18),
            Color.FromArgb(99, 96, 133),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        TextRenderer.DrawText(
            graphics,
            Value,
            _valueFont,
            new Rectangle(14, 26, Width - 28, 34),
            AccentColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        if (Suffix.Length > 0)
        {
            Size valueSize = TextRenderer.MeasureText(graphics, Value, _valueFont, Size.Empty, TextFormatFlags.NoPrefix);
            int suffixX = 16 + valueSize.Width + 6;
            int suffixWidth = Math.Max(0, Width - 16 - (16 + valueSize.Width) - 8);
            TextRenderer.DrawText(
                graphics,
                Suffix,
                _suffixFont,
                new Rectangle(suffixX, 40, suffixWidth, 18),
                Color.FromArgb(99, 96, 133),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _captionFont.Dispose();
            _valueFont.Dispose();
            _suffixFont.Dispose();
        }

        base.Dispose(disposing);
    }
}
