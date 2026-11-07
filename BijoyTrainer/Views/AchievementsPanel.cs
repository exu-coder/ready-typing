using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BijoyTrainer.Controls;

namespace BijoyTrainer.Views;

/// <summary>
/// Dashboard page that shows achievement statistics, an overall progress card
/// and a grid of owner-drawn badge cards.
/// </summary>
public sealed class AchievementsPanel : Panel
{
    /// <summary>Describes a single achievement badge.</summary>
    /// <param name="Name">Badge title.</param>
    /// <param name="Icon">Glyph shown inside the medal square when unlocked.</param>
    /// <param name="Description">Short requirement text.</param>
    /// <param name="Unlocked">Whether the badge has been earned.</param>
    public sealed record BadgeModel(string Name, string Icon, string Description, bool Unlocked);

    private readonly StatCard _unlockedCard;
    private readonly StatCard _totalCard;
    private readonly StatCard _completedCard;
    private readonly StatCard _sessionsCard;

    private readonly RoundedPanel _progressCard;
    private readonly Label _progressTitle;
    private readonly Label _progressPercent;
    private readonly ProgressLine _progressLine;

    private readonly TableLayoutPanel _badgeGrid;
    private readonly List<BadgeCard> _badgeCards = new();

    private readonly Font _progressTitleFont;
    private readonly Font _progressPercentFont;

    /// <summary>Initializes a new instance of the <see cref="AchievementsPanel"/> class.</summary>
    public AchievementsPanel()
    {
        BackColor = Theme.Page;
        Dock = DockStyle.Fill;
        AutoScroll = false;
        DoubleBuffered = true;
        Margin = Padding.Empty;
        Padding = Padding.Empty;

        _progressTitleFont = Theme.CreateFont(10.5f, FontStyle.Bold);
        _progressPercentFont = Theme.CreateFont(10.5f, FontStyle.Bold);

        _unlockedCard = CreateStat("UNLOCKED BADGES", Theme.Accent, new Padding(0, 0, 12, 0));
        _totalCard = CreateStat("TOTAL BADGES", Theme.Sky, new Padding(0, 0, 12, 0));
        _completedCard = CreateStat("COMPLETED LESSONS", Theme.Emerald, new Padding(0, 0, 12, 0));
        _sessionsCard = CreateStat("PRACTICE SESSIONS", Theme.Amber, Padding.Empty);

        var statsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Theme.Page,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        for (int i = 0; i < 4; i++)
        {
            statsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        }

        statsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        statsGrid.Controls.Add(_unlockedCard, 0, 0);
        statsGrid.Controls.Add(_totalCard, 1, 0);
        statsGrid.Controls.Add(_completedCard, 2, 0);
        statsGrid.Controls.Add(_sessionsCard, 3, 0);

        _progressTitle = new Label
        {
            Text = "Achievement progress",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Ink,
            BackColor = Color.White,
            Font = _progressTitleFont,
            Margin = Padding.Empty,
        };

        _progressPercent = new Label
        {
            Text = "0%",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Theme.Accent,
            BackColor = Color.White,
            Font = _progressPercentFont,
            Margin = Padding.Empty,
        };

        _progressLine = new ProgressLine
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 0, 0),
        };

        _progressCard = new RoundedPanel
        {
            Dock = DockStyle.Top,
            Height = 76,
            CornerRadius = 16,
            Margin = new Padding(0, 8, 0, 8),
            Padding = new Padding(18, 14, 18, 12),
        };

        var progressLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        progressLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        progressLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        progressLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10f));

        var progressHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.White,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        progressHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        progressHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
        progressHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        progressHeader.Controls.Add(_progressTitle, 0, 0);
        progressHeader.Controls.Add(_progressPercent, 1, 0);

        progressLayout.Controls.Add(progressHeader, 0, 0);
        progressLayout.Controls.Add(_progressLine, 0, 1);
        _progressCard.Controls.Add(progressLayout);

        _badgeGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            RowCount = 0,
            BackColor = Theme.Page,
            Margin = new Padding(0, 8, 0, 0),
            Padding = Padding.Empty,
        };
        for (int i = 0; i < 3; i++)
        {
            _badgeGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        }

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Page,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.Controls.Add(statsGrid, 0, 0);
        root.Controls.Add(_progressCard, 0, 1);
        root.Controls.Add(_badgeGrid, 0, 2);

        Controls.Add(root);
        SetSummary(0, 0, 0, 0);
    }

    /// <summary>
    /// Updates the four statistic cards and the overall achievement progress.
    /// Counts are clamped to sensible ranges and <paramref name="unlocked"/>
    /// can never exceed <paramref name="total"/>.
    /// </summary>
    /// <param name="unlocked">Number of earned badges.</param>
    /// <param name="total">Number of available badges.</param>
    /// <param name="completed">Number of completed lessons.</param>
    /// <param name="sessions">Number of practice sessions.</param>
    public void SetSummary(int unlocked, int total, int completed, int sessions)
    {
        int safeTotal = Math.Max(0, total);
        int safeUnlocked = Math.Clamp(unlocked, 0, safeTotal);
        int safeCompleted = Math.Max(0, completed);
        int safeSessions = Math.Max(0, sessions);

        _unlockedCard.Value = safeUnlocked.ToString();
        _totalCard.Value = safeTotal.ToString();
        _completedCard.Value = safeCompleted.ToString();
        _sessionsCard.Value = safeSessions.ToString();

        double fraction = safeTotal == 0 ? 0d : (double)safeUnlocked / safeTotal;
        int percent = (int)Math.Round(fraction * 100d, MidpointRounding.AwayFromZero);

        _progressPercent.Text = percent.ToString() + "%";
        _progressLine.Value = fraction;
    }

    /// <summary>
    /// Rebuilds the badge grid from the supplied models, disposing any cards
    /// that were previously shown.
    /// </summary>
    /// <param name="badges">Badges to display; null or an empty list clears the grid.</param>
    public void SetBadges(IReadOnlyList<BadgeModel> badges)
    {
        _badgeGrid.SuspendLayout();

        foreach (BadgeCard card in _badgeCards)
        {
            _badgeGrid.Controls.Remove(card);
            card.Dispose();
        }

        _badgeCards.Clear();

        int count = badges?.Count ?? 0;
        int rows = (count + 2) / 3;

        _badgeGrid.RowCount = rows;
        _badgeGrid.RowStyles.Clear();

        for (int row = 0; row < rows; row++)
        {
            _badgeGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f));
        }

        for (int i = 0; i < count; i++)
        {
            int column = i % 3;
            var card = new BadgeCard(badges![i])
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, column == 2 ? 0 : 12, 0),
            };

            _badgeCards.Add(card);
            _badgeGrid.Controls.Add(card, column, i / 3);
        }

        _badgeGrid.ResumeLayout();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _progressTitleFont.Dispose();
            _progressPercentFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private static StatCard CreateStat(string caption, Color accent, Padding margin)
    {
        return new StatCard
        {
            Caption = caption,
            Value = "0",
            AccentColor = accent,
            Dock = DockStyle.Fill,
            Margin = margin,
        };
    }

    private sealed class ProgressLine : Control
    {
        private double _value;

        public ProgressLine()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw,
                true);

            BackColor = Color.Transparent;
            Height = 9;
        }

        public double Value
        {
            get => _value;
            set
            {
                _value = Math.Clamp(value, 0d, 1d);
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Parent?.BackColor ?? Color.White);

            Rectangle bounds = new(0, 0, Width - 1, Height - 1);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            Theme.DrawProgress(graphics, bounds, _value);
        }
    }

    private sealed class BadgeCard : Control
    {
        private static readonly Color MedalFill = Theme.SoftFill;
        private static readonly Color MedalBorder = Theme.SoftBorder;
        private static readonly Color LockedMedalFill = Color.FromArgb(235, 236, 246);

        private readonly BadgeModel _model;
        private readonly Font _iconFont;
        private readonly Font _nameFont;
        private readonly Font _descriptionFont;
        private readonly Font _statusFont;

        public BadgeCard(BadgeModel model)
        {
            _model = model;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw,
                true);

            BackColor = Color.Transparent;
            Height = 150;
            Margin = Padding.Empty;

            _iconFont = Theme.CreateFont(24f);
            _nameFont = Theme.CreateFont(10.5f, FontStyle.Bold);
            _descriptionFont = Theme.CreateFont(8.5f);
            _statusFont = Theme.CreateFont(8f, FontStyle.Bold);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Parent?.BackColor ?? Theme.Page);

            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            Theme.DrawCard(graphics, new Rectangle(0, 0, Width - 1, Height - 1), 14);

            bool unlocked = _model.Unlocked;
            string icon = unlocked
                ? (string.IsNullOrEmpty(_model.Icon) ? "★" : _model.Icon)
                : "🔒";

            var medal = new Rectangle((Width - 64) / 2, 12, 64, 64);
            using (GraphicsPath path = Theme.RoundedPath(medal, 18))
            using (var fill = new SolidBrush(unlocked ? MedalFill : LockedMedalFill))
            using (var border = new Pen(unlocked ? MedalBorder : Theme.Line, 1f))
            {
                graphics.FillPath(fill, path);
                graphics.DrawPath(border, path);
            }

            TextRenderer.DrawText(
                graphics,
                icon,
                _iconFont,
                medal,
                Theme.Ink,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.NoPadding);

            TextRenderer.DrawText(
                graphics,
                _model.Name ?? string.Empty,
                _nameFont,
                new Rectangle(10, 78, Width - 20, 18),
                unlocked ? Theme.Ink : Theme.Muted,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.EndEllipsis);

            TextRenderer.DrawText(
                graphics,
                _model.Description ?? string.Empty,
                _descriptionFont,
                new Rectangle(14, 96, Width - 28, 32),
                Theme.Muted,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.Top |
                TextFormatFlags.WordBreak |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.EndEllipsis);

            TextRenderer.DrawText(
                graphics,
                unlocked ? "✓ UNLOCKED" : "LOCKED",
                _statusFont,
                new Rectangle(10, Height - 22, Width - 20, 16),
                unlocked ? Theme.Correct : Theme.Muted,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _iconFont.Dispose();
                _nameFont.Dispose();
                _descriptionFont.Dispose();
                _statusFont.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
