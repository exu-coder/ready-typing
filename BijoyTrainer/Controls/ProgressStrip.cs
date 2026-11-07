using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BijoyTrainer.Controls;

/// <summary>
/// A slim rounded progress bar with an indigo-to-cyan gradient fill.
/// </summary>
public class ProgressStrip : Control
{
    private double _value;
    private Color _trackColor = Color.FromArgb(226, 228, 240);
    private Color _startColor = Color.FromArgb(79, 70, 229);
    private Color _endColor = Color.FromArgb(34, 211, 238);

    /// <summary>Initializes a new instance of the <see cref="ProgressStrip"/> class.</summary>
    public ProgressStrip()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.Transparent;
        Height = 8;
    }

    /// <summary>Gets or sets progress in the range 0..1.</summary>
    public double Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0d, 1d);
            Invalidate();
        }
    }

    /// <summary>Gets or sets the unfilled track colour.</summary>
    public Color TrackColor
    {
        get => _trackColor;
        set { _trackColor = value; Invalidate(); }
    }

    /// <summary>Gets or sets the gradient start colour.</summary>
    public Color StartColor
    {
        get => _startColor;
        set { _startColor = value; Invalidate(); }
    }

    /// <summary>Gets or sets the gradient end colour.</summary>
    public Color EndColor
    {
        get => _endColor;
        set { _endColor = value; Invalidate(); }
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent?.BackColor ?? Color.White);

        int radius = Height / 2;
        Rectangle track = new(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));

        using (GraphicsPath trackPath = RoundedPanel.CreateRoundedPath(track, radius))
        using (var trackBrush = new SolidBrush(TrackColor))
        {
            graphics.FillPath(trackBrush, trackPath);
        }

        int fillWidth = (int)Math.Round((Width - 1) * Value);
        if (fillWidth <= 1)
        {
            return;
        }

        Rectangle fill = new(0, 0, fillWidth, track.Height);
        using GraphicsPath fillPath = RoundedPanel.CreateRoundedPath(fill, Math.Min(radius, fillWidth / 2));
        using var gradient = new LinearGradientBrush(
            track,
            StartColor,
            EndColor,
            LinearGradientMode.Horizontal);
        graphics.FillPath(gradient, fillPath);
    }
}
