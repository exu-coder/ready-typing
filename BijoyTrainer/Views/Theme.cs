using System.Drawing.Drawing2D;

namespace BijoyTrainer.Views;

/// <summary>
/// Central palette and drawing helpers for the dashboard, mirroring the
/// CSS variables used by the ReadyTypingWeb mock-up.
/// </summary>
public static class Theme
{
    /// <summary>Page background colour.</summary>
    public static readonly Color Page = Color.FromArgb(238, 240, 249);

    /// <summary>Primary text / heading colour.</summary>
    public static readonly Color Ink = Color.FromArgb(30, 27, 75);

    /// <summary>Secondary (muted) text colour.</summary>
    public static readonly Color Muted = Color.FromArgb(99, 96, 133);

    /// <summary>Darker muted text colour.</summary>
    public static readonly Color Muted2 = Color.FromArgb(70, 68, 96);

    /// <summary>Primary brand accent (indigo).</summary>
    public static readonly Color Accent = Color.FromArgb(79, 70, 229);

    /// <summary>Darker accent used for hover / emphasis.</summary>
    public static readonly Color AccentDark = Color.FromArgb(67, 56, 202);

    /// <summary>Deep accent used for pressed / high-emphasis states.</summary>
    public static readonly Color AccentDeep = Color.FromArgb(55, 48, 163);

    /// <summary>Light accent used for subtle highlights.</summary>
    public static readonly Color AccentLight = Color.FromArgb(129, 140, 248);

    /// <summary>Emerald green used for success states.</summary>
    public static readonly Color Emerald = Color.FromArgb(16, 185, 129);

    /// <summary>Deep cyan used for informational states.</summary>
    public static readonly Color Cyan = Color.FromArgb(6, 182, 212);

    /// <summary>Light cyan used for highlights.</summary>
    public static readonly Color CyanLight = Color.FromArgb(34, 211, 238);

    /// <summary>Card border / divider colour.</summary>
    public static readonly Color Line = Color.FromArgb(226, 228, 240);

    /// <summary>Footer background colour.</summary>
    public static readonly Color Footer = Color.FromArgb(228, 230, 243);

    /// <summary>Colour for correctly typed characters.</summary>
    public static readonly Color Correct = Color.FromArgb(4, 120, 87);

    /// <summary>Colour for incorrectly typed characters.</summary>
    public static readonly Color Wrong = Color.FromArgb(225, 29, 72);

    /// <summary>Background highlight for the current character.</summary>
    public static readonly Color CurrentBack = Color.FromArgb(254, 243, 199);

    /// <summary>Colour for not-yet-typed characters.</summary>
    public static readonly Color Future = Color.FromArgb(70, 68, 96);

    /// <summary>Warning text colour.</summary>
    public static readonly Color Warning = Color.FromArgb(180, 83, 9);

    /// <summary>Amber accent used for highlights.</summary>
    public static readonly Color Amber = Color.FromArgb(245, 158, 11);

    /// <summary>Sky blue accent used for secondary highlights.</summary>
    public static readonly Color Sky = Color.FromArgb(56, 189, 248);

    /// <summary>Soft indigo fill used for subtle panels.</summary>
    public static readonly Color SoftFill = Color.FromArgb(238, 242, 255);

    /// <summary>Soft indigo border used for subtle panels.</summary>
    public static readonly Color SoftBorder = Color.FromArgb(199, 210, 254);

    /// <summary>
    /// Creates a new <see cref="Font"/> instance in the dashboard typeface.
    /// The caller owns (and must dispose) the returned font.
    /// </summary>
    /// <param name="size">Font size in points.</param>
    /// <param name="style">Font style to apply.</param>
    /// <returns>A freshly allocated font instance.</returns>
    public static Font CreateFont(float size, FontStyle style = FontStyle.Regular)
    {
        return new Font("Times New Roman", size, style);
    }

    /// <summary>
    /// Builds a rounded rectangle path with four arcs.
    /// </summary>
    /// <param name="bounds">Rectangle to round.</param>
    /// <param name="radius">
    /// Corner radius in pixels; clamped to at least 1 and at most half the
    /// smaller dimension of <paramref name="bounds"/>.
    /// </param>
    /// <returns>
    /// A disposable path. If the bounds are empty the path contains an empty
    /// rectangle.
    /// </returns>
    public static GraphicsPath RoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            path.AddRectangle(Rectangle.Empty);
            return path;
        }

        int maxRadius = Math.Min(bounds.Width, bounds.Height) / 2;
        int r = Math.Max(1, Math.Min(radius, maxRadius));
        int diameter = r * 2;

        var arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);

        // Top-left
        path.AddArc(arc, 180, 90);

        // Top-right
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);

        // Bottom-right
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        // Bottom-left
        arc.X = bounds.X;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// Draws a card: a subtle drop shadow, a rounded fill and a 1px border.
    /// </summary>
    /// <param name="g">Graphics surface to draw on.</param>
    /// <param name="bounds">Card bounds in client coordinates.</param>
    /// <param name="radius">Corner radius in pixels.</param>
    /// <param name="fill">Optional fill colour; defaults to white.</param>
    public static void DrawCard(Graphics g, Rectangle bounds, int radius = 16, Color? fill = null)
    {
        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return;
        }

        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Subtle drop shadow.
        if (bounds.Width > 4 && bounds.Height > 4)
        {
            var shadowBounds = new Rectangle(bounds.X, bounds.Y + 2, bounds.Width, bounds.Height);
            using var shadowPath = RoundedPath(shadowBounds, radius);
            using var shadowBrush = new SolidBrush(Color.FromArgb(14, 30, 27, 75));
            g.FillPath(shadowBrush, shadowPath);
        }

        using (var path = RoundedPath(bounds, radius))
        {
            using (var brush = new SolidBrush(fill ?? Color.White))
            {
                g.FillPath(brush, path);
            }

            using var pen = new Pen(Line, 1f);
            g.DrawPath(pen, path);
        }
    }

    /// <summary>
    /// Draws a horizontal progress bar with a rounded track and gradient fill.
    /// </summary>
    /// <param name="g">Graphics surface to draw on.</param>
    /// <param name="bounds">Bar bounds in client coordinates.</param>
    /// <param name="fraction">Progress in the range 0..1; clamped.</param>
    /// <param name="fill">Optional start colour; defaults to <see cref="Accent"/>.</param>
    public static void DrawProgress(Graphics g, Rectangle bounds, double fraction, Color? fill = null)
    {
        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return;
        }

        g.SmoothingMode = SmoothingMode.AntiAlias;

        int radius = Math.Max(1, bounds.Height / 2);

        // Track.
        using (var trackPath = RoundedPath(bounds, radius))
        using (var trackBrush = new SolidBrush(Line))
        {
            g.FillPath(trackBrush, trackPath);
        }

        double clamped = Math.Clamp(fraction, 0d, 1d);
        if (clamped <= 0d)
        {
            return;
        }

        int fillWidth = (int)Math.Round(bounds.Width * clamped);
        if (fillWidth < 1)
        {
            return;
        }

        var fillBounds = new Rectangle(bounds.X, bounds.Y, fillWidth, bounds.Height);

        Color start = fill ?? Accent;
        Color end = Color.FromArgb(34, 211, 238);

        using var fillPath = RoundedPath(fillBounds, radius);
        using var gradient = new LinearGradientBrush(
            fillBounds,
            start,
            end,
            LinearGradientMode.Horizontal);
        g.FillPath(gradient, fillPath);
    }

    /// <summary>
    /// Blends a colour toward white while preserving its alpha channel.
    /// </summary>
    /// <param name="color">Source colour.</param>
    /// <param name="amount">Blend amount in the range 0..1.</param>
    /// <returns>The lightened colour.</returns>
    public static Color Lighten(Color color, float amount)
    {
        float t = Math.Clamp(amount, 0f, 1f);
        int r = (int)Math.Round(color.R + ((255 - color.R) * t));
        int gr = (int)Math.Round(color.G + ((255 - color.G) * t));
        int b = (int)Math.Round(color.B + ((255 - color.B) * t));
        return Color.FromArgb(color.A, r, gr, b);
    }

    /// <summary>
    /// Blends a colour toward black while preserving its alpha channel.
    /// </summary>
    /// <param name="color">Source colour.</param>
    /// <param name="amount">Blend amount in the range 0..1.</param>
    /// <returns>The darkened colour.</returns>
    public static Color Darken(Color color, float amount)
    {
        float t = Math.Clamp(amount, 0f, 1f);
        int r = (int)Math.Round(color.R * (1f - t));
        int gr = (int)Math.Round(color.G * (1f - t));
        int b = (int)Math.Round(color.B * (1f - t));
        return Color.FromArgb(color.A, r, gr, b);
    }
}
