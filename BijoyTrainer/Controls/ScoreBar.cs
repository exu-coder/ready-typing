using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BijoyTrainer.Controls;

/// <summary>
/// A labelled gradient bar with a value marker, modelled on the overall rating
/// and speed bars in RapidTyping's lesson results. The caption sits above the
/// right-hand end, the marker shows the current value on the bar and optional
/// labels are drawn underneath.
/// </summary>
public class ScoreBar : Control
{
    private static readonly Color CaptionInk = Color.FromArgb(67, 56, 202);
    private static readonly Color LabelInk = Color.FromArgb(70, 68, 96);
    private static readonly Color MarkerRing = Color.FromArgb(6, 182, 212);

    private string _caption = string.Empty;
    private string _valueText = string.Empty;
    private double _fraction;
    private double _tickFraction = -1d;
    private string _tickText = string.Empty;
    private IReadOnlyList<BarLabel> _labels = Array.Empty<BarLabel>();

    /// <summary>Initializes a new instance of the <see cref="ScoreBar"/> class.</summary>
    public ScoreBar()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.Transparent;
        Font = new Font("Times New Roman", 9f);
    }

    /// <summary>Gets or sets the caption drawn above the bar.</summary>
    public string Caption
    {
        get => _caption;
        set { _caption = value ?? string.Empty; Invalidate(); }
    }

    /// <summary>Gets or sets the value appended to the caption.</summary>
    public string ValueText
    {
        get => _valueText;
        set { _valueText = value ?? string.Empty; Invalidate(); }
    }

    /// <summary>Gets or sets the marker position, from 0 to 1.</summary>
    public double Fraction
    {
        get => _fraction;
        set { _fraction = Math.Clamp(value, 0d, 1d); Invalidate(); }
    }

    /// <summary>Gets or sets a single reference mark drawn below the bar.</summary>
    public double TickFraction
    {
        get => _tickFraction;
        set { _tickFraction = value; Invalidate(); }
    }

    /// <summary>Gets or sets the reference mark text.</summary>
    public string TickText
    {
        get => _tickText;
        set { _tickText = value ?? string.Empty; Invalidate(); }
    }

    /// <summary>Gets or sets the labels spread underneath the bar.</summary>
    public IReadOnlyList<BarLabel> Labels
    {
        get => _labels;
        set { _labels = value ?? Array.Empty<BarLabel>(); Invalidate(); }
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
        int height = ClientSize.Height;
        if (width <= 20 || height <= 20)
        {
            return;
        }

        const int side = 10;
        int barTop = 24;
        int barHeight = 12;
        int barWidth = Math.Max(10, width - side * 2);

        string caption = string.IsNullOrEmpty(ValueText)
            ? Caption
            : Caption + " " + ValueText;

        using (var captionFont = new Font("Times New Roman", 10f, FontStyle.Bold))
        {
            TextRenderer.DrawText(
                graphics,
                caption,
                captionFont,
                new Rectangle(0, 0, width, 20),
                CaptionInk,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        var track = new Rectangle(side, barTop, barWidth, barHeight);
        using (GraphicsPath path = RoundedPanel.CreateRoundedPath(track, barHeight / 2))
        {
            using var brush = CreateGradient(track);
            graphics.FillPath(brush, path);
        }

        int markerX = side + (int)Math.Round(Fraction * barWidth);
        int markerY = barTop + barHeight / 2;

        using (var shadow = new SolidBrush(Color.FromArgb(38, 30, 27, 75)))
        {
            graphics.FillEllipse(shadow, markerX - 8, markerY - 5, 16, 16);
        }

        using (var fill = new SolidBrush(Color.White))
        using (var ring = new Pen(MarkerRing, 2.2f))
        {
            graphics.FillEllipse(fill, markerX - 7, markerY - 7, 14, 14);
            graphics.DrawEllipse(ring, markerX - 7, markerY - 7, 14, 14);
        }

        int labelTop = barTop + barHeight + 10;
        using (var labelFont = new Font("Times New Roman", 9f))
        {
            foreach (BarLabel label in Labels)
            {
                DrawLabel(graphics, labelFont, label, side, barWidth, labelTop);
            }

            if (TickFraction >= 0d && TickText.Length > 0)
            {
                DrawLabel(
                    graphics,
                    labelFont,
                    new BarLabel(TickFraction, TickText),
                    side,
                    barWidth,
                    labelTop);
            }
        }
    }

    private static void DrawLabel(
        Graphics graphics,
        Font font,
        BarLabel label,
        int left,
        int width,
        int top)
    {
        int x = left + (int)Math.Round(Math.Clamp(label.Fraction, 0d, 1d) * width);
        Size size = TextRenderer.MeasureText(graphics, label.Text, font, Size.Empty, TextFormatFlags.NoPrefix);
        int drawX = x - size.Width / 2;

        if (label.Fraction <= 0.0001d)
        {
            drawX = left;
        }
        else if (label.Fraction >= 0.9999d)
        {
            drawX = left + width - size.Width;
        }

        using var brush = new SolidBrush(LabelInk);
        graphics.DrawString(label.Text, font, brush, drawX, top);
    }

    private static LinearGradientBrush CreateGradient(Rectangle bounds)
    {
        var brush = new LinearGradientBrush(bounds, Color.Red, Color.Green, LinearGradientMode.Horizontal);
        var blend = new ColorBlend
        {
            Colors = new[]
            {
                Color.FromArgb(239, 68, 68),
                Color.FromArgb(249, 115, 22),
                Color.FromArgb(250, 204, 21),
                Color.FromArgb(16, 185, 129)
            },
            Positions = new[] { 0f, 0.34f, 0.62f, 1f }
        };

        brush.InterpolationColors = blend;
        return brush;
    }
}

/// <summary>A label anchored under a <see cref="ScoreBar"/>.</summary>
/// <param name="Fraction">Position across the bar, from 0 to 1.</param>
/// <param name="Text">Label text.</param>
public readonly record struct BarLabel(double Fraction, string Text);
