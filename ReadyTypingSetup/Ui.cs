using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ReadyTypingSetup;

/// <summary>A card-like panel with rounded corners, a soft shadow and a border.</summary>
internal sealed class RoundPanel : Panel
{
    public RoundPanel()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.White;
        Radius = 14;
        FillColor = Color.White;
        BorderColor = Color.FromArgb(226, 228, 240);
    }

    public int Radius { get; set; }

    public Color FillColor { get; set; }

    public Color BorderColor { get; set; }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent != null ? Parent.BackColor : Color.FromArgb(238, 240, 249));

        Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        using (GraphicsPath shadow = RoundedPath(
                   new Rectangle(1, 3, bounds.Width - 2, bounds.Height - 2), Radius))
        using (var shadowBrush = new SolidBrush(Color.FromArgb(16, 15, 23, 42)))
        {
            graphics.FillPath(shadowBrush, shadow);
        }

        using (GraphicsPath path = RoundedPath(bounds, Radius))
        using (var fill = new SolidBrush(FillColor))
        using (var border = new Pen(BorderColor, 1f))
        {
            graphics.FillPath(fill, path);
            graphics.DrawPath(border, path);
        }
    }

    public static GraphicsPath RoundedPath(Rectangle bounds, int radius)
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

/// <summary>A flat rounded button with hover and pressed states.</summary>
internal sealed class FlatButton : Button
{
    private bool _hovered;
    private bool _pressed;

    public FlatButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw,
            true);

        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        BackColor = Color.Transparent;
        Font = new Font("Times New Roman", 9.5f, FontStyle.Bold);
        Cursor = Cursors.Hand;
        Height = 40;
        Radius = 10;
        BaseColor = Color.FromArgb(79, 70, 229);
        HoverColor = Color.FromArgb(67, 56, 202);
        PressedColor = Color.FromArgb(55, 48, 163);
        TextColor = Color.White;
        BorderColor = Color.Transparent;
    }

    public Color BaseColor { get; set; }

    public Color HoverColor { get; set; }

    public Color PressedColor { get; set; }

    public Color TextColor { get; set; }

    public Color BorderColor { get; set; }

    public int Radius { get; set; }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        _pressed = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        Graphics graphics = pevent.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent != null ? Parent.BackColor : Color.Transparent);

        Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        Color color = _pressed ? PressedColor : _hovered ? HoverColor : BaseColor;

        using (GraphicsPath path = RoundPanel.RoundedPath(bounds, Radius))
        {
            if (color.A > 0)
            {
                using (var fill = new LinearGradientBrush(bounds, Lighten(color, 0.10f), color, LinearGradientMode.Vertical))
                {
                    graphics.FillPath(fill, path);
                }
            }

            if (BorderColor.A > 0)
            {
                using var border = new Pen(BorderColor, 1f);
                graphics.DrawPath(border, path);
            }
        }

        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            bounds,
            Enabled ? TextColor : Color.FromArgb(148, 163, 184),
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
}

/// <summary>A slim rounded progress bar with an indigo gradient fill.</summary>
internal sealed class ProgressBarEx : Control
{
    private double _value;

    public ProgressBarEx()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.Transparent;
        Height = 12;
    }

    public double Value
    {
        get => _value;
        set
        {
            _value = Math.Max(0d, Math.Min(1d, value));
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent != null ? Parent.BackColor : Color.White);

        Rectangle track = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        int radius = track.Height / 2;

        using (GraphicsPath trackPath = RoundPanel.RoundedPath(track, radius))
        using (var trackBrush = new SolidBrush(Color.FromArgb(226, 228, 240)))
        {
            graphics.FillPath(trackBrush, trackPath);
        }

        int fillWidth = (int)Math.Round((Width - 1) * _value);
        if (fillWidth <= 1)
        {
            return;
        }

        Rectangle fill = new Rectangle(0, 0, fillWidth, track.Height);
        using GraphicsPath fillPath = RoundPanel.RoundedPath(fill, Math.Min(radius, fillWidth / 2));
        using var gradient = new LinearGradientBrush(
            track,
            Color.FromArgb(79, 70, 229),
            Color.FromArgb(34, 211, 238),
            LinearGradientMode.Horizontal);
        graphics.FillPath(gradient, fillPath);
    }
}

/// <summary>Draws the green success tick used by the final setup page.</summary>
internal sealed class SuccessBadge : Control
{
    public SuccessBadge()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.Transparent;
        Size = new Size(64, 64);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent != null ? Parent.BackColor : Color.White);

        Rectangle circle = new Rectangle(1, 1, Width - 3, Height - 3);
        using (var fill = new LinearGradientBrush(
                   circle,
                   Color.FromArgb(16, 185, 129),
                   Color.FromArgb(5, 150, 105),
                   LinearGradientMode.Vertical))
        {
            graphics.FillEllipse(fill, circle);
        }

        using var tick = new Pen(Color.White, Math.Max(3f, Width / 14f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        graphics.DrawLines(
            tick,
            new[]
            {
                new Point(Width * 30 / 100, Height * 52 / 100),
                new Point(Width * 44 / 100, Height * 66 / 100),
                new Point(Width * 71 / 100, Height * 37 / 100)
            });
    }
}
