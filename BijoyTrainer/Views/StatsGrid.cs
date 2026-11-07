using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using BijoyTrainer.Controls;

namespace BijoyTrainer.Views;

/// <summary>
/// A single-row, four-column grid of <see cref="StatCard"/> controls summarising
/// the learner's progress on the dashboard.
/// </summary>
public sealed class StatsGrid : TableLayoutPanel
{
    private readonly StatCard _lessonsCard = new();
    private readonly StatCard _achievementsCard = new();
    private readonly StatCard _sessionsCard = new();
    private readonly StatCard _streakCard = new();

    private bool _ready;

    /// <summary>Initializes a new instance of the <see cref="StatsGrid"/> class.</summary>
    public StatsGrid()
    {
        SuspendLayout();

        ColumnCount = 4;
        RowCount = 1;
        Dock = DockStyle.Fill;
        BackColor = Theme.Page;
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        Height = 88;

        for (int i = 0; i < 4; i++)
        {
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        }

        RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        ConfigureCard(_lessonsCard, "LESSONS COMPLETED", Theme.Accent, new Padding(0, 0, 10, 0));
        ConfigureCard(_achievementsCard, "ACHIEVEMENTS", Theme.Sky, new Padding(0, 0, 10, 0));
        ConfigureCard(_sessionsCard, "PRACTICE SESSIONS", Theme.Emerald, new Padding(0, 0, 10, 0));
        ConfigureCard(_streakCard, "CURRENT STREAK", Theme.Amber, Padding.Empty);

        Controls.Add(_lessonsCard, 0, 0);
        Controls.Add(_achievementsCard, 1, 0);
        Controls.Add(_sessionsCard, 2, 0);
        Controls.Add(_streakCard, 3, 0);

        _ready = true;

        ResumeLayout(performLayout: true);
    }

    /// <summary>Gets the card showing completed lessons.</summary>
    public StatCard LessonsCard => _lessonsCard;

    /// <summary>Gets the card showing unlocked achievements.</summary>
    public StatCard AchievementsCard => _achievementsCard;

    /// <summary>Gets the card showing the number of practice sessions.</summary>
    public StatCard SessionsCard => _sessionsCard;

    /// <summary>Gets the card showing the current streak in days.</summary>
    public StatCard StreakCard => _streakCard;

    /// <summary>
    /// Updates every card at once. Negative values are clamped to zero, and the
    /// call is ignored until the constructor has finished building the children.
    /// </summary>
    /// <param name="lessonsCompleted">Number of lessons completed.</param>
    /// <param name="achievements">Number of achievements unlocked.</param>
    /// <param name="sessions">Number of practice sessions.</param>
    /// <param name="streakDays">Current streak length in days.</param>
    public void SetValues(int lessonsCompleted, int achievements, int sessions, int streakDays)
    {
        if (!_ready)
        {
            return;
        }

        _lessonsCard.Value = FormatCount(lessonsCompleted);
        _lessonsCard.Suffix = string.Empty;

        _achievementsCard.Value = FormatCount(achievements);
        _achievementsCard.Suffix = string.Empty;

        _sessionsCard.Value = FormatCount(sessions);
        _sessionsCard.Suffix = string.Empty;

        _streakCard.Value = FormatCount(streakDays);
        _streakCard.Suffix = "Days";
    }

    /// <summary>
    /// Updates only the streak card, leaving the other three values untouched.
    /// Negative values are clamped to zero.
    /// </summary>
    /// <param name="days">Current streak length in days.</param>
    public void SetStreak(int days)
    {
        if (!_ready)
        {
            return;
        }

        _streakCard.Value = FormatCount(days);
        _streakCard.Suffix = "Days";
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        // The four StatCards are child controls, so the base implementation
        // disposes them; disposing them here as well would double-dispose.
        base.Dispose(disposing);
    }

    private static void ConfigureCard(StatCard card, string caption, Color accent, Padding margin)
    {
        card.Caption = caption;
        card.AccentColor = accent;
        card.Dock = DockStyle.Fill;
        card.Margin = margin;
    }

    private static string FormatCount(int value) =>
        Math.Max(0, value).ToString(CultureInfo.InvariantCulture);
}
