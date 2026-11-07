using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BijoyTrainer.Controls;

/// <summary>
/// A five-star rating with a short description, used on the lesson results
/// screen next to the gradient score bars.
/// </summary>
public class StarRating : Control
{
    private static readonly Color Filled = Color.FromArgb(249, 115, 22);
    private static readonly Color FilledBorder = Color.FromArgb(234, 88, 12);
    private static readonly Color Empty = Color.FromArgb(226, 228, 240);
    private static readonly Color EmptyBorder = Color.FromArgb(214, 216, 234);
    private static readonly Color Ink = Color.FromArgb(30, 27, 75);
    private static readonly Color Muted = Color.FromArgb(99, 96, 133);

    private int _stars = 5;

    /// <summary>Initializes a new instance of the <see cref="StarRating"/> class.</summary>
    public StarRating()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.Transparent;
    }

    /// <summary>Gets or sets the number of filled stars, from 0 to 5.</summary>
    public int Stars
    {
        get => _stars;
        set { _stars = Math.Clamp(value, 0, 5); Invalidate(); }
    }

    /// <inheritdoc />
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? Color.White);
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        int width = ClientSize.Width;
        if (width <= 20)
        {
            return;
        }

        const int count = 5;
        int starSize = Math.Min(34, Math.Max(18, (width - 16) / count - 4));
        int gap = 4;
        int total = count * starSize + (count - 1) * gap;
        int startX = (width - total) / 2;
        int top = 12;

        for (int i = 0; i < count; i++)
        {
            Rectangle bounds = new(startX + i * (starSize + gap), top, starSize, starSize);
            bool filled = i < Stars;

            using (var shadow = new SolidBrush(Color.FromArgb(filled ? 34 : 16, 30, 27, 75)))
            {
                using GraphicsPath shadowPath = CreateStarPath(
                    new Rectangle(bounds.X, bounds.Y + 2, bounds.Width, bounds.Height));
                graphics.FillPath(shadow, shadowPath);
            }

            using GraphicsPath path = CreateStarPath(bounds);
            using var fill = new SolidBrush(filled ? Filled : Empty);
            using var border = new Pen(filled ? FilledBorder : EmptyBorder, 1.2f);
            graphics.FillPath(fill, path);
            graphics.DrawPath(border, path);
        }

        using (var numberFont = new Font("Times New Roman", 12f, FontStyle.Bold))
        using (var labelFont = new Font("Times New Roman", 9.5f))
        {
            TextRenderer.DrawText(
                graphics,
                $"{Stars} of 5",
                numberFont,
                new Rectangle(0, top + starSize + 10, width, 22),
                Ink,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(
                graphics,
                Describe(Stars),
                labelFont,
                new Rectangle(0, top + starSize + 34, width, 20),
                Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    private static string Describe(int stars) => stars switch
    {
        5 => "Excellent",
        4 => "Good",
        3 => "Getting there",
        2 => "Keep practising",
        _ => "Just starting"
    };

    private static GraphicsPath CreateStarPath(Rectangle bounds)
    {
        var path = new GraphicsPath();
        float centerX = bounds.X + bounds.Width / 2f;
        float centerY = bounds.Y + bounds.Height / 2f;
        float outer = Math.Min(bounds.Width, bounds.Height) / 2f;
        float inner = outer * 0.45f;
        var points = new PointF[10];

        for (int i = 0; i < points.Length; i++)
        {
            double angle = -Math.PI / 2d + i * Math.PI / 5d;
            float radius = i % 2 == 0 ? outer : inner;
            points[i] = new PointF(
                centerX + (float)(Math.Cos(angle) * radius),
                centerY + (float)(Math.Sin(angle) * radius));
        }

        path.AddPolygon(points);
        path.CloseFigure();
        return path;
    }
}
