using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BijoyTrainer.Views;

/// <summary>
/// The learning dashboard. Composes the navigation bar, the hero banner, the
/// stats strip, the lesson grids and the achievements gallery into the three
/// switchable sections of the home screen.
/// </summary>
public sealed class HomeView : UserControl
{
    private const string DashboardKey = "dashboard";
    private const string LessonsKey = "lessons";
    private const string AchievementsKey = "achievements";
    private const string PracticeKey = "practice";

    private readonly Font _eyebrowFont = Theme.CreateFont(8f, FontStyle.Bold);
    private readonly Font _titleFont = Theme.CreateFont(26f, FontStyle.Bold);
    private readonly Font _subtitleFont = Theme.CreateFont(10f);
    private readonly Font _sectionTitleFont = Theme.CreateFont(15f, FontStyle.Bold);
    private readonly Font _linkFont = Theme.CreateFont(9f, FontStyle.Bold);

    private HomeNavBar _nav = null!;
    private Panel _content = null!;
    private HeroPanel _hero = null!;
    private StatsGrid _stats = null!;
    private LessonGrid _dashboardGrid = null!;
    private LessonGrid _lessonsGrid = null!;
    private AchievementsPanel _achievements = null!;

    private TableLayoutPanel _dashboardSection = null!;
    private TableLayoutPanel _lessonsSection = null!;
    private TableLayoutPanel _achievementsSection = null!;

    private Panel _dashboardPage = null!;
    private Panel _lessonsPage = null!;
    private Panel _achievementsPage = null!;

    private Label _dashboardTitle = null!;
    private Label _lessonsTitle = null!;
    private Label _achievementsTitle = null!;
    private Label _dashboardSubtitle = null!;
    private Label _lessonsSubtitle = null!;
    private Label _achievementsSubtitle = null!;

    private IReadOnlyList<LessonCardModel> _lessons = Array.Empty<LessonCardModel>();
    private int _completed;
    private int _sessions;
    private int _columns;

    /// <summary>
    /// Raised when a lesson card is chosen. The argument is the 1-based lesson
    /// number the host should load.
    /// </summary>
    public event EventHandler<int>? LessonRequested;

    /// <summary>
    /// Raised when the host should switch to another part of the application.
    /// Currently raised with <c>"practice"</c> for the Quick Practice nav item.
    /// </summary>
    public event EventHandler<string>? NavigationRequested;

    /// <summary>
    /// Initializes the dashboard, builds every section and shows the dashboard
    /// section by default.
    /// </summary>
    public HomeView()
    {
        BackColor = Theme.Page;
        Dock = DockStyle.Fill;
        DoubleBuffered = true;

        _nav = new HomeNavBar
        {
            Dock = DockStyle.Top,
            Height = 46,
        };
        _nav.PageRequested += OnNavPageRequested;

        _content = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Page,
        };

        _hero = new HeroPanel
        {
            Dock = DockStyle.Top,
            Height = 190,
            MinimumSize = new Size(0, 190),
            Margin = new Padding(0, 0, 0, 12),
        };
        _hero.ExploreClicked += (_, _) => ShowSection(LessonsKey);

        _stats = new StatsGrid
        {
            Dock = DockStyle.Top,
            Height = 88,
            MinimumSize = new Size(0, 88),
            Margin = new Padding(0, 0, 0, 14),
        };

        _dashboardGrid = CreateLessonGrid();
        _dashboardGrid.LessonSelected += OnLessonSelected;

        _lessonsGrid = CreateLessonGrid();
        _lessonsGrid.LessonSelected += OnLessonSelected;

        _achievements = new AchievementsPanel
        {
            Dock = DockStyle.Top,
            Height = 520,
            MinimumSize = new Size(0, 520),
            Margin = new Padding(0, 2, 0, 0),
        };

        _dashboardSection = BuildDashboardSection();
        _lessonsSection = BuildLessonsSection();
        _achievementsSection = BuildAchievementsSection();

        // Each section lives in its own scrollable dock-fill page. Toggling
        // Visible between stacked Dock=Fill pages is bulletproof, unlike
        // collapsing rows inside a shared AutoSize TableLayoutPanel.
        _dashboardPage = CreatePage(_dashboardSection);
        _lessonsPage = CreatePage(_lessonsSection);
        _achievementsPage = CreatePage(_achievementsSection);

        _content.Controls.Add(_dashboardPage);
        _content.Controls.Add(_lessonsPage);
        _content.Controls.Add(_achievementsPage);

        // Docked controls are laid out from the last child to the first, so the
        // fill panel is added before the top bar.
        Controls.Add(_content);
        Controls.Add(_nav);

        ShowSection(DashboardKey);
        UpdateColumns(true);
        UpdateTextWidths();
        SetProgress(0, 0);
    }

    /// <summary>
    /// Gets or sets the logo image shown in the hero card. The caller keeps
    /// ownership of the image.
    /// </summary>
    public Image? Logo
    {
        get => _hero.Logo;
        set => _hero.Logo = value;
    }

    /// <summary>Replaces the lesson catalogue shown on the dashboard and lessons sections.</summary>
    /// <param name="lessons">The lessons supplied by the host; may be empty.</param>
    public void SetLessons(IReadOnlyList<LessonCardModel> lessons)
    {
        _lessons = lessons ?? Array.Empty<LessonCardModel>();

        _dashboardGrid.SetLessons(_lessons.Take(3).ToList());
        _lessonsGrid.SetLessons(_lessons);
    }

    /// <summary>Updates the progress figures shown on the dashboard and achievements sections.</summary>
    /// <param name="completed">Number of completed lessons.</param>
    /// <param name="sessions">Number of finished practice sessions.</param>
    public void SetProgress(int completed, int sessions)
    {
        _completed = Math.Max(0, completed);
        _sessions = Math.Max(0, sessions);

        List<AchievementsPanel.BadgeModel> badges = BuildBadges(_completed, _sessions);
        int unlocked = badges.Count(badge => badge.Unlocked);

        _stats.SetValues(_completed, unlocked, _sessions, 0);
        _achievements.SetSummary(unlocked, badges.Count, _completed, _sessions);
        _achievements.SetBadges(badges);
    }

    /// <summary>
    /// Shows one of the three home sections and resets the scroll position to
    /// the top. Unknown names fall back to the dashboard.
    /// </summary>
    /// <param name="section">One of <c>dashboard</c>, <c>lessons</c> or <c>achievements</c>.</param>
    public void ShowSection(string section)
    {
        string key = NormalizeSection(section);

        _dashboardPage.Visible = key == DashboardKey;
        _lessonsPage.Visible = key == LessonsKey;
        _achievementsPage.Visible = key == AchievementsKey;

        _nav.SetActive(key);

        _dashboardPage.AutoScrollPosition = new Point(0, 0);
        _lessonsPage.AutoScrollPosition = new Point(0, 0);
        _achievementsPage.AutoScrollPosition = new Point(0, 0);
    }

    /// <inheritdoc />
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (_dashboardGrid is null || _achievementsSection is null)
        {
            return;
        }

        UpdateColumns(false);
        UpdateTextWidths();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _eyebrowFont.Dispose();
            _titleFont.Dispose();
            _subtitleFont.Dispose();
            _sectionTitleFont.Dispose();
            _linkFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnNavPageRequested(object? sender, string page)
    {
        if (string.Equals(page, PracticeKey, StringComparison.OrdinalIgnoreCase))
        {
            NavigationRequested?.Invoke(this, PracticeKey);
            return;
        }

        ShowSection(page);
    }

    private void OnLessonSelected(object? sender, int lessonNumber)
    {
        LessonRequested?.Invoke(this, lessonNumber);
    }

    private static string NormalizeSection(string? section)
    {
        if (string.IsNullOrWhiteSpace(section))
        {
            return DashboardKey;
        }

        return section.Trim().ToLowerInvariant() switch
        {
            LessonsKey => LessonsKey,
            AchievementsKey => AchievementsKey,
            _ => DashboardKey,
        };
    }

    private void UpdateColumns(bool force)
    {
        int columns = ClientSize.Width >= 900 ? 3 : ClientSize.Width >= 620 ? 2 : 1;
        if (!force && columns == _columns)
        {
            return;
        }

        _columns = columns;
        _dashboardGrid.SetColumns(columns);
        _lessonsGrid.SetColumns(columns);
    }

    private void UpdateTextWidths()
    {
        int width = Math.Max(240, ClientSize.Width - 60);
        var maximum = new Size(width, 0);

        _dashboardTitle.MaximumSize = maximum;
        _lessonsTitle.MaximumSize = maximum;
        _achievementsTitle.MaximumSize = maximum;
        _dashboardSubtitle.MaximumSize = maximum;
        _lessonsSubtitle.MaximumSize = maximum;
        _achievementsSubtitle.MaximumSize = maximum;
    }

    private TableLayoutPanel BuildDashboardSection()
    {
        TableLayoutPanel section = CreateSection(7);

        section.Controls.Add(CreateEyebrow("BIJOYTRAINER / LEARNING SPACE"), 0, 0);

        _dashboardTitle = CreateTitle("Master Bengali Typing.");
        section.Controls.Add(_dashboardTitle, 0, 1);

        _dashboardSubtitle = CreateSubtitle(
            "Build speed, accuracy and confidence in Bengali typing with short, guided lessons.");
        section.Controls.Add(_dashboardSubtitle, 0, 2);

        section.Controls.Add(_hero, 0, 3);
        section.Controls.Add(_stats, 0, 4);
        section.Controls.Add(BuildContinueHead(), 0, 5);
        section.Controls.Add(_dashboardGrid, 0, 6);

        return section;
    }

    private TableLayoutPanel BuildLessonsSection()
    {
        TableLayoutPanel section = CreateSection(4);

        section.Controls.Add(CreateEyebrow("BIJOYTRAINER / LESSONS"), 0, 0);

        _lessonsTitle = CreateTitle("Lessons");
        section.Controls.Add(_lessonsTitle, 0, 1);

        _lessonsSubtitle = CreateSubtitle("Choose a lesson and practice at your own pace.");
        section.Controls.Add(_lessonsSubtitle, 0, 2);
        section.Controls.Add(_lessonsGrid, 0, 3);

        return section;
    }

    private TableLayoutPanel BuildAchievementsSection()
    {
        TableLayoutPanel section = CreateSection(4);

        section.Controls.Add(CreateEyebrow("BIJOYTRAINER / ACHIEVEMENTS"), 0, 0);

        _achievementsTitle = CreateTitle("Achievements");
        section.Controls.Add(_achievementsTitle, 0, 1);

        _achievementsSubtitle = CreateSubtitle(
            "Every lesson completed is another step toward mastery.");
        section.Controls.Add(_achievementsSubtitle, 0, 2);
        section.Controls.Add(_achievements, 0, 3);

        return section;
    }

    private Control BuildContinueHead()
    {
        var head = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Page,
            Margin = new Padding(0, 4, 0, 6),
            Padding = Padding.Empty,
        };
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        head.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = "Continue Learning",
            AutoSize = true,
            Font = _sectionTitleFont,
            ForeColor = Theme.Ink,
            BackColor = Theme.Page,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0),
        };

        var link = new Button
        {
            Text = "View all lessons →",
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Theme.AccentDark,
            Font = _linkFont,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Anchor = AnchorStyles.Right,
            TabStop = false,
            Margin = new Padding(0, 4, 0, 0),
        };
        link.BackColor = Color.Transparent;
        link.FlatAppearance.BorderSize = 0;
        link.FlatAppearance.MouseOverBackColor = Color.Transparent;
        link.FlatAppearance.MouseDownBackColor = Color.Transparent;
        link.Click += (_, _) => ShowSection(LessonsKey);

        head.Controls.Add(title, 0, 0);
        head.Controls.Add(link, 1, 0);

        return head;
    }

    private static TableLayoutPanel CreateSection(int rows)
    {
        var section = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = rows,
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Page,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        for (int i = 0; i < rows; i++)
        {
            section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        return section;
    }

    private static Panel CreatePage(TableLayoutPanel section)
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Page,
            Margin = Padding.Empty,
        };

        section.Padding = new Padding(18, 6, 18, 16);
        page.Controls.Add(section);
        return page;
    }

    private static LessonGrid CreateLessonGrid() => new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        MinimumSize = Size.Empty,
        BackColor = Theme.Page,
        Margin = new Padding(0, 2, 0, 0),
    };

    private Label CreateEyebrow(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = _eyebrowFont,
        ForeColor = Theme.Muted,
        BackColor = Theme.Page,
        Margin = new Padding(0, 2, 0, 2),
    };

    private Label CreateTitle(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = _titleFont,
        ForeColor = Theme.Ink,
        BackColor = Theme.Page,
        MaximumSize = new Size(760, 0),
        Margin = new Padding(0, 0, 0, 2),
    };

    private Label CreateSubtitle(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = _subtitleFont,
        ForeColor = Theme.Muted,
        BackColor = Theme.Page,
        MaximumSize = new Size(760, 0),
        Margin = new Padding(0, 0, 0, 12),
    };

    private static List<AchievementsPanel.BadgeModel> BuildBadges(int completed, int sessions) => new()
    {
        new("First Steps", "🌱", "Complete your first lesson.", completed >= 1),
        new("Quick Learner", "📘", "Complete 3 lessons.", completed >= 3),
        new("Dedicated", "🔥", "Complete 5 lessons.", completed >= 5),
        new("Typing Expert", "⌨", "Complete 7 lessons.", completed >= 7),
        new("Master", "🏆", "Complete all 9 lessons.", completed >= 9),
        new("Practice Hero", "⚡", "Complete 10 practice sessions.", sessions >= 10),
    };
}
