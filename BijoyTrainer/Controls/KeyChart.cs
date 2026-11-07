using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using BijoyTrainer.Core;

namespace BijoyTrainer.Controls;

/// <summary>Selects what a <see cref="KeyChart"/> bar represents.</summary>
public enum KeyChartMode
{
    /// <summary>Average key presses per minute.</summary>
    Speed,

    /// <summary>Correct presses as a percentage of all presses.</summary>
    Accuracy
}

/// <summary>
/// A compact per-key bar chart for the detailed statistics tab. Speed mode
/// draws green bars in key presses per minute; accuracy mode colours each bar
/// by how reliable the key was and prints the correct-to-total ratio.
/// </summary>
public class KeyChart : Control
{
    private static readonly Color Ink = Color.FromArgb(30, 27, 75);
    private static readonly Color Muted = Color.FromArgb(99, 96, 133);
    private static readonly Color Border = Color.FromArgb(226, 228, 240);
    private static readonly Color HeaderFill = Color.FromArgb(243, 244, 251);
    private static readonly Color Axis = Color.FromArgb(214, 216, 234);
    private static readonly Color Good = Color.FromArgb(34, 197, 94);
    private static readonly Color GoodDark = Color.FromArgb(22, 163, 74);
    private static readonly Color Warn = Color.FromArgb(245, 158, 11);
    private static readonly Color Bad = Color.FromArgb(239, 68, 68);
    private static readonly Color AverageLine = Color.FromArgb(16, 185, 129);

    private IReadOnlyList<KeyReport> _keys = Array.Empty<KeyReport>();
    private string _title = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="KeyChart"/> class.</summary>
    public KeyChart()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.White;
    }

    /// <summary>Gets or sets the chart heading.</summary>
    public string Title
    {
        get => _title;
        set { _title = value ?? string.Empty; Invalidate(); }
    }

    /// <summary>Gets or sets what the bars measure.</summary>
    public KeyChartMode Mode { get; set; }

    /// <summary>Gets or sets the per-key data shown in the chart.</summary>
    public IReadOnlyList<KeyReport> Keys
    {
        get => _keys;
        set { _keys = value ?? Array.Empty<KeyReport>(); Invalidate(); }
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(BackColor);

        int width = ClientSize.Width;
        int height = ClientSize.Height;
        if (width <= 40 || height <= 60)
        {
            return;
        }

        using (var header = new SolidBrush(HeaderFill))
        {
            graphics.FillRectangle(header, 0, 0, width, 34);
        }

        using (var headerLine = new Pen(Border, 1f))
        {
            graphics.DrawLine(headerLine, 0, 34, width, 34);
        }

        using (var titleFont = new Font("Times New Roman", 10f, FontStyle.Bold))
        {
            // Title occupies the left half; the average hint is right-aligned in the
            // right half, so the two never overlap.
            int titleWidth = Math.Max(0, width / 2 - 20);
            TextRenderer.DrawText(
                graphics,
                Title,
                titleFont,
                new Rectangle(14, 6, titleWidth, 22),
                Ink,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        var shown = Keys
            .Where(key => key.Presses > 0)
            .OrderByDescending(key => key.Presses)
            .ThenBy(key => key.Key)
            .Take(8)
            .OrderBy(ValueOf)
            .ThenBy(key => key.Key)
            .ToList();

        if (shown.Count == 0)
        {
            using var emptyFont = new Font("Times New Roman", 10f);
            TextRenderer.DrawText(
                graphics,
                "Type the line to record per-key statistics.",
                emptyFont,
                new Rectangle(20, 34, width - 40, height - 34),
                Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            return;
        }

        double average = shown.Average(ValueOf);
        using (var hintFont = new Font("Times New Roman", 8.5f))
        {
            string hint = Mode == KeyChartMode.Speed
                ? $"avg {Math.Round(average)} KPM"
                : $"avg {Math.Round(average)}%";
            TextRenderer.DrawText(
                graphics,
                hint,
                hintFont,
                new Rectangle(width / 2, 6, width / 2 - 14, 22),
                Muted,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        int plotLeft = 22;
        int plotRight = width - 24;
        int plotTop = 52;
        int plotBottom = height - 46;
        int plotWidth = plotRight - plotLeft;
        int plotHeight = plotBottom - plotTop;
        if (plotWidth <= 20 || plotHeight <= 20)
        {
            return;
        }

        double max = Mode == KeyChartMode.Speed
            ? Math.Max(10d, shown.Max(key => key.SpeedKpm) * 1.18d)
            : 100d;

        int slot = Math.Max(1, plotWidth / shown.Count);
        int barWidth = Math.Clamp((int)Math.Round(slot * 0.46d), 12, 34);

        DrawAverageLine(graphics, plotLeft, plotRight, plotTop, plotBottom, plotHeight, average, max);

        using var axisPen = new Pen(Axis, 1.2f);
        graphics.DrawLine(axisPen, plotLeft - 2, plotBottom, plotRight + 2, plotBottom);

        using var speedFont = new Font("Times New Roman", 9f, FontStyle.Bold);
        using var tagFont = new Font("Times New Roman", 8.5f, FontStyle.Bold);
        using var ratioFont = new Font("Times New Roman", 8f);

        for (int i = 0; i < shown.Count; i++)
        {
            KeyReport key = shown[i];
            double value = ValueOf(key);
            double ratio = Math.Clamp(value / max, 0d, 1d);
            int barHeight = Math.Max(3, (int)Math.Round(ratio * plotHeight));
            int x = plotLeft + i * slot + (slot - barWidth) / 2;
            int y = plotBottom - barHeight;

            Color barColor = Mode == KeyChartMode.Speed
                ? Good
                : value >= 90d
                    ? Good
                    : value >= 70d
                        ? Warn
                        : Bad;

            DrawBar(graphics, new Rectangle(x, y, barWidth, barHeight), barColor);

            if (Mode == KeyChartMode.Speed)
            {
                DrawSpeedTag(graphics, speedFont, x + barWidth / 2, y, value, plotTop);
            }
            else
            {
                DrawAccuracyTag(graphics, tagFont, ratioFont, x + barWidth / 2, y, key, plotTop);
            }

            DrawKeyPill(graphics, key, x + barWidth / 2, plotBottom + 8, slot, barWidth);
        }
    }

    private double ValueOf(KeyReport key)
    {
        return Mode == KeyChartMode.Speed ? key.SpeedKpm : key.Accuracy;
    }

    private static void DrawBar(Graphics graphics, Rectangle bounds, Color color)
    {
        if (bounds.Height <= 0 || bounds.Width <= 0)
        {
            return;
        }

        int radius = Math.Min(3, Math.Min(bounds.Width, bounds.Height) / 2);
        using GraphicsPath path = RoundedPanel.CreateRoundedPath(bounds, radius);
        using var fill = new LinearGradientBrush(
            bounds,
            Lighten(color, 0.12f),
            Darken(color, 0.08f),
            LinearGradientMode.Vertical);
        graphics.FillPath(fill, path);
    }

    private static void DrawAverageLine(
        Graphics graphics,
        int left,
        int right,
        int top,
        int bottom,
        int plotHeight,
        double average,
        double max)
    {
        double ratio = Math.Clamp(average / max, 0d, 1d);
        float y = bottom - (float)(ratio * plotHeight);

        using var pen = new Pen(AverageLine, 1.4f) { DashStyle = DashStyle.Dot };
        graphics.DrawLine(pen, left - 8, y, right + 8, y);

        using var diamond = new SolidBrush(AverageLine);
        DrawDiamond(graphics, diamond, left - 9, y, 5);
        DrawDiamond(graphics, diamond, right + 9, y, 5);
    }

    private static void DrawDiamond(Graphics graphics, Brush brush, float x, float y, float radius)
    {
        var points = new[]
        {
            new PointF(x, y - radius),
            new PointF(x + radius, y),
            new PointF(x, y + radius),
            new PointF(x - radius, y)
        };
        graphics.FillPolygon(brush, points);
    }

    private static void DrawSpeedTag(Graphics graphics, Font font, int centerX, int barTop, double value, int minTop)
    {
        string text = Math.Round(value).ToString();
        Size textSize = TextRenderer.MeasureText(graphics, text, font, Size.Empty, TextFormatFlags.NoPrefix);
        int width = textSize.Width + 12;
        int height = 18;
        int x = centerX - width / 2;
        int y = Math.Max(minTop, barTop - height - 5);

        DrawTag(graphics, new Rectangle(x, y, width, height), text, font, Ink, 4);
    }

    private static void DrawAccuracyTag(
        Graphics graphics,
        Font font,
        Font ratioFont,
        int centerX,
        int barTop,
        KeyReport key,
        int minTop)
    {
        const int width = 56;
        const int height = 32;
        int x = centerX - width / 2;
        int y = Math.Max(minTop, barTop - height - 5);
        var bounds = new Rectangle(x, y, width, height);

        using (GraphicsPath path = RoundedPanel.CreateRoundedPath(bounds, 4))
        using (var fill = new SolidBrush(Color.White))
        using (var border = new Pen(Axis, 1f))
        {
            graphics.FillPath(fill, path);
            graphics.DrawPath(border, path);
        }

        string percent = $"{Math.Round(key.Accuracy)}%";
        string ratio = $"{key.Correct}:{key.Presses}";
        TextRenderer.DrawText(
            graphics,
            percent,
            font,
            new Rectangle(bounds.X, bounds.Y + 2, bounds.Width, 16),
            Ink,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(
            graphics,
            ratio,
            ratioFont,
            new Rectangle(bounds.X, bounds.Y + 15, bounds.Width, 15),
            Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    private static void DrawTag(
        Graphics graphics,
        Rectangle bounds,
        string text,
        Font font,
        Color color,
        int radius)
    {
        using (GraphicsPath path = RoundedPanel.CreateRoundedPath(bounds, radius))
        using (var fill = new SolidBrush(Color.White))
        using (var border = new Pen(Axis, 1f))
        {
            graphics.FillPath(fill, path);
            graphics.DrawPath(border, path);
        }

        TextRenderer.DrawText(
            graphics,
            text,
            font,
            bounds,
            color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    private static void DrawKeyPill(
        Graphics graphics,
        KeyReport key,
        int centerX,
        int top,
        int slot,
        int barWidth)
    {
        Color finger = KeyboardPanel.KeyColor(key.Key);
        string label = key.Label;

        using var font = new Font("Times New Roman", 8.5f, FontStyle.Bold);
        Size textSize = TextRenderer.MeasureText(graphics, label, font, Size.Empty, TextFormatFlags.NoPrefix);
        int width = Math.Min(slot - 4, Math.Max(barWidth + 12, textSize.Width + 16));
        width = Math.Max(26, width);
        int height = 20;
        var bounds = new Rectangle(centerX - width / 2, top, width, height);

        using GraphicsPath path = RoundedPanel.CreateRoundedPath(bounds, 5);
        using var fill = new SolidBrush(Lighten(finger, 0.74f));
        using var border = new Pen(Darken(finger, 0.12f), 1f);
        graphics.FillPath(fill, path);
        graphics.DrawPath(border, path);

        TextRenderer.DrawText(
            graphics,
            label,
            font,
            bounds,
            Ink,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    private static Color Lighten(Color color, float amount)
    {
        return Color.FromArgb(
            color.A,
            (int)Math.Min(255, color.R + (255 - color.R) * amount),
            (int)Math.Min(255, color.G + (255 - color.G) * amount),
            (int)Math.Min(255, color.B + (255 - color.B) * amount));
    }

    private static Color Darken(Color color, float amount)
    {
        return Color.FromArgb(
            color.A,
            (int)Math.Max(0, color.R * (1f - amount)),
            (int)Math.Max(0, color.G * (1f - amount)),
            (int)Math.Max(0, color.B * (1f - amount)));
    }
}
