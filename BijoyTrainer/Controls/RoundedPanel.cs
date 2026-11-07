using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BijoyTrainer.Controls;

/// <summary>
/// A panel that paints a white rounded card with a soft shadow and border.
/// Child controls are laid out normally on top of the card.
/// </summary>
public class RoundedPanel : Panel
{
    private int _cornerRadius = 18;
    private Color _fillColor = Color.White;
    private Color _borderColor = Color.FromArgb(226, 228, 240);
    private bool _showShadow = true;

    /// <summary>Initializes a new instance of the <see cref="RoundedPanel"/> class.</summary>
    public RoundedPanel()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        // White (not Transparent) so child controls that ask for the parent
        // colour get the card surface. The rounded corners are produced by
        // OnPaintBackground, which repaints them with the real parent colour.
        BackColor = Color.White;
        Padding = new Padding(18);
    }

    /// <summary>Gets or sets the corner radius in pixels.</summary>
    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = Math.Max(0, value); Invalidate(); }
    }

    /// <summary>Gets or sets the card fill colour.</summary>
    public Color FillColor
    {
        get => _fillColor;
        set { _fillColor = value; Invalidate(); }
    }

    /// <summary>Gets or sets the card border colour.</summary>
    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Invalidate(); }
    }

    /// <summary>Gets or sets a value indicating whether a soft drop shadow is drawn.</summary>
    public bool ShowShadow
    {
        get => _showShadow;
        set { _showShadow = value; Invalidate(); }
    }

    /// <inheritdoc />
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Color parentColor = Parent?.BackColor ?? Color.FromArgb(238, 240, 249);
        graphics.Clear(parentColor);

        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        if (ShowShadow)
        {
            Rectangle shadow = new(bounds.X + 1, bounds.Y + 4, bounds.Width - 2, bounds.Height - 3);
            using GraphicsPath shadowPath = CreateRoundedPath(shadow, CornerRadius);
            using var shadowBrush = new SolidBrush(Color.FromArgb(16, 30, 27, 75));
            graphics.FillPath(shadowBrush, shadowPath);
        }

        using GraphicsPath path = CreateRoundedPath(bounds, CornerRadius);
        using var fill = new SolidBrush(FillColor);
        graphics.FillPath(fill, path);

        using var border = new Pen(BorderColor, 1f);
        graphics.DrawPath(border, path);
    }

    /// <summary>Creates a rounded rectangle path.</summary>
    /// <param name="bounds">Rectangle to round.</param>
    /// <param name="radius">Corner radius.</param>
    /// <returns>A disposable path.</returns>
    public static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int diameter = Math.Max(1, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
