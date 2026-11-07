using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BijoyTrainer.Controls;
using BijoyTrainer.Core;

namespace BijoyTrainer;

/// <summary>The action a learner chose on the lesson results screen.</summary>
public enum ResultsChoice
{
    /// <summary>Load the following lesson.</summary>
    NextLesson,

    /// <summary>Reset the current lesson and type it again.</summary>
    TryAgain
}

/// <summary>
/// The lesson results screen, drawn inside the main window instead of a
/// separate dialog. It reproduces the two RapidTyping views: a "Next step"
/// page with the overall rating, speed, slowdown bars and star rating, and a
/// "Detailed statistics" page with per-key speed and accuracy. Enter is the
/// only key that confirms a choice; the "Go on" button does the same thing
/// with the mouse.
/// </summary>
public sealed class ResultsView : UserControl
{
    private static readonly Color PageColor = Color.FromArgb(238, 240, 249);
    private static readonly Color Ink = Color.FromArgb(30, 27, 75);
    private static readonly Color Muted = Color.FromArgb(99, 96, 133);
    private static readonly Color Accent = Color.FromArgb(79, 70, 229);
    private static readonly Color AccentDark = Color.FromArgb(67, 56, 202);
    private static readonly Color Success = Color.FromArgb(16, 185, 129);
    private static readonly Color Warning = Color.FromArgb(180, 83, 9);
    private static readonly Color Border = Color.FromArgb(226, 228, 240);
    private static readonly Color BannerFill = Color.FromArgb(238, 242, 255);
    private static readonly Color BannerBorder = Color.FromArgb(199, 210, 254);

    private readonly LessonReport _report;

    private readonly RoundedPanel _banner;
    private readonly Label _bannerTitle;
    private readonly Label _bannerDetail;
    private readonly Label _bannerCounter;

    private readonly RoundedPanel _ratingCard;
    private readonly ScoreBar[] _bars = new ScoreBar[4];
    private readonly StarRating _stars;

    private readonly Panel _tabStrip;
    private Label _nextTab = null!;
    private Label _statsTab = null!;
    private Panel _nextUnderline = null!;
    private Panel _statsUnderline = null!;
    private Panel _tabLine = null!;

    private readonly RoundedPanel _pageCard;
    private readonly Panel _nextPage;
    private readonly Panel _statsPage;
    private readonly RadioButton _nextOption;
    private readonly RadioButton _retryOption;
    private Label _nextHeading = null!;
    private Label _nextDetail = null!;
    private Label _retryDetail = null!;
    private Label _choiceInfo = null!;

    private readonly KeyChart _speedChart;
    private readonly KeyChart _accuracyChart;

    private readonly AccentButton _goButton;
    private readonly Label _footerHint;

    private bool _confirmed;

    /// <summary>Initializes the results screen for a finished lesson.</summary>
    /// <param name="report">The finished lesson's statistics.</param>
    public ResultsView(LessonReport report)
    {
        _report = report ?? throw new ArgumentNullException(nameof(report));

        BackColor = PageColor;
        DoubleBuffered = true;
        Font = new Font("Times New Roman", 9f);

        _bannerTitle = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 15f, FontStyle.Bold),
            ForeColor = _report.Passed ? AccentDark : Warning,
            Text = _report.Passed ? "Lesson complete" : "Almost there",
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false
        };

        _bannerDetail = new Label
        {
            AutoEllipsis = true,
            AutoSize = false,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 9f),
            ForeColor = Muted,
            Text = _report.Passed
                ? $"{_report.Title}  ·  {Math.Round(_report.Rating)}% accuracy  ·  " +
                  $"{Math.Round(_report.WordsPerMinute)} WPM"
                : $"{_report.Title}  ·  {MistakeCount()}  ·  " +
                  "try again to unlock the next lesson",
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false
        };

        _bannerCounter = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 10f, FontStyle.Bold),
            ForeColor = AccentDark,
            Text = $"LESSON {_report.LessonNumber} / {_report.LessonCount}",
            TextAlign = ContentAlignment.MiddleRight,
            UseMnemonic = false
        };

        _banner = new RoundedPanel
        {
            Padding = new Padding(0),
            CornerRadius = 14,
            FillColor = BannerFill,
            BorderColor = BannerBorder
        };
        _banner.Controls.Add(_bannerTitle);
        _banner.Controls.Add(_bannerDetail);
        _banner.Controls.Add(_bannerCounter);

        _ratingCard = new RoundedPanel
        {
            Padding = new Padding(0),
            CornerRadius = 16,
            FillColor = Color.White
        };
        _stars = BuildRatingCard(_ratingCard);

        _tabStrip = new Panel { BackColor = PageColor };
        BuildTabStrip(_tabStrip);

        _nextPage = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        _statsPage = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Visible = false };

        (_nextOption, _retryOption) = BuildNextStepPage(_nextPage);
        (_speedChart, _accuracyChart) = BuildStatisticsPage(_statsPage);

        _pageCard = new RoundedPanel
        {
            Padding = new Padding(0),
            CornerRadius = 16,
            FillColor = Color.White
        };
        _pageCard.Controls.Add(_statsPage);
        _pageCard.Controls.Add(_nextPage);

        _goButton = new AccentButton
        {
            BaseColor = Accent,
            HoverColor = AccentDark,
            TextColor = Color.White,
            CornerRadius = 11,
            Font = new Font("Times New Roman", 11.5f, FontStyle.Bold),
            Text = "Go on",
            UseVisualStyleBackColor = false
        };
        _goButton.Click += (_, _) => Confirm();

        _footerHint = new Label
        {
            AutoSize = false,
            BackColor = PageColor,
            Font = new Font("Times New Roman", 9f),
            ForeColor = Muted,
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false
        };
        UpdateFooterHint();

        Controls.Add(_banner);
        Controls.Add(_ratingCard);
        Controls.Add(_tabStrip);
        Controls.Add(_pageCard);
        Controls.Add(_goButton);
        Controls.Add(_footerHint);

        Resize += (_, _) => LayoutContent();
        LayoutContent();
    }

    /// <summary>Raised when the learner confirms the selected option.</summary>
    public event EventHandler<ResultsChoice>? Confirmed;

    /// <summary>Gets the report shown on this screen.</summary>
    public LessonReport Report => _report;

    /// <summary>Gets a value indicating whether the statistics page is visible.</summary>
    public bool StatisticsVisible => _statsPage.Visible;

    /// <summary>Gets a value indicating whether the next-lesson option is enabled.</summary>
    public bool NextLessonAvailable => _nextOption.Enabled;

    /// <summary>Gets a value indicating whether Try again is currently selected.</summary>
    public bool RetrySelected => _retryOption.Checked;

    /// <summary>Switches to the detailed statistics tab.</summary>
    public void SelectStatisticsTab() => SelectTab(statistics: true);

    /// <summary>Switches to the next-step tab.</summary>
    public void SelectNextStepTab() => SelectTab(statistics: false);

    /// <summary>Moves the keyboard focus to the preselected option.</summary>
    public void FocusChoice()
    {
        if (_report.Passed)
        {
            _nextOption.Focus();
        }
        else
        {
            _retryOption.Focus();
        }
    }

    /// <summary>
    /// Confirms the selected option. Called by the Go on button and by the main
    /// window when Enter is pressed; does nothing after the first call.
    /// </summary>
    public void Confirm()
    {
        if (_confirmed)
        {
            return;
        }

        _confirmed = true;
        ResultsChoice choice = _report.Passed && _nextOption.Checked
            ? ResultsChoice.NextLesson
            : ResultsChoice.TryAgain;

        Confirmed?.Invoke(this, choice);
    }

    private void LayoutContent()
    {
        int width = ClientSize.Width;
        int height = ClientSize.Height;
        if (width < 320 || height < 320)
        {
            return;
        }

        const int gap = 8;
        const int bannerHeight = 56;
        const int ratingHeight = 246;
        const int tabHeight = 38;
        const int footerHeight = 48;
        const int footerGap = 8;

        int available = height - bannerHeight - gap - ratingHeight - gap - tabHeight - 6;
        int pageHeight = Math.Clamp(available - footerGap - footerHeight, 160, 360);

        // Keep the whole results stack vertically balanced on tall windows
        // instead of pinning it to the top and leaving a large empty card.
        int total = bannerHeight + gap + ratingHeight + gap + tabHeight + 6 +
                    pageHeight + footerGap + footerHeight;
        int top = Math.Max(0, (height - total) / 2);

        _banner.SetBounds(0, top, width, bannerHeight);

        int y = top + bannerHeight + gap;
        _ratingCard.SetBounds(0, y, width, ratingHeight);

        y += ratingHeight + gap;
        _tabStrip.SetBounds(0, y, width, tabHeight);

        y += tabHeight + 6;
        _pageCard.SetBounds(0, y, width, pageHeight);

        int footerTop = y + pageHeight + footerGap;
        _goButton.SetBounds(Math.Max(0, width - 236), footerTop, 228, 40);
        _footerHint.SetBounds(2, footerTop, Math.Max(80, width - 260), 40);

        LayoutBanner(width);
        LayoutRatingCard(width);
        LayoutTabs(width);
        LayoutPage();
    }

    private void LayoutBanner(int width)
    {
        int counterWidth = Math.Min(300, Math.Max(180, width / 4));
        _bannerTitle.SetBounds(20, 4, Math.Max(120, width - counterWidth - 60), 26);
        _bannerDetail.SetBounds(22, 28, Math.Max(120, width - counterWidth - 64), 22);
        _bannerCounter.SetBounds(width - counterWidth - 18, 8, counterWidth, 40);
    }

    private void LayoutRatingCard(int width)
    {
        const int side = 24;
        const int starWidth = 192;
        int starLeft = Math.Max(side + 200, width - side - starWidth);
        int barWidth = Math.Max(240, starLeft - side - 26);

        int[] tops = { 2, 60, 118, 176 };
        for (int i = 0; i < _bars.Length; i++)
        {
            _bars[i].SetBounds(side, tops[i], barWidth, 62);
        }

        _stars.SetBounds(starLeft, 26, starWidth, 190);
    }

    private void LayoutTabs(int width)
    {
        _nextTab.SetBounds(0, 0, 104, 32);
        _statsTab.SetBounds(112, 0, 182, 32);
        _nextUnderline.SetBounds(0, 32, 104, 3);
        _statsUnderline.SetBounds(112, 32, 182, 3);
        _tabLine.SetBounds(0, 35, width, 1);
    }

    private void LayoutPage()
    {
        int innerWidth = Math.Max(200, _pageCard.ClientSize.Width);
        int innerHeight = Math.Max(120, _pageCard.ClientSize.Height);

        // Centre the short "what next" block inside a tall card, and tighten
        // the spacing when the window is short enough to need it.
        bool compact = innerHeight < 240;
        int blockHeight = compact ? 146 : 202;
        int top = Math.Max(6, (innerHeight - blockHeight) / 2);

        int nextTop = top + (compact ? 28 : 32);
        int nextDetailTop = top + (compact ? 54 : 62);
        int retryTop = top + (compact ? 76 : 90);
        int retryDetailTop = top + (compact ? 102 : 120);
        int infoTop = top + (compact ? 132 : 150);

        _nextHeading.SetBounds(30, top, Math.Max(120, innerWidth - 60), 26);
        _nextOption.SetBounds(32, nextTop, Math.Max(120, innerWidth - 64), 26);
        _nextDetail.SetBounds(56, nextDetailTop, Math.Max(120, innerWidth - 84), 20);
        _retryOption.SetBounds(32, retryTop, Math.Max(120, innerWidth - 64), 26);
        _retryDetail.SetBounds(56, retryDetailTop, Math.Max(120, innerWidth - 84), 20);
        _choiceInfo.SetBounds(
            32,
            infoTop,
            Math.Max(120, innerWidth - 64),
            Math.Max(24, innerHeight - infoTop - 6));

        int chartWidth = Math.Max(140, (innerWidth - 44) / 2);
        int chartHeight = Math.Max(90, innerHeight - 24);
        _speedChart.SetBounds(16, 14, chartWidth, chartHeight);
        _accuracyChart.SetBounds(16 + chartWidth + 12, 14, chartWidth, chartHeight);
    }

    private StarRating BuildRatingCard(RoundedPanel card)
    {
        ScoreBar rating = CreateBar(
            "Overall rating",
            $"{Math.Round(_report.Rating)}%",
            _report.Rating / 100d);
        rating.Labels = new[]
        {
            new BarLabel(0d, "Could be better"),
            new BarLabel(0.34d, "OK"),
            new BarLabel(0.68d, "Good"),
            new BarLabel(1d, "Excellent")
        };

        double wpm = _report.WordsPerMinute;
        ScoreBar speed = CreateBar(
            "Speed",
            $"{Math.Round(wpm)} WPM",
            wpm / 60d);
        speed.TickFraction = 10d / 60d;
        speed.TickText = "10 WPM";

        ScoreBar words = CreateBar(
            "Slowdown",
            $"{Math.Round(_report.WordsWithDelay)}% of words",
            _report.WordsWithDelay / 100d);
        words.TickFraction = 0.9d;
        words.TickText = "90%";

        ScoreBar keys = CreateBar(
            "Slowdown",
            $"{Math.Round(_report.SlowKeystrokes)}% of keystrokes",
            _report.SlowKeystrokes / 100d);
        keys.TickFraction = 0.9d;
        keys.TickText = "90%";

        var stars = new StarRating
        {
            Stars = _report.Stars
        };

        _bars[0] = rating;
        _bars[1] = speed;
        _bars[2] = words;
        _bars[3] = keys;

        card.Controls.Add(rating);
        card.Controls.Add(speed);
        card.Controls.Add(words);
        card.Controls.Add(keys);
        card.Controls.Add(stars);
        return stars;
    }

    private static ScoreBar CreateBar(string caption, string value, double fraction)
    {
        return new ScoreBar
        {
            Caption = caption,
            ValueText = value,
            Fraction = fraction
        };
    }

    private void BuildTabStrip(Panel strip)
    {
        _nextTab = CreateTab("Next step", 104);
        _statsTab = CreateTab("Detailed statistics", 182);
        _statsTab.Location = new Point(112, 0);

        _nextUnderline = new Panel { BackColor = Accent };
        _statsUnderline = new Panel { BackColor = Accent, Visible = false };
        _tabLine = new Panel { BackColor = Border };

        _nextTab.Click += (_, _) => SelectTab(statistics: false);
        _statsTab.Click += (_, _) => SelectTab(statistics: true);

        strip.Controls.Add(_nextTab);
        strip.Controls.Add(_statsTab);
        strip.Controls.Add(_nextUnderline);
        strip.Controls.Add(_statsUnderline);
        strip.Controls.Add(_tabLine);
    }

    private static Label CreateTab(string text, int width)
    {
        return new Label
        {
            AutoSize = false,
            BackColor = PageColor,
            Cursor = Cursors.Hand,
            Font = new Font("Times New Roman", 10.5f, FontStyle.Bold),
            ForeColor = Muted,
            Size = new Size(width, 32),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false
        };
    }

    private void SelectTab(bool statistics)
    {
        _nextPage.Visible = !statistics;
        _statsPage.Visible = statistics;
        _nextUnderline.Visible = !statistics;
        _statsUnderline.Visible = statistics;
        _nextTab.ForeColor = statistics ? Muted : Accent;
        _statsTab.ForeColor = statistics ? Accent : Muted;
    }

    private (RadioButton Next, RadioButton Retry) BuildNextStepPage(Panel page)
    {
        var heading = new Label
        {
            AutoSize = false,
            BackColor = Color.White,
            Font = new Font("Times New Roman", 13f, FontStyle.Bold),
            ForeColor = Ink,
            Location = new Point(30, 8),
            Size = new Size(600, 26),
            Text = "Choose what to do next",
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false
        };
        _nextHeading = heading;

        var next = new RadioButton
        {
            AutoSize = false,
            BackColor = Color.White,
            Enabled = _report.Passed,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Times New Roman", 12f),
            ForeColor = Ink,
            Location = new Point(32, 40),
            Size = new Size(460, 26),
            Text = "Go to the next lesson",
            Checked = _report.Passed,
            UseMnemonic = false
        };

        _nextDetail = new Label
        {
            AutoSize = false,
            BackColor = Color.White,
            Font = new Font("Times New Roman", 9.5f),
            ForeColor = Muted,
            Text = _report.LessonNumber >= _report.LessonCount
                ? "You reached the last lesson – this restarts the course at lesson 1."
                : $"Continue to lesson {_report.LessonNumber + 1} of {_report.LessonCount}.",
            UseMnemonic = false
        };

        var retry = new RadioButton
        {
            AutoSize = false,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Times New Roman", 12f),
            ForeColor = Ink,
            Location = new Point(32, 98),
            Size = new Size(460, 26),
            Text = "Try again",
            Checked = !_report.Passed,
            UseMnemonic = false
        };

        _retryDetail = new Label
        {
            AutoSize = false,
            BackColor = Color.White,
            Font = new Font("Times New Roman", 9.5f),
            ForeColor = Muted,
            Text = "Reset this line and type it once more.",
            UseMnemonic = false
        };

        _choiceInfo = new Label
        {
            AutoSize = false,
            BackColor = Color.White,
            Font = new Font("Times New Roman", 9.5f),
            ForeColor = _report.Passed ? Success : Warning,
            Text = _report.Passed
                ? "Clean run – no mistakes. Both options are available."
                : "The next lesson unlocks after a mistake-free run. Try again to continue.",
            UseMnemonic = false
        };

        next.CheckedChanged += (_, _) => UpdateFooterHint();
        retry.CheckedChanged += (_, _) => UpdateFooterHint();

        page.Controls.Add(heading);
        page.Controls.Add(next);
        page.Controls.Add(_nextDetail);
        page.Controls.Add(retry);
        page.Controls.Add(_retryDetail);
        page.Controls.Add(_choiceInfo);
        return (next, retry);
    }

    private (KeyChart Speed, KeyChart Accuracy) BuildStatisticsPage(Panel page)
    {
        var speed = new KeyChart
        {
            Mode = KeyChartMode.Speed,
            Title = "Speed, KPM",
            Keys = _report.Keys
        };

        var accuracy = new KeyChart
        {
            Mode = KeyChartMode.Accuracy,
            Title = "Accuracy, keystrokes",
            Keys = _report.Keys
        };

        page.Controls.Add(speed);
        page.Controls.Add(accuracy);
        return (speed, accuracy);
    }

    private void UpdateFooterHint()
    {
        _footerHint.Text = _report.Passed && _nextOption.Checked
            ? "Press Enter to start the next lesson"
            : "Press Enter to try this lesson again";
    }

    private string MistakeCount()
    {
        int count = _report.Mistakes;
        return count == 1 ? "1 wrong character" : $"{count} wrong characters";
    }
}
