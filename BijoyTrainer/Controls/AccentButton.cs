using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BijoyTrainer.Controls;

/// <summary>
/// A flat, rounded, owner-drawn button with hover and pressed states.
/// </summary>
public class AccentButton : Button
{
    private bool _hovered;
    private bool _pressed;

    private Color _baseColor = Color.FromArgb(79, 70, 229);
    private Color _hoverColor = Color.FromArgb(67, 56, 202);
    private Color _pressedColor = Color.FromArgb(55, 48, 163);
    private Color _textColor = Color.White;

    /// <summary>Initializes a new instance of the <see cref="AccentButton"/> class.</summary>
    public AccentButton()
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
        Height = 38;
    }

    /// <summary>Gets or sets the normal background colour.</summary>
    public Color BaseColor
    {
        get => _baseColor;
        set { _baseColor = value; Invalidate(); }
    }

    /// <summary>Gets or sets the hover background colour.</summary>
    public Color HoverColor
    {
        get => _hoverColor;
        set { _hoverColor = value; Invalidate(); }
    }

    /// <summary>Gets or sets the pressed background colour.</summary>
    public Color PressedColor
    {
        get => _pressedColor;
        set { _pressedColor = value; Invalidate(); }
    }

    /// <summary>Gets or sets the text colour.</summary>
    public Color TextColor
    {
        get => _textColor;
        set { _textColor = value; Invalidate(); }
    }

    /// <summary>Gets or sets the corner radius.</summary>
    public int CornerRadius { get; set; } = 10;

    /// <inheritdoc />
    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        _pressed = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs pevent)
    {
        Graphics graphics = pevent.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent?.BackColor ?? Color.White);

        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        Color color = !Enabled
            ? Color.FromArgb(186, 192, 220)
            : _pressed
                ? PressedColor
                : _hovered
                    ? HoverColor
                    : BaseColor;

        Color textColor = !Enabled ? Color.FromArgb(148, 153, 184) : TextColor;

        using (GraphicsPath path = RoundedPanel.CreateRoundedPath(bounds, CornerRadius))
        using (var brush = new LinearGradientBrush(
                   bounds,
                   Lighten(color, 0.10f),
                   color,
                   LinearGradientMode.Vertical))
        {
            graphics.FillPath(brush, path);
        }

        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            bounds,
            textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
        base.OnEnabledChanged(e);
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
