using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using BijoyTrainer.Controls;

namespace BijoyTrainer.Views;

/// <summary>
/// The dashboard hero card: a welcome caption, headline, supporting copy, a
/// call-to-action button and an optional logo tile in the top-right corner.
/// </summary>
public sealed class HeroPanel : RoundedPanel
{
    private const string WatermarkGlyph = "অ";
    private const string LogoGlyph = "অ";

    private readonly AccentButton _exploreButton;
    private readonly Font _captionFont = Theme.CreateFont(8f, FontStyle.Bold);
    private readonly Font _titleFont = Theme.CreateFont(26f, FontStyle.Bold);
    private readonly Font _bodyFont = Theme.CreateFont(10f);
    private readonly Font _buttonFont = Theme.CreateFont(9.5f, FontStyle.Bold);
    private readonly Font _watermarkFont = CreateGlyphFont(150f, FontStyle.Bold);
    private readonly Font _logoGlyphFont = CreateGlyphFont(34f, FontStyle.Bold);
    private readonly int _bodyLineHeight;

    private Image? _logo;
    private string _caption = "WELCOME BACK";
    private string _title = "Master Bengali Typing.";
    private string _body =
        "Improve your typing speed, build accuracy and become a Bijoy typing expert — one lesson at a time. " +
        "Follow the next-key hint and finish before the timer ends.";

    /// <summary>Initializes a new instance of the <see cref="HeroPanel"/> class.</summary>
    public HeroPanel()
    {
        DoubleBuffered = true;
        CornerRadius = 16;
        FillColor = Color.White;
        BorderColor = Theme.Line;
        ShowShadow = true;
        Padding = System.Windows.Forms.Padding.Empty;
        MinimumSize = new Size(600, 150);
        Height = 170;

        _bodyLineHeight = TextRenderer.MeasureText("Ag", _bodyFont).Height;

        _exploreButton = new AccentButton
        {
            Text = "Explore Lessons →",
            Size = new Size(172, 40),
            CornerRadius = 11,
            BaseColor = Theme.Accent,
            HoverColor = Theme.AccentDark,
            PressedColor = Theme.AccentDeep,
            TextColor = Color.White,
            Font = _buttonFont
        };
        _exploreButton.Click += ExploreButton_Click;

        Controls.Add(_exploreButton);
        LayoutExploreButton();
    }

    /// <summary>
    /// Gets or sets the optional logo image shown in the top-right tile.
    /// </summary>
    /// <remarks>The caller retains ownership of the image.</remarks>
    public Image? Logo
    {
        get => _logo;
        set
        {
            _logo = value;
            Invalidate();
        }
    }

    /// <summary>Gets or sets the small uppercase caption above the headline.</summary>
    public string Caption
    {
        get => _caption;
        set
        {
            _caption = value ?? string.Empty;
            Invalidate();
        }
    }

    /// <summary>Gets or sets the hero headline.</summary>
    public string Title
    {
        get => _title;
        set
        {
            _title = value ?? string.Empty;
            Invalidate();
        }
    }

    /// <summary>Gets or sets the supporting copy shown under the headline.</summary>
    public string Body
    {
        get => _body;
        set
        {
            _body = value ?? string.Empty;
            Invalidate();
        }
    }

    /// <summary>Raised when the "Explore Lessons" button is clicked.</summary>
    public event EventHandler? ExploreClicked;

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

        // Keep the decoration and copy inside the rounded card outline.
        Rectangle cardBounds = new(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using GraphicsPath cardPath = Theme.RoundedPath(cardBounds, CornerRadius);
        GraphicsState state = graphics.Save();
        graphics.SetClip(cardPath, CombineMode.Intersect);

        DrawWatermark(graphics);
        DrawCopy(graphics);
        DrawLogo(graphics);

        graphics.Restore(state);
    }

    /// <inheritdoc />
    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        LayoutExploreButton();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_exploreButton is not null)
            {
                _exploreButton.Click -= ExploreButton_Click;
                _exploreButton.Dispose();
            }

            _captionFont.Dispose();
            _titleFont.Dispose();
            _bodyFont.Dispose();
            _buttonFont.Dispose();
            _watermarkFont.Dispose();
            _logoGlyphFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void LayoutExploreButton()
    {
        // OnResize can run from the base constructor before the button exists.
        if (_exploreButton is null)
        {
            return;
        }

        _exploreButton.Location = new Point(24, Math.Max(0, Height - 64));
    }

    private void ExploreButton_Click(object? sender, EventArgs e)
    {
        ExploreClicked?.Invoke(this, EventArgs.Empty);
    }

    private void DrawWatermark(Graphics graphics)
    {
        SizeF size = graphics.MeasureString(WatermarkGlyph, _watermarkFont);
        float x = Width - 36f - size.Width;
        using var brush = new SolidBrush(Color.FromArgb(14, Theme.Accent));
        graphics.DrawString(WatermarkGlyph, _watermarkFont, brush, x, -70f);
    }

    private void DrawCopy(Graphics graphics)
    {
        TextRenderer.DrawText(
            graphics,
            Caption,
            _captionFont,
            new Point(24, 20),
            Theme.Muted,
            TextFormatFlags.Left | TextFormatFlags.NoPrefix);

        int titleWidth = Math.Max(200, Width - 150);
        TextRenderer.DrawText(
            graphics,
            Title,
            _titleFont,
            new Rectangle(24, 38, titleWidth, 44),
            Theme.Ink,
            TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

        // Leave the bottom band free for the CTA button. When the panel is only
        // tall enough for a single line, nudge the copy up rather than letting
        // it run into the button or lose its descenders.
        // The logo tile occupies the right 132px of the panel (88px tile plus
        // margins); keep a 24px gutter between the copy and the tile so the
        // body text never runs underneath it, even at minimum width.
        int bodyWidth = Math.Max(1, Width - 132 - 24 - 24);
        int bodyTop = 84;
        int bodyBottom = Height - 72;
        int bodyHeight = Math.Min(54, bodyBottom - bodyTop);
        if (bodyHeight > 0 && bodyHeight < _bodyLineHeight)
        {
            int shiftedTop = bodyBottom - _bodyLineHeight;
            if (shiftedTop >= 38 + _titleFont.Height)
            {
                bodyTop = shiftedTop;
                bodyHeight = _bodyLineHeight;
            }
        }

        if (bodyHeight <= 0)
        {
            return;
        }

        TextRenderer.DrawText(
            graphics,
            Body,
            _bodyFont,
            new Rectangle(24, bodyTop, bodyWidth, bodyHeight),
            Theme.Muted2,
            TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }

    private void DrawLogo(Graphics graphics)
    {
        int x = Math.Max(0, Width - 22 - 88);
        Rectangle bounds = new(x, 22, 88, 88);
        using GraphicsPath path = Theme.RoundedPath(bounds, 18);

        if (_logo is not null)
        {
            GraphicsState state = graphics.Save();
            graphics.SetClip(path, CombineMode.Intersect);
            graphics.DrawImage(_logo, bounds);
            graphics.Restore(state);
        }
        else
        {
            using (var fill = new SolidBrush(Theme.Accent))
            {
                graphics.FillPath(fill, path);
            }

            TextRenderer.DrawText(
                graphics,
                LogoGlyph,
                _logoGlyphFont,
                bounds,
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        using var border = new Pen(Theme.Line, 1f);
        graphics.DrawPath(border, path);
    }

    /// <summary>
    /// Creates a font that can display the Bengali glyph used by the watermark
    /// and the logo fallback, falling back to the dashboard typeface.
    /// </summary>
    private static Font CreateGlyphFont(float size, FontStyle style)
    {
        string[] candidates = { "Nirmala UI", "Vrinda", "Shonar Bangla" };

        foreach (string family in candidates)
        {
            try
            {
                var font = new Font(family, size, style);
                if (string.Equals(font.Name, family, StringComparison.OrdinalIgnoreCase))
                {
                    return font;
                }

                font.Dispose();
            }
            catch (ArgumentException)
            {
                // Family is unavailable; try the next candidate.
            }
        }

        return Theme.CreateFont(size, style);
    }
}
