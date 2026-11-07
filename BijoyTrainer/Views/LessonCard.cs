using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using BijoyTrainer.Controls;

namespace BijoyTrainer.Views;

/// <summary>
/// Immutable description of a single lesson shown on the dashboard.
/// </summary>
/// <param name="Number">The one-based lesson number.</param>
/// <param name="Title">Short lesson title.</param>
/// <param name="Description">One or two line description of the lesson.</param>
/// <param name="Category">Category label, for example <c>Beginner</c>.</param>
/// <param name="Time">Estimated completion time, for example <c>5 min</c>.</param>
/// <param name="Icon">Glyph shown inside the card's icon chip.</param>
/// <param name="Completed">Whether the learner has already completed the lesson.</param>
public sealed record LessonCardModel(
    int Number,
    string Title,
    string Description,
    string Category,
    string Time,
    string Icon,
    bool Completed);

/// <summary>
/// A rounded dashboard card that summarises one lesson and offers a button to
/// start (or repeat) it. Clicking anywhere on the card also starts the lesson.
/// </summary>
public sealed class LessonCard : RoundedPanel
{
    private const int ButtonWidth = 116;
    private const int ButtonHeight = 32;

    private readonly Font _iconFont;
    private readonly Font _titleFont;
    private readonly Font _descriptionFont;
    private readonly Font _metaFont;
    private readonly Font _statusFont;
    private readonly Font _statusBoldFont;
    private readonly Font _buttonFont;

    private AccentButton? _startButton;

    /// <summary>
    /// Initializes a new instance of the <see cref="LessonCard"/> class.
    /// </summary>
    /// <param name="model">The lesson to display. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/>.</exception>
    public LessonCard(LessonCardModel model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));

        DoubleBuffered = true;
        CornerRadius = 14;
        FillColor = Color.White;
        BorderColor = Theme.Line;
        MinimumSize = new Size(220, 180);
        Height = 196;
        Cursor = Cursors.Hand;
        Padding = Padding.Empty;

        _iconFont = Theme.CreateFont(16f);
        _titleFont = Theme.CreateFont(12.5f, FontStyle.Bold);
        _descriptionFont = Theme.CreateFont(9f);
        _metaFont = Theme.CreateFont(8.5f);
        _statusFont = Theme.CreateFont(8.5f);
        _statusBoldFont = Theme.CreateFont(8.5f, FontStyle.Bold);
        _buttonFont = Theme.CreateFont(8.5f, FontStyle.Bold);

        bool completed = Model.Completed;

        _startButton = new AccentButton
        {
            Size = new Size(ButtonWidth, ButtonHeight),
            CornerRadius = 9,
            Font = _buttonFont,
            Text = completed ? "Practice again →" : "Start lesson →",
            BaseColor = completed ? Theme.SoftFill : Theme.Accent,
            HoverColor = completed ? Color.FromArgb(224, 231, 255) : Theme.AccentDark,
            PressedColor = completed ? Theme.SoftBorder : Theme.AccentDeep,
            TextColor = completed ? Theme.AccentDark : Color.White
        };
        _startButton.Click += OnStartButtonClick;

        Controls.Add(_startButton);
        PositionButton();
    }

    /// <summary>Gets the lesson displayed by this card.</summary>
    public LessonCardModel Model { get; }

    /// <summary>
    /// Raised when the learner asks to start (or repeat) the lesson. The event
    /// argument is the lesson number.
    /// </summary>
    public event EventHandler<int>? StartRequested;

    /// <inheritdoc />
    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        StartRequested?.Invoke(this, Model.Number);
    }

    /// <inheritdoc />
    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        PositionButton();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle chip = new(18, 18, 44, 44);
        using (GraphicsPath chipPath = Theme.RoundedPath(chip, 12))
        using (var chipFill = new SolidBrush(Theme.SoftFill))
        using (var chipBorder = new Pen(Theme.SoftBorder, 1f))
        {
            graphics.FillPath(chipFill, chipPath);
            graphics.DrawPath(chipBorder, chipPath);
        }

        TextRenderer.DrawText(
            graphics,
            Model.Icon,
            _iconFont,
            chip,
            Theme.AccentDark,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        int contentWidth = Width - 36;
        if (contentWidth <= 0)
        {
            return;
        }

        TextRenderer.DrawText(
            graphics,
            Model.Title,
            _titleFont,
            new Rectangle(18, 76, contentWidth, 22),
            Theme.Ink,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);

        TextRenderer.DrawText(
            graphics,
            Model.Description,
            _descriptionFont,
            new Rectangle(18, 102, contentWidth, 34),
            Theme.Muted,
            TextFormatFlags.Left |
            TextFormatFlags.Top |
            TextFormatFlags.WordBreak |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);

        TextRenderer.DrawText(
            graphics,
            $"{Model.Category} · {Model.Time}",
            _metaFont,
            new Rectangle(18, 136, contentWidth, 16),
            Theme.Muted,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);

        Rectangle progress = new(18, Height - 30, contentWidth, 6);
        Theme.DrawProgress(graphics, progress, Model.Completed ? 1d : 0d);

        TextRenderer.DrawText(
            graphics,
            Model.Completed ? "✓ Completed" : "Not started",
            Model.Completed ? _statusBoldFont : _statusFont,
            new Rectangle(18, Height - 22, contentWidth, 18),
            Model.Completed ? Theme.Correct : Theme.Muted,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_startButton is not null)
            {
                _startButton.Click -= OnStartButtonClick;
                _startButton.Dispose();
                _startButton = null;
            }

            _iconFont.Dispose();
            _titleFont.Dispose();
            _descriptionFont.Dispose();
            _metaFont.Dispose();
            _statusFont.Dispose();
            _statusBoldFont.Dispose();
            _buttonFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void PositionButton()
    {
        if (_startButton is null)
        {
            return;
        }

        _startButton.Location = new Point(Width - 18 - ButtonWidth, Height - 44);
    }

    private void OnStartButtonClick(object? sender, EventArgs e)
    {
        StartRequested?.Invoke(this, Model.Number);
    }
}

/// <summary>
/// A three-column grid of <see cref="LessonCard"/> controls. The grid rebuilds
/// itself when the lesson list or the column count changes.
/// </summary>
public sealed class LessonGrid : TableLayoutPanel
{
    private const float CardRowHeight = 196f;

    private IReadOnlyList<LessonCardModel> _lessons = Array.Empty<LessonCardModel>();
    private int _columns = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="LessonGrid"/> class.
    /// </summary>
    public LessonGrid()
    {
        DoubleBuffered = true;
        BackColor = Theme.Page;
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        ColumnCount = 3;
        RowCount = 0;
        Dock = DockStyle.Fill;
    }

    /// <summary>
    /// Raised when one of the cards asks to start its lesson. The event
    /// argument is the lesson number.
    /// </summary>
    public event EventHandler<int>? LessonSelected;

    /// <summary>
    /// Replaces the displayed lessons and rebuilds the grid.
    /// </summary>
    /// <param name="lessons">Lessons to show; <see langword="null"/> is treated as empty.</param>
    public void SetLessons(IReadOnlyList<LessonCardModel>? lessons)
    {
        _lessons = lessons is null ? Array.Empty<LessonCardModel>() : lessons.ToArray();
        Rebuild();
    }

    /// <summary>
    /// Changes the number of columns and rebuilds the grid. The value is
    /// clamped to the range 1 through 3.
    /// </summary>
    /// <param name="columns">Desired column count.</param>
    public void SetColumns(int columns)
    {
        int clamped = Math.Clamp(columns, 1, 3);
        if (clamped == _columns)
        {
            return;
        }

        _columns = clamped;
        Rebuild();
    }

    private void Rebuild()
    {
        SuspendLayout();
        try
        {
            for (int i = Controls.Count - 1; i >= 0; i--)
            {
                Control child = Controls[i];
                if (child is LessonCard card)
                {
                    card.StartRequested -= OnCardStartRequested;
                }

                Controls.RemoveAt(i);
                child.Dispose();
            }

            int rowCount = _lessons.Count == 0
                ? 0
                : (_lessons.Count + _columns - 1) / _columns;

            // Setting the counts synchronises the style collections, so clear
            // them afterwards and add exactly the styles we want.
            ColumnCount = _columns;
            RowCount = rowCount;
            ColumnStyles.Clear();
            RowStyles.Clear();

            for (int c = 0; c < _columns; c++)
            {
                ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / _columns));
            }

            for (int r = 0; r < rowCount; r++)
            {
                RowStyles.Add(new RowStyle(SizeType.Absolute, CardRowHeight));
            }

            for (int i = 0; i < _lessons.Count; i++)
            {
                int column = i % _columns;
                var card = new LessonCard(_lessons[i])
                {
                    Margin = new Padding(0, 0, column == _columns - 1 ? 0 : 12, 12),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                card.StartRequested += OnCardStartRequested;
                Controls.Add(card, column, i / _columns);
            }
        }
        finally
        {
            ResumeLayout(true);
        }
    }

    private void OnCardStartRequested(object? sender, int number)
    {
        LessonSelected?.Invoke(this, number);
    }
}
